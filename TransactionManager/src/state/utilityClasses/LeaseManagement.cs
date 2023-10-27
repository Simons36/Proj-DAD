using Common.structs;
using TransactionManager.src.structs;

namespace TransactionManager.src.state.utilityClasses
{
    public class LeaseManagement
    {
        private List<LeaseTransactionManagerStruct> _leasesOwnedByThisServer;

        private List<Lease> _receivedLeasesOrderedByLeaseManagers;

        private HashSet<string> _seenLeasesKeys;

        private string _thisServerName;

        private Dictionary<string, int> _lastLeaseNrThatSawThisLeaseKey;


        public LeaseManagement(string thisServerName){
            _leasesOwnedByThisServer = new List<LeaseTransactionManagerStruct>();
            _receivedLeasesOrderedByLeaseManagers = new List<Lease>();
            _seenLeasesKeys = new HashSet<string>();
            _thisServerName = thisServerName;
            _lastLeaseNrThatSawThisLeaseKey = new Dictionary<string, int>();
        }

        public void AppendReceivedLeases(List<Lease> leases){
            lock(_receivedLeasesOrderedByLeaseManagers){

                int nextIndex = _receivedLeasesOrderedByLeaseManagers.Count;

                lock(_seenLeasesKeys){

                    foreach(Lease lease in leases){
                        foreach(string dadIntKey in lease.DadIntsKeys){

                            if(!_seenLeasesKeys.Contains(dadIntKey)){
                                
                                _seenLeasesKeys.Add(dadIntKey);
                                if(lease.AssignedTransactionManager.Equals(_thisServerName)){
                                    CreateLeaseForKey(dadIntKey, nextIndex);
                                }

                            }
                        }
                        nextIndex++;

                    }
                }

                _receivedLeasesOrderedByLeaseManagers.AddRange(leases);
            }

        }

        private LeaseTransactionManagerStruct GetLeaseWithKey(string key){
            lock(_leasesOwnedByThisServer){
                return _leasesOwnedByThisServer.Find(lease => lease.Key == key);
            }
        }

        public void CreateLeaseForKey(string key, int index){
            if(ContainsLeaseWithKey(key)){
                throw new Exception("Lease already exists for key " + key);
            }

            lock(_lastLeaseNrThatSawThisLeaseKey){
                _lastLeaseNrThatSawThisLeaseKey.Add(key, index);
            }

            LeaseTransactionManagerStruct lease = new LeaseTransactionManagerStruct(key, index);
            lock(_leasesOwnedByThisServer){
                _leasesOwnedByThisServer.Add(lease);
                Monitor.PulseAll(_leasesOwnedByThisServer);
            }
        }

        public LeaseTransactionManagerStruct RemoveLease(string key){

            lock(_leasesOwnedByThisServer){
            
                if(!ContainsLeaseWithKey(key)){
                    throw new Exception("Lease does not exist for key " + key);
                }

                LeaseTransactionManagerStruct lease = _leasesOwnedByThisServer.Find(lease => lease.Key == key);
                _leasesOwnedByThisServer.Remove(_leasesOwnedByThisServer.Find(lease => lease.Key == key));
                return lease;
            }
        }

        public bool ContainsLeaseWithKey(string key){
            lock(_leasesOwnedByThisServer){
                return _leasesOwnedByThisServer.Any(lease => lease.Key == key);
            }
        }

        public List<string> GetKeysThatNeedToBeRequested(List<string> listKeys){
            lock(this){

                List<string> keysToRequest = new List<string>();

                foreach(string key in listKeys){
                    if(!_seenLeasesKeys.Contains(key)){
                        keysToRequest.Add(key);
                    }else{

                        if(!ContainsLeaseWithKey(key)){
                            int lastSeenIndex = _lastLeaseNrThatSawThisLeaseKey[key];

                            bool willReceive = false;

                            for(int i = lastSeenIndex + 1; i < _receivedLeasesOrderedByLeaseManagers.Count; i++){
                                if(_receivedLeasesOrderedByLeaseManagers[i].DadIntsKeys.Contains(key) && _receivedLeasesOrderedByLeaseManagers[i].AssignedTransactionManager.Equals(_thisServerName)){
                                    willReceive = true;
                                }
                            }

                            if(!willReceive){
                                keysToRequest.Add(key);
                            }
                        }

                    }

                }

                return keysToRequest;
            }
        }

        public void GotNecessaryLeases(List<string> leases){
            lock(_leasesOwnedByThisServer){
                while(!ContainsAllLeases(leases)){
                    //print leases that this server has currently
                    Console.WriteLine();
                    foreach(LeaseTransactionManagerStruct lease in _leasesOwnedByThisServer){
                        Console.WriteLine("Lease for key " + lease.Key + " with index " + lease.Index);
                    }
                    Console.WriteLine();
                    Monitor.Wait(_leasesOwnedByThisServer);
                }
            }
        }

        public bool ContainsAllLeases(List<string> leases){
            foreach(string lease in leases){
                if(!ContainsLeaseWithKey(lease)){
                    return false;
                }
            }
            return true;
        }

        public List<LeaseTransactionManagerStruct> ReleaseUsedKeys(){
            List<LeaseTransactionManagerStruct> leasesToRelease = new List<LeaseTransactionManagerStruct>();

            List<string> keys = new List<string>();
            foreach(LeaseTransactionManagerStruct lease in _leasesOwnedByThisServer){
                keys.Add(lease.Key);
            }

            lock(_receivedLeasesOrderedByLeaseManagers){
                foreach(string key in keys){
                    LeaseTransactionManagerStruct lease = GetLeaseWithKey(key);
                    try{
                        for(int i = lease.Index; i < _receivedLeasesOrderedByLeaseManagers.Count; i++){
                            if(_receivedLeasesOrderedByLeaseManagers[i].DadIntsKeys.Contains(key) && !_receivedLeasesOrderedByLeaseManagers[i].AssignedTransactionManager.Equals(_thisServerName)){
                                leasesToRelease.Add(RemoveLease(key));
                                break;
                            }
                        }
                    }catch(Exception e){
                        Console.WriteLine("Error: " + e.StackTrace);
                    }
                }
            }

            return leasesToRelease;
        }

        public void AddReceivedLeases(List<LeaseTransactionManagerStruct> receivedLeases){

            foreach(LeaseTransactionManagerStruct lease in receivedLeases){
                int index = lease.Index;
                bool skip = false;

                for(int i = index + 1; i < _receivedLeasesOrderedByLeaseManagers.Count; i++){

                    if(_receivedLeasesOrderedByLeaseManagers[i].DadIntsKeys.Contains(lease.Key)){

                        if( _receivedLeasesOrderedByLeaseManagers[i].AssignedTransactionManager.Equals(_thisServerName)){
                            lease.Index = i;
                            break;
                        }else{
                            skip = true;
                            break;
                        }
                    }   
                }

                if(skip){
                    continue;
                }
                

                if(index == lease.Index){
                    throw new Exception("Lease has same index, impossible");
                }

                lock(_lastLeaseNrThatSawThisLeaseKey){
                    _lastLeaseNrThatSawThisLeaseKey[lease.Key] = lease.Index;
                }

                lock(_leasesOwnedByThisServer){
                    _leasesOwnedByThisServer.Add(lease);
                    Monitor.PulseAll(_leasesOwnedByThisServer);
                }
            }

            foreach(LeaseTransactionManagerStruct lease in receivedLeases){
                Console.WriteLine("Received lease for key " + lease.Key + " with index " + lease.Index + "at time " + DateTime.Now.ToString("HH:mm:ss.fff"));
            }

            Console.WriteLine();
            Console.WriteLine("Leases owned by this server: ");
            foreach(LeaseTransactionManagerStruct lease in _leasesOwnedByThisServer){
                Console.WriteLine("Lease for key " + lease.Key + " with index " + lease.Index);
            }
            
        }

        public void PrintLeasesOwnedByThisServer(){
            lock(_leasesOwnedByThisServer){
                foreach(LeaseTransactionManagerStruct lease in _leasesOwnedByThisServer){
                    Console.WriteLine("Lease for key " + lease.Key + " with index " + lease.Index);
                }
                Console.WriteLine();
            }
        }

        public void PrintReceivedLeasesOrderedByLeaseManagers(){
            lock(_receivedLeasesOrderedByLeaseManagers){
                for(int i = 0; i < _receivedLeasesOrderedByLeaseManagers.Count; i++){
                    Console.WriteLine("Lease nr " + i + ":");
                    Console.WriteLine("Transaction manager: " + _receivedLeasesOrderedByLeaseManagers[i].AssignedTransactionManager);
                    foreach(string key in _receivedLeasesOrderedByLeaseManagers[i].DadIntsKeys){
                        Console.WriteLine("Key: " + key);
                    }
                    Console.WriteLine();
                }
                Console.WriteLine();
            }
        }
    }
}