using Common.structs;
using TransactionManager.src.service;
using Common.util;
using Common.exceptions;
using System.Timers;

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

        private Dictionary<int, bool> _hasReceivedLeaseFromEpoch;

        //To check for unreceived lease request from the lease managers sent in epoch N, we have the following strategy:
        //1. If the duration of a time slot is greater or equal to X seconds, if by the beggining of the epoch N+2 we have not received an answer, broadcast to lease managers to warn them
        //2. If the duration of a time slot is less than X seconds, then after five seconds pass from the end of the epoch, contact the lease managers to warn them
        private bool _checksIfReceivedLeaseInEndOfEpoch;

        //The X seconds threshold talked about in the previous comment
        private static int _timeslotDurationThreshold = 5000;


        public TransactionManagerState(LeaseManagerServiceImpl leaseManagerService, string name, TimeOnly startingTime, int timeSlotDuration){
            _leaseManagerService = leaseManagerService;
            _currentAcquiredLeases = new List<Lease>();
            _dadIntsSet = new Dictionary<string, DadInt>();
            _name = name;

            _currentEpoch = 0;
            _timeSlotDuration = timeSlotDuration;
            _hasReceivedLeaseFromEpoch = new Dictionary<int, bool>();

            if(timeSlotDuration >= _timeslotDurationThreshold)
                _checksIfReceivedLeaseInEndOfEpoch = true;
            else
                _checksIfReceivedLeaseInEndOfEpoch = false;

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

                List<Lease> newLeases = await _leaseManagerService.LeaseSolicitation(leaseToBeRequested);
                //write newLeases
                foreach(Lease lease in newLeases){
                    Console.WriteLine("Received lease:\n" + lease.ToString());
                    Console.WriteLine();
                }
                _currentAcquiredLeases.AddRange(newLeases);
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
                SetTimer(timeSlotDuration);
            }catch(InvalidStartingTimeException e){
                throw e;
            }
            
        }

        private void SetTimer(int timeToWait){
            // Create a timer with a two second interval.
            _epochTimer = new System.Timers.Timer(timeToWait)
            {
                // Hook up the Elapsed event for the timer. 
                AutoReset = true,
                Enabled = true
            };
            _epochTimer.Elapsed += AdvanceEpoch;

        }

        private void AdvanceEpoch(object source, ElapsedEventArgs e){
            Console.WriteLine("Epoch " + _currentEpoch + " ended");

            if(_checksIfReceivedLeaseInEndOfEpoch){
                if(_hasReceivedLeaseFromEpoch.ContainsKey(_currentEpoch - 1) && !_hasReceivedLeaseFromEpoch[_currentEpoch - 1]){
                    Console.WriteLine("Epoch" + (_currentEpoch - 1) + " has not received feedback from lease managers. Sending warning to lease managers.");
                    _leaseManagerService.WarnUnreceivedLeases(_currentEpoch - 1);
                }
            }else{
                if(_currentEpoch > 0){
                    UnreceivedResultsTimerRun(_currentEpoch);
                }
            }

            _currentEpoch++;
            _hasReceivedLeaseFromEpoch.Add(_currentEpoch, false);

            // if(_checksIfReceivedLeaseInEndOfEpoch /*&& !_hasReceivedLeaseFromEpoch[_currentEpoch - 2]*/){
            //     Console.WriteLine("Epoch" + (_currentEpoch - 2) + " has not received feedback from lease managers. Sending warning to lease managers.");
            //     _leaseManagerService.WarnUnreceivedLeases(_currentEpoch - 2);
            // }else{
            //     UnreceivedResultsTimerRun(_currentEpoch);
            // }

            Console.WriteLine("Epoch " + _currentEpoch + " started");
        }


        private void UnreceivedResultsTimerRun(int epoch){
            System.Timers.Timer timer = new System.Timers.Timer(_timeslotDurationThreshold);
            _epochTimer.Elapsed += (sender, e) => UnreceivedResultsTimerHandler(sender, e, epoch);
            _epochTimer.Enabled = true;
            _epochTimer.AutoReset = false;
        }

        private void UnreceivedResultsTimerHandler(object source, ElapsedEventArgs e, int epoch){
            if(!_hasReceivedLeaseFromEpoch[epoch]){
                Console.WriteLine("Epoch" + epoch + " has not received feedback from lease managers. Sending warning to lease managers.");
                _leaseManagerService.WarnUnreceivedLeases(epoch);
            }
        }

    }
}