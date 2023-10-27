using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Common.structs;
using LeaseManager.src.service;
using LeaseManager.src.paxos.exceptions;

namespace LeaseManager.src.paxos
{
    public class PaxosInstance
    {

        private int _writeTimestamp;

        private int _readTimestamp;

        private int _id;

        private int _epoch;

        private bool _wasLeaderPreviousEpoch;

        private bool _isLeaderCurrentEpoch;

        private List<Lease> _proposedValue;

        private List<Lease> _unmodifiedProposedValue;

        private PaxosInternalServiceClient _paxosClient;

        private bool _hasReceivedFinalConfirmation;

        public PaxosInstance(int id, int epoch, bool isLeaderCurrentEpoch, bool wasLeaderPreviousEpoch, 
                                List<Lease> proposedValue, PaxosInternalServiceClient paxosClient){

            _id = id;
            _epoch = epoch;
            _isLeaderCurrentEpoch = isLeaderCurrentEpoch;
            _wasLeaderPreviousEpoch = wasLeaderPreviousEpoch;
            _writeTimestamp = 0;
            _readTimestamp = 0;

            _unmodifiedProposedValue = proposedValue;
            ProposedValueSetter(proposedValue);

            _paxosClient = paxosClient;
            _hasReceivedFinalConfirmation = false;
        }

        //getter for iscurrenleader
        public bool IsLeaderCurrentEpoch{
            get { return _isLeaderCurrentEpoch; }
        }

        //epoch getter
        public int Epoch{
            get { return _epoch; }
        }

        //getter for proposed value
        public List<Lease> ProposedValue{
            get { return _proposedValue; }
        }

        //getter for unmodified proposed value
        public List<Lease> UnmodifiedProposedValue{
            get { return _unmodifiedProposedValue; }
        }

        //getter for has received final confirmation
        public bool HasReceivedFinalConfirmation{
            get { return _hasReceivedFinalConfirmation; }
            set { _hasReceivedFinalConfirmation = value; }
        }

        private void ProposedValueSetter(List<Lease> receivedLeases){
            _proposedValue = new List<Lease>();

            for(int i = receivedLeases.Count - 1; i >= 1; i--){

                string tmName = receivedLeases[i].AssignedTransactionManager;

                Dictionary<string, bool> movedThisString = new Dictionary<string, bool>();

                foreach(string keyLease in receivedLeases[i].DadIntsKeys){
                    movedThisString.Add(keyLease, false);
                    int newPosition = i;

                    // for(int k = i - 1; k >= 0; k--){
                    //     if(!receivedLeases[k].AssignedTransactionManager.Equals(tmName) && receivedLeases[k].DadIntsKeys.Contains(keyLease)){
                    //         break;
                    //     }else{
                    //         newPosition = k;
                    //     }
                    // }

                    for(int k = i - 1; k >= 0; k--){
                        if(!receivedLeases[k].AssignedTransactionManager.Equals(tmName)){
                            if(receivedLeases[k].DadIntsKeys.Contains(keyLease)){
                                break;
                            }
                        }else{
                            newPosition = k;
                        }
                        
                    }



                    if(newPosition != i){
                        receivedLeases[newPosition].DadIntsKeys.Add(keyLease);
                        movedThisString[keyLease] = true;
                    }
                }

                foreach(string keyLease in movedThisString.Keys){
                    if(movedThisString[keyLease]){
                        receivedLeases[i].DadIntsKeys.Remove(keyLease);
                    }
                }
            }

            foreach(Lease lease in receivedLeases){
                if(lease.DadIntsKeys.Count != 0){
                    _proposedValue.Add(lease);
                }
            }
        }

        public void StartInstance(){

            int sentPrepareTimestamp = _writeTimestamp +_id;
            if(_isLeaderCurrentEpoch && _epoch != 1 && !_wasLeaderPreviousEpoch){ //in first epoch promise fase not needed

                PaxosMessageStruct prepareMessage = new PaxosMessageStruct(sentPrepareTimestamp, _epoch);
                
                List<PaxosMessageStruct> receivedPromises = new List<PaxosMessageStruct>();
                try{
                    receivedPromises = Task.Run(() =>{return _paxosClient.broadcastPrepareMessage(prepareMessage);}).Result;
                }catch(Exception e){
                    Console.WriteLine("Error: " + e.Message);
                }
                


                
                foreach(PaxosMessageStruct promise in receivedPromises){
                    Console.WriteLine("-OP" + _epoch + "- Received promise with write timestamp: " + promise.WriteTimestamp + " and epoch: " + promise.Epoch);
                    Console.WriteLine("and value:");
                    foreach(Lease lease in promise.Leases){

                        Console.WriteLine(lease.ToString());

                        Console.WriteLine();
                    }
                    Console.WriteLine();


                    //it will check for all received write timestamps:
                    //1. if all received write_ts are less than its own write_ts, it will adopt and send accept to its own value
                    //2. otherwise it will send accept to the value with the highest write_ts

                    lock(this){

                        if(promise.WriteTimestamp > _writeTimestamp){
                            Console.WriteLine("-OP" + _epoch + "-Received write timestamp greater than own write timestamp");
                            
                            _writeTimestamp = promise.WriteTimestamp;
                            _proposedValue = promise.Leases;
                            break;
                        }

                    }


                    
                }

            }

            if(sentPrepareTimestamp > _writeTimestamp){
                _writeTimestamp = sentPrepareTimestamp;
            }

            if(_isLeaderCurrentEpoch){
                //ACCEPT FASE:

                Console.WriteLine("-OP" + _epoch + "-Sending accept message with write timestamp: " + _writeTimestamp + " and epoch: " + _epoch);
                Console.WriteLine("and value:");
                foreach(Lease lease in _proposedValue){

                    Console.WriteLine(lease.ToString());

                    Console.WriteLine();
                }
                Console.WriteLine();


                PaxosMessageStruct acceptMessage = new PaxosMessageStruct(_writeTimestamp, _proposedValue, _epoch);

                try{
                    List<PaxosMessageStruct> receivedAccepted = Task.Run(() =>{return _paxosClient.broadcastAcceptMessage(acceptMessage);}).Result;
                }catch(Exception){

                }

                //because it is the leader, it doesnt make sense to wait for confirmation from itself
                _hasReceivedFinalConfirmation = true;

                Console.WriteLine("-OP" + _epoch + "-Value has been agreed upon");

            }else{
                //wait for accept fase to complete
                lock(this){
                    Monitor.Wait(this);
                }
            }

            Console.WriteLine("---------------- EPOCH NR: " + _epoch + " | ORDERING COMPLETE -----------------");
            Console.WriteLine();
            
        }

        public PaxosMessageStruct PrepareRequestHandler(PaxosMessageStruct paxosMessageStruct){
            Console.WriteLine("-OP" + _epoch + "-Received prepare request with write timestamp: " + paxosMessageStruct.WriteTimestamp + " and epoch: " + paxosMessageStruct.Epoch);

            lock(this){

                if(paxosMessageStruct.WriteTimestamp > _readTimestamp){
                    _readTimestamp = paxosMessageStruct.WriteTimestamp;
                    return new PaxosMessageStruct(_writeTimestamp, _proposedValue, _epoch);
                }

            }

            throw new ReadTimestampGreaterThanWriteTimestampException();

        }

        public PaxosMessageStruct AcceptRequestHandler(PaxosMessageStruct paxosMessageStruct){



            lock(this){
                if(paxosMessageStruct.WriteTimestamp >= _readTimestamp){
                    _writeTimestamp = paxosMessageStruct.WriteTimestamp;

                    _proposedValue = paxosMessageStruct.Leases;
                    Monitor.PulseAll(this);
                    return paxosMessageStruct;
                }
            }


            throw new ReadTimestampGreaterThanWriteTimestampException();

        }

        public void SentToTransactionManagersConfirmationHandler(){
            Console.WriteLine("-OP" + _epoch + "-Received confirmation from leader that value has been sent to transaction managers");
            _hasReceivedFinalConfirmation = true;
        }


    }
}