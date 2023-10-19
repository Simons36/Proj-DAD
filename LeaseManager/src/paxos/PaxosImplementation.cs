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

            Console.WriteLine("Received lease request:");
            Console.WriteLine(requestedLease.ToString());
            Console.WriteLine("Adding to current epoch received leases");

            lock(this){
                _currentEpochReceivedLeases.Add(requestedLease);
            }

            lock(this){

                //TODO: remove this
                if(thisRequestEpoch == -1){
                    thisRequestEpoch = 0;
                }

                if(!_epochResult.ContainsKey(thisRequestEpoch )){
                    _epochResult.Add(thisRequestEpoch , new TaskCompletionSource<List<Lease>>());
                    Console.WriteLine("Added task completion source for epoch: " + thisRequestEpoch);
                }
            }

            return thisRequestEpoch;

        }

        public async Task<List<Lease>> GetRequestReult(int epoch){
    
            return await _epochResult[epoch].Task;
        }

        public async void AdvanceEpoch(){
            _currentEpoch++;
            Console.WriteLine("Advancing epoch to nr: " + _currentEpoch);
            
            if(_currentEpoch != 0){

                Task orderRequests = new Task(() => OrderPreviousEpochRequests(_currentEpochReceivedLeases, _currentEpoch - 1));
                orderRequests.Start();

                lock(this){
                    _currentEpochReceivedLeases.Clear();
                }

                await orderRequests;
            }

        }

        public async void OrderPreviousEpochRequests(List<Lease> previousEpochRequests, int epoch){
            

            Console.WriteLine("Ordering previous epoch requests");
            

            bool isThisServerLeader;

            if(!_isCurrentLeader){
                isThisServerLeader = ThisServerLeader();
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

        public bool ThisServerLeader(){
            Console.Write("Determining leader: ");

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
            Console.WriteLine("Received prepare request with write timestamp: " + paxosMessageStruct.WriteTimestamp + " and epoch: " + paxosMessageStruct.Epoch);

            PaxosInstance paxosInstance = _activePaxosInstances[paxosMessageStruct.Epoch];

            try{
                return paxosInstance.PrepareRequestHandler(paxosMessageStruct);
            }catch(ReadTimestampGreaterThanWriteTimestampException e){
                throw e;
            }
        }

        public PaxosMessageStruct AcceptRequestHandler(PaxosMessageStruct paxosMessageStruct){
            Console.WriteLine("Received accept request with write timestamp: " + paxosMessageStruct.WriteTimestamp + " and epoch: " + paxosMessageStruct.Epoch);

            PaxosInstance paxosInstance = _activePaxosInstances[paxosMessageStruct.Epoch];

            return paxosInstance.AcceptRequestHandler(paxosMessageStruct);
            
        }

        public void CheckIfNeededToSendTransactionMangers(PaxosInstance paxosInstance){
            Console.WriteLine("Checking if need to send transaction managers");

            if(paxosInstance.IsLeaderCurrentEpoch){
                _epochResult[paxosInstance.Epoch].SetResult(paxosInstance.ProposedValue);
            }else{
                _epochResult[paxosInstance.Epoch].SetResult(new List<Lease>());
            }
        }

    }
}