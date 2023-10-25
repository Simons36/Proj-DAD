using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Grpc.Core;
using LeaseManager.src.paxos;
using LeaseManager.src.service.util;
using LeaseManager.src.paxos.exceptions;
using LeaseManager.src.service.exceptions;
using Common.structs;
using Common.util;


namespace LeaseManager.src.service
{
    public class PaxosInternalServiceServer : PaxosInternalService.PaxosInternalServiceBase
    {
        private PaxosImplementation _paxosClient;

        public PaxosInternalServiceServer(PaxosImplementation paxosClient){
            _paxosClient = paxosClient;
        }

        public override Task<PromiseMessage> Prepare(PrepareMessage request, ServerCallContext context)
        {
            try{
                PaxosMessageStruct promiseMessage = _paxosClient.PrepareRequestHandler(
                PaxosMessagesParser.ParsePrepareMessageToPaxosMessageStruct(request));
                return Task.FromResult(PaxosMessagesParser.ParsePaxosMessageStructToPromiseMessage(promiseMessage));

            }catch (ReadTimestampGreaterThanWriteTimestampException){
                throw new ReadTimestampGreaterThanWriteTimestampRpcException();
            }
            
        }

        public override Task<AcceptedMessage> Accept(AcceptMessage request, ServerCallContext context)
        {
            try{
                PaxosMessageStruct acceptedMessage = _paxosClient.AcceptRequestHandler(
                    PaxosMessagesParser.ParseAcceptMessageToPaxosMessageStruct(request));

                return Task.FromResult(PaxosMessagesParser.ParsePaxosMessageStructToAcceptedMessage(acceptedMessage));

            }catch (ReadTimestampGreaterThanWriteTimestampException){
                throw new ReadTimestampGreaterThanWriteTimestampRpcException();
            }
            
        }

        public override Task<EmptyMessage> SentToTransactionManagersConfirmation(LeaseReplySent request, ServerCallContext context){
            //parse request proto leases to leases
            List<Lease> leases = new List<Lease>();
            foreach(ProtoLease leaseProto in request.Leases){
                leases.Add(UtilMethods.parseProtoLeaseToLease(leaseProto));
            }
            _paxosClient.SentToTransactionManagersConfirmationHandler(request.Epoch, leases);
            return Task.FromResult(new EmptyMessage());
        }

        public override Task<CheckConfirmationReceivedReply> CheckConfirmationReceived(CheckConfirmationReceivedRequest request, ServerCallContext context){
            bool confirmationReceived = _paxosClient.CheckConfirmationReceivedHandler(request.Epoch);
            return Task.FromResult(new CheckConfirmationReceivedReply{ConfirmationReceived = confirmationReceived});
        }
    }
}