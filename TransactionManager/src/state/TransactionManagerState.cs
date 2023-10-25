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

        private List<Lease> _currentAcquiredLeases;

        private Dictionary<string, DadInt> _dadIntsSet;

        private string _name;

        private int _currentEpoch;

        private System.Timers.Timer _epochTimer;

        private int _timeSlotDuration;


        public TransactionManagerState(LeaseManagerServiceImpl leaseManagerService, string name, TimeOnly startingTime, int timeSlotDuration){
            _leaseManagerService = leaseManagerService;
            _currentAcquiredLeases = new List<Lease>();
            _dadIntsSet = new Dictionary<string, DadInt>();
            _name = name;

            _currentEpoch = 0;
            _timeSlotDuration = timeSlotDuration;
            //do epoch begin async
            Task.Run(() => EpochHandler(startingTime, timeSlotDuration));
        }

        public async Task<List<DadInt>> TransactionHandler(string clientId, List<string> keysToBeRead, List<DadInt> dadIntsToBeWritten){
            List<DadInt> returnedDadInts = new List<DadInt>();

            //create list of strings with keys of keystoberead and dadintstobewritten and no repetitions
            List<string> allKeys = new List<string>();

            foreach(string key in keysToBeRead)
                allKeys.Add(key);
            
            foreach(DadInt dadInt in dadIntsToBeWritten)
                if(!allKeys.Contains(dadInt.Key))
                    allKeys.Add(dadInt.Key);

            List<string> keysWithNoLease = GetKeysWithNoLease(allKeys);

            //TODO: FIX LEASE MANAGMENT
            if(keysWithNoLease.Count != 0){

                Lease leaseToBeRequested = new Lease();
                leaseToBeRequested.AssignedTransactionManager = _name;
                leaseToBeRequested.DadIntsKeys = keysWithNoLease.ToHashSet();

                LeaseSolicitationReturnStruct newLeases = await _leaseManagerService.LeaseSolicitation(leaseToBeRequested);
                //write newLeases
                Console.WriteLine("Received leases requested in epoch " + newLeases.Epoch + ":");

                for(int i = 0; i < newLeases.Leases.Count; i++){
                    Console.WriteLine("Received lease " + i + ":\n" + newLeases.Leases[i].ToString());
                    Console.WriteLine();
                }
                _currentAcquiredLeases.AddRange(newLeases.Leases);
            }
            //TODO: FIX LEASE MANAGMENT

            return ExecuteTransaction(keysToBeRead, dadIntsToBeWritten);
        }

        private List<string> GetKeysWithNoLease(List<string> keys){
            List<string> keysWithNoLease = new List<string>();

            List<string> keysWithLeaseFor = new List<string>();

            lock(this){

                foreach(Lease lease in _currentAcquiredLeases){
                    keysWithLeaseFor.AddRange(lease.DadIntsKeys);
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

        private List<DadInt> ExecuteTransaction(List<string> keysToBeRead, List<DadInt> dadIntsToBeWritten){
            List<DadInt> returnedDadInts = new List<DadInt>();

            lock(this){
                
                foreach(string key in keysToBeRead){
                    if(_dadIntsSet.ContainsKey(key)){
                        returnedDadInts.Add(_dadIntsSet[key]);
                    }
                }

                foreach(DadInt dadInt in dadIntsToBeWritten){

                    if(_dadIntsSet.ContainsKey(dadInt.Key)){
                        _dadIntsSet[dadInt.Key] = dadInt;
                    }else{
                        Console.WriteLine("DadInt with key " + dadInt.Key + " was not found. Creating new one.");
                        _dadIntsSet.Add(dadInt.Key, dadInt);
                    }
                
                }
            }

            return returnedDadInts;
        }

        private void EpochHandler(TimeOnly startingTime, int timeSlotDuration){

            try{
                int timeToWait = UtilMethods.getTimeUntilStart(startingTime);
                Thread.Sleep(timeToWait);
                _currentEpoch = 1;
                Console.WriteLine("Epoch " + _currentEpoch + " started");
                SetTimer(timeSlotDuration);
            }catch(InvalidStartingTimeException e){
                throw e;
            }
            
        }

        private void SetTimer(int timeToWait){
            _epochTimer = new System.Timers.Timer(timeToWait);
            _epochTimer.Elapsed += AdvanceEpoch;
            _epochTimer.AutoReset = true;
            _epochTimer.Enabled = true;

        }

        private void AdvanceEpoch(object source, ElapsedEventArgs e){
            Console.WriteLine("Epoch " + _currentEpoch + " ended");


            _currentEpoch++;

            Console.WriteLine("Epoch " + _currentEpoch + " started");
        }
    }
}