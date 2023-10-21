using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Grpc.Core;
using LeaseManager.src.paxos;
using LeaseManager.src.service.util;
using LeaseManager.src.paxos.exceptions;
using LeaseManager.src.service.exceptions;

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
    }
}