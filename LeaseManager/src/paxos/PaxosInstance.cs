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

        private PaxosInternalServiceClient _paxosClient;

        public PaxosInstance(int id, int epoch, bool isLeaderCurrentEpoch, bool wasLeaderPreviousEpoch, List<Lease> proposedValue, PaxosInternalServiceClient paxosClient){
            _id = id;
            _epoch = epoch;
            _isLeaderCurrentEpoch = isLeaderCurrentEpoch;
            _wasLeaderPreviousEpoch = wasLeaderPreviousEpoch;
            _writeTimestamp = 0;
            _readTimestamp = 0;

            _proposedValue = proposedValue;
            _paxosClient = paxosClient;

            //TODO: REMOVE THIS
            Lease testLease = new Lease("test" + id, new List<DadInt>());
            _proposedValue.Add(testLease);
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

        public async void StartInstance(){

            if(_isLeaderCurrentEpoch && _epoch != 0 && !_wasLeaderPreviousEpoch){ //in first epoch promise fase not needed

                PaxosMessageStruct prepareMessage = new PaxosMessageStruct(_writeTimestamp + _id, _epoch);

                _writeTimestamp = _writeTimestamp + _id;

                List<PaxosMessageStruct> receivedPromises = await Task.Run(() =>{return _paxosClient.broadcastPrepareMessage(prepareMessage);});
                
                foreach(PaxosMessageStruct promise in receivedPromises){
                    Console.WriteLine("Received promise with write timestamp: " + promise.WriteTimestamp + " and epoch: " + promise.Epoch);
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
                            Console.WriteLine("Received write timestamp greater than own write timestamp");
                            
                            _writeTimestamp = promise.WriteTimestamp;
                            _proposedValue = promise.Leases;
                            break;
                        }

                    }

                    
                }

            }

            if(_isLeaderCurrentEpoch){
                //ACCEPT FASE:

                Console.WriteLine("Sending accept message with write timestamp: " + _writeTimestamp + " and epoch: " + _epoch);
                Console.WriteLine("and value:");
                foreach(Lease lease in _proposedValue){

                    Console.WriteLine(lease.ToString());

                    Console.WriteLine();
                }
                Console.WriteLine();


                PaxosMessageStruct acceptMessage = new PaxosMessageStruct(_writeTimestamp, _proposedValue, _epoch);

                List<PaxosMessageStruct> receivedAccepted = await Task.Run(() =>{return _paxosClient.broadcastAcceptMessage(acceptMessage);});

                Console.WriteLine("Value has been agreed upon");

            }

            
        }

        public PaxosMessageStruct PrepareRequestHandler(PaxosMessageStruct paxosMessageStruct){
            Console.WriteLine("Received prepare request with write timestamp: " + paxosMessageStruct.WriteTimestamp + " and epoch: " + paxosMessageStruct.Epoch);

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
                _writeTimestamp = paxosMessageStruct.WriteTimestamp;

                _proposedValue = paxosMessageStruct.Leases;
            }

            return paxosMessageStruct;
        }


    }
}