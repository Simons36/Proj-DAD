using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Common.structs;
using TransactionManager.src.service;

namespace TransactionManager.src.state
{
    public class TransactionManagerState
    {
        private LeaseManagerServiceImpl _leaseManagerService;

        private List<Lease> _currentAcquiredLeases;

        private Dictionary<string, DadInt> _dadIntsSet;

        private string _name;

        public TransactionManagerState(LeaseManagerServiceImpl leaseManagerService, string name){
            _leaseManagerService = leaseManagerService;
            _currentAcquiredLeases = new List<Lease>();
            _dadIntsSet = new Dictionary<string, DadInt>();
            _name = name;
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
    }
}