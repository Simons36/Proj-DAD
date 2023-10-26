using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Common.structs;
using TransactionManager.src.state;
using Common.util;

namespace TransactionManager.src.service
{
    public class TransactionManagerInternalServiceServer : TransactionManagerInternalService.TransactionManagerInternalServiceBase{

        private TransactionManagerState _state;

        public TransactionManagerInternalServiceServer(TransactionManagerState state)
        {
            _state = state;
        }

        public override Task<ExecutedTransactionResponse> ExecutedTransaction(ExecutedTransactionRequest request, Grpc.Core.ServerCallContext context)
        {
            Console.WriteLine("Received information about executed transaction");

            List<DadInt> dadInts = new List<DadInt>();
            foreach(ProtoDadInt dadIntProto in request.DadIntsWritten){
                dadInts.Add(UtilMethods.parseProtoDadInt(dadIntProto));
            }

            Task.Run(() => _state.ReceivedTransactionExecutionHandler(request.TransactionKeys.ToList(), 
                                                                         request.GaveUpLeasesOnThisKey.ToList(), 
                                                                                                 dadInts));
            return Task.FromResult(new ExecutedTransactionResponse());
        }
    }
}