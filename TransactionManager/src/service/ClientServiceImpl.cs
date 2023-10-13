using Grpc.Core;
using TransactionManager.src.state;
using Common.util;

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
            List<ProtoDadInt> dadIntsToBeWritten = request.WriteDads.ToList();

            List<Common.structs.DadInt> newDadIntsToBeWritten = new List<Common.structs.DadInt>();

            foreach(ProtoDadInt protoDadInt in dadIntsToBeWritten){
                newDadIntsToBeWritten.Add(UtilMethods.parseProtoDadInt(protoDadInt));
            }

            List<Common.structs.DadInt> returnedDadInts = _state.TransactionHandler(clientId, keysToBeRead, newDadIntsToBeWritten);

            List<ProtoDadInt> newReturnedDadInts = new List<ProtoDadInt>();
            foreach(Common.structs.DadInt commonDadInt in returnedDadInts){
                newReturnedDadInts.Add(UtilMethods.parseCommonDadInt(commonDadInt));
            }

            return Task.FromResult(new TxSubmitReply { DadInts = { newReturnedDadInts } });
        }
    }
}