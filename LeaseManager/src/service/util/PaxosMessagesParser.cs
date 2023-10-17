using Common.structs;
using LeaseManager.src.paxos;
using Common.util;

namespace LeaseManager.src.service.util
{
    public static class PaxosMessagesParser
    {

        public static PaxosMessageStruct ParsePrepareMessageToPaxosMessageStruct(PrepareMessage prepareMessage){
                
            return new PaxosMessageStruct(prepareMessage.WriteTimestamp, prepareMessage.Epoch);
    
        }

        public static PrepareMessage ParsePaxosMessageStructToPrepareMessage(PaxosMessageStruct paxosMessageStruct){
            
            return new PrepareMessage{
                WriteTimestamp = paxosMessageStruct.WriteTimestamp,
                Epoch = paxosMessageStruct.Epoch
            };

        }

        public static PaxosMessageStruct ParsePromiseMessageToPaxosMessageStruct(PromiseMessage promiseMessage){

            List<Lease> leases = new List<Lease>();

            foreach(ProtoLease protoLease in promiseMessage.Leases){
                leases.Add(UtilMethods.parseProtoLeaseToLease(protoLease));
            }
            
            return new PaxosMessageStruct(promiseMessage.WriteTimestamp, leases, promiseMessage.Epoch);

        }

        public static PromiseMessage ParsePaxosMessageStructToPromiseMessage(PaxosMessageStruct paxosMessageStruct){

            List<ProtoLease> protoLeases = new List<ProtoLease>();

            foreach(Lease lease in paxosMessageStruct.Leases){
                protoLeases.Add(UtilMethods.parseLeaseToProtoLease(lease));
            }

            return new PromiseMessage{
                WriteTimestamp = paxosMessageStruct.WriteTimestamp,
                Leases = {protoLeases},
                Epoch = paxosMessageStruct.Epoch
            };

        }

        public static AcceptMessage ParsePaxosMessageStructToAcceptMessage(PaxosMessageStruct paxosMessageStruct){
                
            List<ProtoLease> protoLeases = new List<ProtoLease>();

            foreach(Lease lease in paxosMessageStruct.Leases){
                protoLeases.Add(UtilMethods.parseLeaseToProtoLease(lease));
            }

            return new AcceptMessage{
                WriteTimestamp = paxosMessageStruct.WriteTimestamp,
                Leases = {protoLeases},
                Epoch = paxosMessageStruct.Epoch
            };
    
        }

        public static PaxosMessageStruct ParseAcceptedMessageToPaxosMessageStruct(AcceptedMessage acceptedMessage){

            List<Lease> leases = new List<Lease>();

            foreach(ProtoLease protoLease in acceptedMessage.Leases){
                leases.Add(UtilMethods.parseProtoLeaseToLease(protoLease));
            }
            
            return new PaxosMessageStruct(acceptedMessage.WriteTimestamp, leases, acceptedMessage.Epoch);

        }

        public static PaxosMessageStruct ParseAcceptMessageToPaxosMessageStruct(AcceptMessage acceptMessage){

            List<Lease> leases = new List<Lease>();

            foreach(ProtoLease protoLease in acceptMessage.Leases){
                leases.Add(UtilMethods.parseProtoLeaseToLease(protoLease));
            }
            
            return new PaxosMessageStruct(acceptMessage.WriteTimestamp, leases, acceptMessage.Epoch);

        }

        public static AcceptedMessage ParsePaxosMessageStructToAcceptedMessage(PaxosMessageStruct paxosMessageStruct){
                
            List<ProtoLease> protoLeases = new List<ProtoLease>();

            foreach(Lease lease in paxosMessageStruct.Leases){
                protoLeases.Add(UtilMethods.parseLeaseToProtoLease(lease));
            }

            return new AcceptedMessage{
                WriteTimestamp = paxosMessageStruct.WriteTimestamp,
                Leases = {protoLeases},
                Epoch = paxosMessageStruct.Epoch
            };
        }
        
    }
}