using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Grpc.Core;
using LeaseManager.src.service;
using Common.util;
using Common.exceptions;
using Common.structs;
using LeaseManager.src.paxos.exceptions;

namespace LeaseManager.src.paxos
{
    public class PaxosImplementation
    {
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

        }

        public void Start(){

            Console.WriteLine("----------------------");
            Console.WriteLine("LABEL:");
            Console.WriteLine("-LPX- -> Corresponds to an operation of listening phase of epoch X");
            Console.WriteLine("-OPX- -> Corresponds to an operation of ordering phase of epoch X");
            Console.WriteLine("----------------------");

            _currentEpoch = -1;

            int timeToWait = 0;

            try{
                
                foreach(TimeOnly epochStart in _epochStartingTimes){
                    timeToWait = UtilMethods.getTimeUntilStart(epochStart);
                
                    Thread.Sleep(timeToWait);
                    
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

            lock(this){
                _currentEpochReceivedLeases.Add(requestedLease);
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
            
            if(_currentEpoch != 0){
                int previousEpoch = _currentEpoch - 1;
                
                Console.WriteLine("---------------- EPOCH NR: " + previousEpoch + " | ORDERING PHASE STARTED -----------------");
                Console.WriteLine();

                List<Lease> copiedCurrentEpochReceivedLeases = new List<Lease>(_currentEpochReceivedLeases);

                Task orderRequests = new Task(() => OrderPreviousEpochRequests(copiedCurrentEpochReceivedLeases, previousEpoch));
                orderRequests.Start();

                lock(this){
                    _currentEpochReceivedLeases.Clear();
                }

                await orderRequests;
                
            }
            Console.WriteLine("---------------- EPOCH NR: " + _currentEpoch + " | LISTENING PHASE STARTED -----------------");
            Console.WriteLine();

        }

        public async void OrderPreviousEpochRequests(List<Lease> previousEpochRequests, int epoch){
            

            Console.WriteLine("-OP" + epoch + "- Ordering epoch " + epoch + " requests");
            

            bool isThisServerLeader;

            if(!_isCurrentLeader){
                isThisServerLeader = ThisServerLeader(epoch);
            }else{
                isThisServerLeader = true;
            }

            PaxosInstance paxosInstance = new PaxosInstance(_id,  epoch, isThisServerLeader, 
                                    _isCurrentLeader, previousEpochRequests, _paxosClient);

            Task paxosTask = new Task(() => paxosInstance.StartInstance());
            paxosTask.Start();

            _activePaxosInstances.Add(epoch, paxosInstance);
            
            await paxosTask;

            CheckIfNeededToSendTransactionMangers(paxosInstance);

        }

        public bool ThisServerLeader(int epoch){
            Console.Write("-OP" + epoch + "- Determining leader: ");

            if(_id == 1){
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
            Console.Write("-OP" + paxosInstance.Epoch + "- Checking if this server needs to send transaction managers the result of epoch " + paxosInstance.Epoch + ":");

            if(paxosInstance.IsLeaderCurrentEpoch){
                Console.WriteLine("yes");
                _epochResult[paxosInstance.Epoch].SetResult(paxosInstance.ProposedValue);

                Console.WriteLine("-OP" + paxosInstance.Epoch + "- Reached consensus:");
                Console.WriteLine();
                for(int i = 0; i < paxosInstance.ProposedValue.Count; i++){
                    Console.WriteLine("Lease nr " + i + ":");
                    Console.WriteLine(paxosInstance.ProposedValue[i].ToString());
                    Console.WriteLine();
                }
            }else{
                Console.WriteLine("no");
                _epochResult[paxosInstance.Epoch].SetResult(new List<Lease>());
            }
        }

    }
}