using Common.structs;
using TransactionManager.src.service;
using Common.util;
using Common.exceptions;
using System.Timers;
using TransactionManager.src.structs;

namespace TransactionManager.src.state
{
    public class TransactionManagerState
    {
        private LeaseManagerServiceImpl _leaseManagerService;

        private TransactionManagerInternalServiceClient _transactionManagerClient;

        private List<string> _currentAcquiredLeases;

        private Dictionary<string, DadInt> _dadIntsSet;

        private string _name;

        private int _currentEpoch;

        private System.Timers.Timer _epochTimer;

        private int _timeSlotDuration;
        
        private int _timeSlotNumber;

        private TimeOnly _startingTime;

        private Dictionary<int, bool> _receivedLeasesReferringToEpoch; //key: epoch, value: true if received leases for that epoch, false otherwise

        private Dictionary<int, bool> _sentLeaseRequestReferringToEpoch; //key: epoch, value: true if sent lease request for that epoch, false otherwise

        private Dictionary<string, List<string>> _listOfServersHoldingLeases; //key: dadint key, value: list of servers holding leases for that dadint

        private Dictionary<int, Transaction> _transactions; //key: transaction id, value: transaction

        private static int _msecondsToWaitForLease = 10000; //how many milliseconds to wait for a lease to be released

        private System.Timers.Timer _serverDoesntReleaseLeaseTimer;


        public TransactionManagerState(LeaseManagerServiceImpl leaseManagerService, string name, TimeOnly startingTime, 
                                        int timeSlotDuration, int timeSlotNumber, TransactionManagerInternalServiceClient transactionManagerClient){
            _leaseManagerService = leaseManagerService;
            _transactionManagerClient = transactionManagerClient;
            _currentAcquiredLeases = new List<string>();
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

            _listOfServersHoldingLeases = new Dictionary<string, List<string>>();
            _transactions = new Dictionary<int, Transaction>();
        }

        public void StartTransactionManager(){
            EpochHandler(_startingTime, _timeSlotDuration);
        }

        public async Task<List<DadInt>> TransactionHandler(string clientId, List<string> keysToBeRead, List<DadInt> dadIntsToBeWritten){
            
            //if there were no sent lease requests in the previous epoch, no need to wait for received leases
            WaitForPreviousEpochToFinish(_currentEpoch - 1);

            Transaction transaction = new Transaction(keysToBeRead, dadIntsToBeWritten);
            int transactionId;

            lock(_transactions){
                transactionId = _transactions.Count;
                _transactions.Add(transactionId, transaction);
            }

            //create list of strings with keys of keystoberead and dadintstobewritten and no repetitions
            List<string> allKeys = new List<string>();

            foreach(string key in keysToBeRead)
                allKeys.Add(key);
            
            foreach(DadInt dadInt in dadIntsToBeWritten)
                if(!allKeys.Contains(dadInt.Key))
                    allKeys.Add(dadInt.Key);

            List<string> keysWithNoLease = GetKeysWithNoLease(allKeys);

            await SendToLeaseManagers(keysWithNoLease);

            return ExecuteTransaction(transactionId);
        }

        private List<string> GetKeysWithNoLease(List<string> keys){
            List<string> keysWithNoLease = new List<string>();

            List<string> keysWithLeaseFor = new List<string>();

            lock(_currentAcquiredLeases){

                Console.WriteLine("Count:" + _currentAcquiredLeases.Count);

                foreach(string key in _currentAcquiredLeases){
                    keysWithLeaseFor.Add(key);
                }

            }

            keysWithLeaseFor = keysWithLeaseFor.Distinct().ToList();

            foreach(string key in keys){
                if(!keysWithLeaseFor.Contains(key)){
                    keysWithNoLease.Add(key);
                }
            }

            return keysWithNoLease;
        }

        private List<DadInt> ExecuteTransaction(int transactionId){
            List<DadInt> returnedDadInts;
            int previousTransaction = transactionId - 1;

            lock(_transactions){

                if(transactionId != 0){
                    while(!_transactions.ContainsKey(previousTransaction) || !_transactions[previousTransaction].HasFinished){
                        Monitor.Wait(_transactions);
                    }
                }

            }

            if(transactionId != 0){
                Console.WriteLine(_transactions[previousTransaction].HasFinished);
            }


            List<string> necessaryKeys = new List<string>();
            
            Console.WriteLine("GOing to execute transaction " + transactionId + "key ola: ");

            // for(int i = 0; i < _listOfServersHoldingLeases["ola"].Count; i++){
            //     Console.WriteLine("Position " + i + ":" + _listOfServersHoldingLeases["ola"][i]);
            // }

            necessaryKeys = CheckIfIsThisServerTurn(transactionId);


                lock(_transactions){
                    lock(_dadIntsSet){
                        returnedDadInts = _transactions[transactionId].Execute(_dadIntsSet);
                    }
                    Console.WriteLine("Executed " + transactionId + " at time: " + DateTime.Now.ToString("HH:mm:ss.fff"));
    



                    List<string> keysWithLeasesWeWillGiveUpOn = GetUnusedLeases(transactionId);

                    List<string> necessaryKeysRemovedFromLeasesWeWillGiveUpOn = necessaryKeys.Except(keysWithLeasesWeWillGiveUpOn).ToList();

                    Task.Run(() => _transactionManagerClient.
                    CommunicateTransactionHasBeenDone(necessaryKeys, necessaryKeysRemovedFromLeasesWeWillGiveUpOn, _transactions[transactionId].DadIntsToBeWritten));
                    Monitor.PulseAll(_transactions);
                }
            
            return returnedDadInts;
        }

        private List<string> CheckIfIsThisServerTurn(int transactionId){
            List<string> necessaryLeasesKeys = _transactions[transactionId].GetNecessaryKeys();

            foreach(string key in necessaryLeasesKeys){
                //Console.WriteLine(_transactions[transactionId - 1].HasFinished);
                Console.WriteLine("Checkin if has lease for key " + key);

                lock(this){
                    int x = 0;
                    while(!_listOfServersHoldingLeases[key][0].Equals(_name)){ //while it is not this server turn, wait
                        x++;
                        Console.WriteLine("Passed " + x + " times here for transaction " + transactionId + "for key " + key);
                        Task.Run(() => ServerDoesntReleaseLeaseTimeout());
                        Monitor.Wait(this);
                    }

                    string thisServer = _listOfServersHoldingLeases[key][0];
                    _listOfServersHoldingLeases[key].RemoveAt(0);
                    _listOfServersHoldingLeases[key].Add(thisServer);
                }

                Console.WriteLine("Checking complete for key " + key);
            }

            return necessaryLeasesKeys;
            //if reaches here, then is this server turn
        }

        private List<string> GetUnusedLeases(int transactionId){
            List<string> keysWithLeasesWeWillGiveUpOn = new List<string>();

            lock(this){


                List<string> leasesThatWilStillBeUsed = new List<string>();

                foreach(int id in _transactions.Keys){
                    if(!_transactions[id].HasFinished){
                        leasesThatWilStillBeUsed = leasesThatWilStillBeUsed.Concat(_transactions[id].GetNecessaryKeys()).Distinct().ToList();
                    }
                }

                
                    Console.WriteLine("Current acquired leases:");
                foreach(string key in _currentAcquiredLeases){
                    Console.WriteLine("Key: " + key);

                    if(!leasesThatWilStillBeUsed.Contains(key)){
                        keysWithLeasesWeWillGiveUpOn.Add(key);
                    }
                }

                keysWithLeasesWeWillGiveUpOn = keysWithLeasesWeWillGiveUpOn.Distinct().ToList();

                foreach(string key in keysWithLeasesWeWillGiveUpOn){ //remove from total leases
                    _currentAcquiredLeases.RemoveAll(entry => entry.Equals(key));
                }
                

                foreach(string key in keysWithLeasesWeWillGiveUpOn){ //but also remove itself from list of servers holding leases
                    _listOfServersHoldingLeases[key].RemoveAll(entry => entry.Equals(_name));
                }
            
            }

            return keysWithLeasesWeWillGiveUpOn;

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


        public void ReceivedTransactionExecutionHandler(List<string> keysExecutedInTransaction, List<string> gaveUpLeasesOnThisKeys, List<DadInt> dadIntsWritten){
            lock(this){
                foreach(string key in keysExecutedInTransaction){
                    if(!gaveUpLeasesOnThisKeys.Contains(key)){

                        //move server that just executed transaction to the end of the list
                        string server = _listOfServersHoldingLeases[key][0];
                        _listOfServersHoldingLeases[key].RemoveAt(0);
                        _listOfServersHoldingLeases[key].Add(server);


                    }
                }

                foreach(string key in gaveUpLeasesOnThisKeys){

                    Console.WriteLine();
                    Console.WriteLine("Removing server" + _listOfServersHoldingLeases[key][0] + " from key " + key);
                    Console.WriteLine();
                    _listOfServersHoldingLeases[key].RemoveAt(0);
                    Console.WriteLine("Pulse all on key " + key);
                    
                }

                Monitor.PulseAll(this);
            }
            for(int i = 0; i < _listOfServersHoldingLeases["simao"].Count; i++){
                Console.WriteLine("Position " + i + ":" + _listOfServersHoldingLeases["simao"][i]);
            }

            UpdateDadIntsSet(dadIntsWritten);

            DisposeServerDoesntReleaseLeaseTimer();
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
                object lockObject = new object();
                SetTimer(timeSlotDuration, lockObject);

                WaitForFinalEpoch(lockObject);

            }catch(InvalidStartingTimeException e){
                throw e;
            }
            
        }

        private void SetTimer(int timeToWait, object lockObject){
            _epochTimer = new System.Timers.Timer(timeToWait);
            _epochTimer.Elapsed += (sender, e) => AdvanceEpoch(sender, e, lockObject);
            _epochTimer.AutoReset = true;
            _epochTimer.Enabled = true;

        }

        private void AdvanceEpoch(object source, ElapsedEventArgs e, object objectCheckForEpochEnd){
            Console.WriteLine("Epoch " + _currentEpoch + " ended");

            lock(objectCheckForEpochEnd){
                Monitor.PulseAll(objectCheckForEpochEnd);
            }

            _currentEpoch++;

            Console.WriteLine("Epoch " + _currentEpoch + " started");
        }

        private void WaitForFinalEpoch(object lockObject){
            lock(lockObject){
                while(_currentEpoch != _timeSlotNumber){
                    Monitor.Wait(lockObject);
                }
            }
        }


        private void WaitForPreviousEpochToFinish(int previousEpoch){

            if(_currentEpoch != 1 && _sentLeaseRequestReferringToEpoch[previousEpoch]){
                lock(_receivedLeasesReferringToEpoch){
                    while(!_receivedLeasesReferringToEpoch[previousEpoch]){
                        Monitor.Wait(_receivedLeasesReferringToEpoch);
                    }
                }

            }

        }

        private async Task SendToLeaseManagers(List<string> keysWithNoLease){
            if(keysWithNoLease.Count != 0){

                Lease leaseToBeRequested = new Lease();
                leaseToBeRequested.AssignedTransactionManager = _name;
                leaseToBeRequested.DadIntsKeys = keysWithNoLease.ToHashSet();

                _sentLeaseRequestReferringToEpoch[_currentEpoch] = true;
                LeaseSolicitationReturnStruct newLeases = await _leaseManagerService.LeaseSolicitation(leaseToBeRequested);


                //write newLeases
                Console.WriteLine("Received leases requested in epoch " + newLeases.Epoch + ":");

                for(int i = 0; i < newLeases.Leases.Count; i++){
                    Console.WriteLine("Received lease " + i + ":\n" + newLeases.Leases[i].ToString());
                    Console.WriteLine();
                }

                lock(_receivedLeasesReferringToEpoch){
                    //because lease solicitation can be called multiple times in the same epoch, only add leases if they were not already received
                    if(!_receivedLeasesReferringToEpoch[newLeases.Epoch]){ 
                        //add to current leases

                        OrganizeReceivedLeases(newLeases);


                        _receivedLeasesReferringToEpoch[newLeases.Epoch] = true;
                        Monitor.PulseAll(_receivedLeasesReferringToEpoch);
                    }

                }
            }
        }

        private void OrganizeReceivedLeases(LeaseSolicitationReturnStruct receivedLeases){

            foreach(Lease lease in receivedLeases.Leases){
                foreach(string dadIntKey in lease.DadIntsKeys){
                    lock(_listOfServersHoldingLeases){
                        if(_listOfServersHoldingLeases.ContainsKey(dadIntKey)){
                            _listOfServersHoldingLeases[dadIntKey].Add(lease.AssignedTransactionManager);
                        }else{
                            _listOfServersHoldingLeases.Add(dadIntKey, new List<string>{lease.AssignedTransactionManager});
                        }
                    }
                }

                if(lease.AssignedTransactionManager.Equals(_name)){
                    lock(_currentAcquiredLeases){
                        foreach(string dadIntKey in lease.DadIntsKeys)
                            _currentAcquiredLeases.Add(dadIntKey);
                    }
                }
            }

        }

    }
}