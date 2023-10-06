using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Grpc.Core;
using TransactionManager.src.service.util;
using TransactionManager.src.state;

namespace TransactionManager.src.service
{
    public class ClientServiceImpl : ClientService.ClientServiceBase
    {
        //this class is for communication with clients (it responds to client requests)

        private TransactionManagerState _state;

        public ClientServiceImpl(TransactionManagerState state){
            _state = state;
        }

        public override Task<TxSubmitReply> TxSubmit(TxSubmitRequest request, ServerCallContext context){
            Console.WriteLine("Received TxSubmit request from client " + request.Client);

            string clientId = request.Client;
            List<string> keysToBeRead = request.ReadDads.ToList();
            List<DadInt> dadIntsToBeWritten = request.WriteDads.ToList();

            List<Common.DadInt> newDadIntsToBeWritten = new List<Common.DadInt>();

            foreach(DadInt protoDadInt in dadIntsToBeWritten){
                newDadIntsToBeWritten.Add(DadIntParser.parseProtoDadInt(protoDadInt));
            }

            List<Common.DadInt> returnedDadInts = _state.TransactionHandler(clientId, keysToBeRead, newDadIntsToBeWritten);

            List<DadInt> newReturnedDadInts = new List<DadInt>();
            foreach(Common.DadInt commonDadInt in returnedDadInts){
                newReturnedDadInts.Add(DadIntParser.parseCommonDadInt(commonDadInt));
            }

            return Task.FromResult(new TxSubmitReply { DadInts = { newReturnedDadInts } });
        }
    }
}