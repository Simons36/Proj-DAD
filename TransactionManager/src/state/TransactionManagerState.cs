using Common.structs;
using TransactionManager.src.service;
using Common.util;
using Common.exceptions;
using System.Timers;
using TransactionManager.src.structs;
using TransactionManager.src.state.utilityClasses;
using System.Security.Cryptography;

namespace TransactionManager.src.state
{
    public class TransactionManagerState
    {
        private LeaseManagerServiceImpl _leaseManagerService;

        private TransactionManagerInternalServiceClient _transactionManagerClient;

        private Dictionary<string, DadInt> _dadIntsSet;

        private string _name;

        private int _currentEpoch;

        private System.Timers.Timer _epochTimer;

        private int _timeSlotDuration;
        
        private int _timeSlotNumber;

        private TimeOnly _startingTime;

        private Dictionary<int, bool> _receivedLeasesReferringToEpoch; //key: epoch, value: true if received leases for that epoch, false otherwise

        private Dictionary<int, bool> _sentLeaseRequestReferringToEpoch; //key: epoch, value: true if sent lease request for that epoch, false otherwise

        private LeaseManagement _leaseManagement; //list of leases received in order of epoch

        private List<string> _dadIntsKeysNeverSeenBefore; //list of dadint keys that were never seen before

        private Dictionary<int, Transaction> _transactions; //key: transaction id, value: transaction

        private static int _msecondsToWaitForLease = 10000; //how many milliseconds to wait for a lease to be released

        private System.Timers.Timer _serverDoesntReleaseLeaseTimer;

        private bool _isOnline;


        public TransactionManagerState(LeaseManagerServiceImpl leaseManagerService, string name, TimeOnly startingTime, 
                                        int timeSlotDuration, int timeSlotNumber, TransactionManagerInternalServiceClient transactionManagerClient){
            _leaseManagerService = leaseManagerService;
            _transactionManagerClient = transactionManagerClient;
            _dadIntsSet = new Dictionary<string, DadInt>();
            _name = name;

            _currentEpoch = 0;

            _timeSlotDuration = timeSlotDuration;
            _timeSlotNumber = timeSlotNumber;
            _startingTime = startingTime;


            _sentLeaseRequestReferringToEpoch = new Dictionary<int, bool>();
            _receivedLeasesReferringToEpoch = new Dictionary<int, bool>();
            for(int i = 1; i <= timeSlotNumber; i++){
                _sentLeaseRequestReferringToEpoch.Add(i, false);
                _receivedLeasesReferringToEpoch.Add(i, false);
            }

            _leaseManagement = new LeaseManagement(_name);

            _dadIntsKeysNeverSeenBefore = new List<string>();

            _transactions = new Dictionary<int, Transaction>();

            _isOnline = true;
        }

        public void StartTransactionManager(){
            EpochHandler(_startingTime, _timeSlotDuration);
        }

        public async Task<List<DadInt>> TransactionHandler(string clientId, List<string> keysToBeRead, List<DadInt> dadIntsToBeWritten){
            
            //if there were no sent lease requests in the previous epoch, no need to wait for received leases
            //WaitForPreviousEpochToFinish(_currentEpoch - 1);

            Transaction transaction = new Transaction(keysToBeRead, dadIntsToBeWritten);
            int transactionId;

            lock(_transactions){
                transactionId = _transactions.Count;
                _transactions.Add(transactionId, transaction);
            }

            Console.WriteLine("Received transaction with transaction id " + transactionId);
            Console.WriteLine();

            //create list of strings with keys of keystoberead and dadintstobewritten and no repetitions
            List<string> allKeys = new List<string>();

            foreach(string key in keysToBeRead)
                allKeys.Add(key);
            
            foreach(DadInt dadInt in dadIntsToBeWritten)
                if(!allKeys.Contains(dadInt.Key))
                    allKeys.Add(dadInt.Key);

            List<string> keysWithNoLease = GetKeysWithNoLease(allKeys);

            if(keysWithNoLease.Count != 0){
                Console.WriteLine("Will need to ask for lease for transaction " + transactionId + " for the following keys:");
                foreach (string key in keysWithNoLease){
                    Console.WriteLine("  - " + key);
                }
                Console.WriteLine();

            }

            await SendToLeaseManagers(keysWithNoLease, false);

            return ExecuteTransaction(transactionId);
        }

        private List<string> GetKeysWithNoLease(List<string> keys){
            List<string> keysToBeAskedForLease = new List<string>();

            lock(_leaseManagement){
                keysToBeAskedForLease = _leaseManagement.GetKeysThatNeedToBeRequested(keys);
            }

            return keysToBeAskedForLease;
        }

        private List<DadInt> ExecuteTransaction(int transactionId){
            List<DadInt> returnedDadInts;
            int previousTransaction = transactionId - 1;

            lock(_transactions){

                if(transactionId != 0){
                    while(!_transactions.ContainsKey(previousTransaction) || !_transactions[previousTransaction].HasFinished){
                        Console.WriteLine("Waiting for transaction " + previousTransaction + " to finish");
                        Monitor.Wait(_transactions);
                    }
                }

            }

            WaitForThisServerTurn(transactionId);

            lock(_transactions){
                lock(_dadIntsSet){
                    returnedDadInts = _transactions[transactionId].Execute(_dadIntsSet);
                }
                Console.WriteLine("Executed " + transactionId + " at time: " + DateTime.Now.ToString("HH:mm:ss.fff"));

                List<LeaseTransactionManagerStruct> leasesReleased = _leaseManagement.ReleaseUsedKeys();

                Console.WriteLine("Sending the following leases at time:");
                foreach(LeaseTransactionManagerStruct lease in leasesReleased){
                    Console.WriteLine("Lease for key " + lease.Key + " with index " + lease.Index);
                }
                Console.WriteLine();
                Console.WriteLine("Right now this server has the following leases:");
                _leaseManagement.PrintLeasesOwnedByThisServer();
                Console.WriteLine();
                _transactionManagerClient.CommunicateTransactionHasBeenDone(_transactions[transactionId].DadIntsToBeWritten, leasesReleased);

                Monitor.PulseAll(_transactions);
            }

            Console.WriteLine("Returned dadints for transaction " + transactionId + ":");
            foreach(DadInt dadInt in returnedDadInts){
                Console.WriteLine("  - " + dadInt.Key + " -> " + dadInt.Value);
            }
            
            return returnedDadInts;
        }

        private void WaitForThisServerTurn(int transactionId){
            Console.WriteLine("Checking if this server has the appropriate leases for transaction " + transactionId);
            List<string> necessaryLeasesKeys = _transactions[transactionId].GetNecessaryKeys();

            _leaseManagement.GotNecessaryLeases(necessaryLeasesKeys);
            Console.WriteLine("This server has the appropriate leases for transaction " + transactionId);
        }

        private void ServerDoesntReleaseLeaseTimeout(){
            _serverDoesntReleaseLeaseTimer = new System.Timers.Timer(_msecondsToWaitForLease);
            _serverDoesntReleaseLeaseTimer.Elapsed += ServerDoesntReleaseLeaseTimeoutHandler;
        }

        private void ServerDoesntReleaseLeaseTimeoutHandler(object source, ElapsedEventArgs e){
        }

        private void DisposeServerDoesntReleaseLeaseTimer(){
            _serverDoesntReleaseLeaseTimer.Dispose();
        }


        public void ReceivedTransactionExecutionHandler(List<DadInt> dadIntsWritten, List<LeaseTransactionManagerStruct> freedLeases){
            UpdateDadIntsSet(dadIntsWritten);

            _leaseManagement.AddReceivedLeases(freedLeases);
        }

        private void UpdateDadIntsSet(List<DadInt> dadIntsWritten){
            lock(_dadIntsSet){
                foreach(DadInt dadInt in dadIntsWritten){
                    if(_dadIntsSet.ContainsKey(dadInt.Key)){
                        _dadIntsSet[dadInt.Key] = dadInt;
                    }else{
                        _dadIntsSet.Add(dadInt.Key, dadInt);
                    }
                }
            }
        }


        private void EpochHandler(TimeOnly startingTime, int timeSlotDuration){

            try{
                int timeToWait = UtilMethods.getTimeUntilStart(startingTime);
                Thread.Sleep(timeToWait);
                _currentEpoch = 1;
                Console.WriteLine("Epoch " + _currentEpoch + " started");


                Task.Run(() => SendToLeaseManagers(new List<string>(), true));
                AdvanceEpoch();
            }catch(InvalidStartingTimeException e){
                throw e;
            }
            
        }

        private void AdvanceEpoch(){
            while(_currentEpoch <= _timeSlotNumber){

                Thread.Sleep(_timeSlotDuration);
                Console.WriteLine("Epoch " + _currentEpoch + " ended");

                _currentEpoch++;
                Task.Run(() => SendToLeaseManagers(new List<string>(), true)); //just to make sure that even if we dont send any keys, we still receive leases from other servers

                Console.WriteLine("Epoch " + _currentEpoch + " started");
            }

            _isOnline = false;
        }


        private async Task SendToLeaseManagers(List<string> keysWithNoLease, bool justToGetResults){
            if(keysWithNoLease.Count != 0 || justToGetResults){

                Lease leaseToBeRequested = new Lease();
                leaseToBeRequested.AssignedTransactionManager = _name;
                leaseToBeRequested.DadIntsKeys = keysWithNoLease.ToHashSet();

                _sentLeaseRequestReferringToEpoch[_currentEpoch] = true;
                LeaseSolicitationReturnStruct newLeases = await _leaseManagerService.LeaseSolicitation(leaseToBeRequested);

                lock(_receivedLeasesReferringToEpoch){
                    //because lease solicitation can be called multiple times in the same epoch, only add leases if they were not already received
                    if(!_receivedLeasesReferringToEpoch[newLeases.Epoch]){ 
                        //add to current leases

                        _leaseManagement.AppendReceivedLeases(newLeases.Leases);


                        _receivedLeasesReferringToEpoch[newLeases.Epoch] = true;
                        Monitor.PulseAll(_receivedLeasesReferringToEpoch);
                    }

                }
            }
        }

        public void StatusCommandHandler(){
            Console.Write("Received Status Command: ");
            if(_isOnline){
                Console.WriteLine("Online");
            }else{
                Console.WriteLine("Offline");
            }
        }

    }
}