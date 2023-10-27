using LeaseManager.src.service;
using Common.util;
using Common.exceptions;
using Common.structs;
using LeaseManager.src.paxos.exceptions;

namespace LeaseManager.src.paxos
{
    public class PaxosImplementation
    {
        private string _name;

        private List<TimeOnly> _epochStartingTimes;

        private Dictionary<string, int> _leaseManagerNameToId;

        private int _crashingTimeSlot;

        private Dictionary<string, List<int>> _suspectedServers;

        private int _id;

        private int _numLeaseManagers;

        private int _currentEpoch;

        private List<Lease> _currentEpochReceivedLeases;

        private PaxosInternalServiceClient _paxosClient;

        private bool _isCurrentLeader;

        private Dictionary<int, PaxosInstance> _activePaxosInstances;

        private Dictionary<int, TaskCompletionSource<List<Lease>>> _epochResult;

        //needed for leader election
        private int _timesRestartedEpoch;

        public PaxosImplementation(int timeslotNumber, int duration, TimeOnly startingTime, Dictionary<string, int> leaseManagerNameToId, 
                                    int crashingTimeSlot, Dictionary<string, List<int>> suspectedServers, int id, PaxosInternalServiceClient paxosClient){
            
            _leaseManagerNameToId = leaseManagerNameToId;
            _crashingTimeSlot = crashingTimeSlot;
            _suspectedServers = suspectedServers;

            _epochStartingTimes = new List<TimeOnly>();
            _epochStartingTimes.Add(startingTime);

            for(int i = 0; i < timeslotNumber; i++){
                _epochStartingTimes.Add(_epochStartingTimes[i].Add(TimeSpan.FromMilliseconds(duration)));
            }

            _id = id;
            _currentEpochReceivedLeases = new List<Lease>();
            _numLeaseManagers = leaseManagerNameToId.Count;
            _paxosClient = paxosClient;

            _isCurrentLeader = false;

            _activePaxosInstances = new Dictionary<int, PaxosInstance>();

            _epochResult = new Dictionary<int, TaskCompletionSource<List<Lease>>>();
            _timesRestartedEpoch = 0;
        }

        public string GetThisServerName(){
            foreach(string key in _leaseManagerNameToId.Keys){
                if(_leaseManagerNameToId[key] == _id){
                    return key;
                }
            }
            throw new Exception();
        }

        public void Start(){

            Console.WriteLine("----------------------");
            Console.WriteLine("LABEL:");
            Console.WriteLine("-LPX- -> Corresponds to an operation of listening phase of epoch X");
            Console.WriteLine("-OPX- -> Corresponds to an operation of ordering phase of epoch X");
            Console.WriteLine("----------------------");

            _currentEpoch = 0;

            int timeToWait = 0;

            try{
                
                foreach(TimeOnly epochStart in _epochStartingTimes){
                    timeToWait = UtilMethods.getTimeUntilStart(epochStart);
                
                    Thread.Sleep(timeToWait);
                    
                    if(_currentEpoch == _crashingTimeSlot - 1){
                        Console.WriteLine("Crashing at epoch " + _currentEpoch + 1);
                        return;
                    }
                    Task advanceEpoch = new Task(() => AdvanceEpoch());
                    advanceEpoch.Start();

                }


            }catch(InvalidStartingTimeException e){
                throw e;
            }
           
        }

        public int RegisterLeaseRequest(Lease requestedLease){
            int thisRequestEpoch = _currentEpoch;

            Console.WriteLine("-LP" + thisRequestEpoch 
                                           + "- Received the following lease request:\n\n" 
                                           + requestedLease.ToString() 
                                           + "\n-LP" 
                                           + thisRequestEpoch 
                                           + "- Adding to current epoch received leases");

            if(requestedLease.DadIntsKeys.Count != 0){
                lock(this){
                    _currentEpochReceivedLeases.Add(requestedLease);
                }
            }


            lock(this){
                if(!_epochResult.ContainsKey(thisRequestEpoch )){
                    _epochResult.Add(thisRequestEpoch , new TaskCompletionSource<List<Lease>>());
                }
            }

            return thisRequestEpoch;

        }

        public async Task<List<Lease>> GetRequestReult(int epoch){
    
            return await _epochResult[epoch].Task;
        }

        public async void AdvanceEpoch(){
            
            _currentEpoch++;
            Console.WriteLine("---------------- EPOCH NR: " + _currentEpoch + " | LISTENING PHASE STARTED -----------------");
            Console.WriteLine();
            
            if(_currentEpoch != 1){
                int previousEpoch = _currentEpoch - 1;
                
                Console.WriteLine("---------------- EPOCH NR: " + previousEpoch + " | ORDERING PHASE STARTED -----------------");
                Console.WriteLine();

                List<Lease> copiedCurrentEpochReceivedLeases = new List<Lease>(_currentEpochReceivedLeases);

                if((previousEpoch - 1) > 0 && !CheckIfPreviousEpochCompleted(previousEpoch - 1)){
                    Console.WriteLine("OP" + (previousEpoch - 1) + "- Previous epoch not completed (leader may have crashed)");
                    CheckIfRestartEpochNeeded(previousEpoch - 1);
                }
                Task orderRequests = new Task(() => OrderPreviousEpochRequests(copiedCurrentEpochReceivedLeases, previousEpoch, false));
                orderRequests.Start();

                lock(this){
                    _currentEpochReceivedLeases.Clear();
                }

                await orderRequests;
                
            }

        }

        public void OrderPreviousEpochRequests(List<Lease> previousEpochRequests, int epoch, bool isRestart){

            if(isRestart){
                Console.WriteLine("-OP" + epoch + "- Restarting epoch " + epoch + " requests");
            }else{
                Console.WriteLine("-OP" + epoch + "- Ordering epoch " + epoch + " requests");
            }


            bool isThisServerLeader = false;

            if(!_isCurrentLeader){
                isThisServerLeader = ThisServerLeader(epoch);
            }else{
                isThisServerLeader = true;
            }

            if(isRestart){
                _activePaxosInstances.Remove(epoch);

            }

            PaxosInstance paxosInstance = new PaxosInstance(_id,  epoch, isThisServerLeader, 
                                    _isCurrentLeader, previousEpochRequests, _paxosClient);

            Task paxosTask = new Task(() => paxosInstance.StartInstance());
            paxosTask.Start();

            //set for next epochs
            _isCurrentLeader = isThisServerLeader;


            _activePaxosInstances.Add(epoch, paxosInstance);
            
            paxosTask.Wait();

            CheckIfNeededToSendTransactionMangers(paxosInstance);

        }

        public bool ThisServerLeader(int epoch){
            Console.Write("-OP" + epoch + "- Determining leader: ");

            if(_id == (1 + _timesRestartedEpoch)){
                Console.WriteLine("this server is leader.");
                return true;
            }else{
                List<int> suspectedServersCurrentEpoch = GetSuspectedServersCurrentEpoch();

                int serverId = 1;
                
                for(; serverId < GetMaxNumberOfCrashedServers() + 1; serverId++){
                    if(!suspectedServersCurrentEpoch.Contains(serverId)){
                        Console.WriteLine("this server is not leader.");
                        return false;
                    }
                }

                if(serverId == _id){
                    Console.WriteLine("this server thinks it is leader, because it suspects previous leaders are crashed.");
                    return true;
                }
                
                Console.WriteLine("this server is not leader.");
                return false;
            }

        }

        //returns the servers that are suspected to be faulty in the current epoch
        public List<int> GetSuspectedServersCurrentEpoch(){
            List<int> returnedServers = new List<int>();

            foreach(string key in _suspectedServers.Keys){

                if(_suspectedServers[key].Contains(_currentEpoch))
                    returnedServers.Add(_leaseManagerNameToId[key]);
                    
            }

            return returnedServers;
        }

        //there always needs to be a majority of servers running to achieve consensus
        public int GetMaxNumberOfCrashedServers(){
            //return the highest possible minority of servers that can be crashed
            return (_numLeaseManagers - 1) / 2;
        }

        public PaxosMessageStruct PrepareRequestHandler(PaxosMessageStruct paxosMessageStruct){
            Console.WriteLine("-OP" + paxosMessageStruct.Epoch + "- Received prepare request with write timestamp: " + paxosMessageStruct.WriteTimestamp + " and epoch: " + paxosMessageStruct.Epoch);

            PaxosInstance paxosInstance = _activePaxosInstances[paxosMessageStruct.Epoch];

            try{
                return paxosInstance.PrepareRequestHandler(paxosMessageStruct);
            }catch(ReadTimestampGreaterThanWriteTimestampException e){
                throw e;
            }
        }

        public PaxosMessageStruct AcceptRequestHandler(PaxosMessageStruct paxosMessageStruct){
            Console.WriteLine("-OP" + paxosMessageStruct.Epoch + "- Received accept request with write timestamp: " + paxosMessageStruct.WriteTimestamp + " and epoch: " + paxosMessageStruct.Epoch);

            PaxosInstance paxosInstance = _activePaxosInstances[paxosMessageStruct.Epoch];

            try{
                return paxosInstance.AcceptRequestHandler(paxosMessageStruct);
            }catch(ReadTimestampGreaterThanWriteTimestampException e){
                throw e;
            }

            
        }

        public void CheckIfNeededToSendTransactionMangers(PaxosInstance paxosInstance){
            
            if(!_epochResult.ContainsKey(paxosInstance.Epoch)){
                Console.WriteLine("-OP" + paxosInstance.Epoch + "- There were no requests in epoch " + paxosInstance.Epoch + " so no need to send result to transaction managers");
                paxosInstance.HasReceivedFinalConfirmation = true;
            }else{
                Console.Write("-OP" + paxosInstance.Epoch + "- Checking if this server needs to send transaction managers the result of epoch " + paxosInstance.Epoch + ":");

                if(paxosInstance.IsLeaderCurrentEpoch){
                    Console.WriteLine(" Yes");
                    
                    _epochResult[paxosInstance.Epoch].SetResult(paxosInstance.ProposedValue);

                    //send confirmation to lease managers
                    _paxosClient.BroadcastSentToTmsConfirmation(paxosInstance.Epoch, paxosInstance.ProposedValue);

                }else{
                    Console.WriteLine(" No");

                    _epochResult[paxosInstance.Epoch].SetResult(new List<Lease>());

                    Console.WriteLine("There were no requests in epoch " + paxosInstance.Epoch + " so no need to send result to transaction managers");
                    
                }

            }
        

            Console.WriteLine("-OP" + paxosInstance.Epoch + "- Reached consensus:");
            Console.WriteLine();
            if(paxosInstance.ProposedValue.Count != 0){
                for(int i = 0; i < paxosInstance.ProposedValue.Count; i++){
                    Console.WriteLine("Lease nr " + i + ":");
                    Console.WriteLine(paxosInstance.ProposedValue[i].ToString());
                    Console.WriteLine();
                }
            }else{
                Console.WriteLine("EMPTY");
            }
        }

        public bool CheckIfPreviousEpochCompleted(int epoch){
            return _activePaxosInstances[epoch].HasReceivedFinalConfirmation;
        }

        public void SentToTransactionManagersConfirmationHandler(int epoch, List<Lease> epochResult){
            _activePaxosInstances[epoch].SentToTransactionManagersConfirmationHandler();
        }

        public void CheckIfRestartEpochNeeded(int epoch){
            Console.WriteLine("-OP" + epoch + "- Checking if previous epoch needs to be restarted");

            List<bool> listConfirmationOtherServers = _paxosClient.BroadcastCheckConfirmationReceived(epoch);
            bool needsRestart = true;

            foreach(bool confirmation in listConfirmationOtherServers){
                if(confirmation)//if just one server has received confirmation, then it is not needed to restart
                    needsRestart = false;
            }

            if(needsRestart){
                Console.WriteLine("-OP" + epoch + "- Previous epoch needs to be restarted");
                _timesRestartedEpoch++; //for leader election
                OrderPreviousEpochRequests(_activePaxosInstances[epoch].UnmodifiedProposedValue, epoch, true);

            }else{
                Console.WriteLine("-OP" + epoch + "- Previous epoch does not need to be restarted");
            }
        }

        public bool CheckConfirmationReceivedHandler(int epoch){
            return _activePaxosInstances[epoch].HasReceivedFinalConfirmation;
        }

    }
}