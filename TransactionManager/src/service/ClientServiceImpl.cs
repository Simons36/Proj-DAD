using Grpc.Core;
using TransactionManager.src.state;
using Common.util;
using Common.structs;

namespace TransactionManager.src.service
{
    public class ClientServiceImpl : ClientService.ClientServiceBase
    {
        //this class is for communication with clients (it responds to client requests)

        private TransactionManagerState _state;

        public ClientServiceImpl(TransactionManagerState state){
            _state = state;
        }

        public override async Task<TxSubmitReply> TxSubmit(TxSubmitRequest request, ServerCallContext context){
            try{
                Console.WriteLine("Received TxSubmit request from client " + request.Client);

                string clientId = request.Client;
                List<string> keysToBeRead = request.ReadDads.ToList();
                List<ProtoDadInt> dadIntsToBeWritten = request.WriteDads.ToList();

                List<DadInt> newDadIntsToBeWritten = new List<DadInt>();

                foreach(ProtoDadInt protoDadInt in dadIntsToBeWritten){
                    newDadIntsToBeWritten.Add(UtilMethods.parseProtoDadInt(protoDadInt));
                }

                List<DadInt> returnedDadInts = await _state.TransactionHandler(clientId, keysToBeRead, newDadIntsToBeWritten);
                
                List<ProtoDadInt> newReturnedDadInts = new List<ProtoDadInt>();
                foreach(DadInt commonDadInt in returnedDadInts){
                    newReturnedDadInts.Add(UtilMethods.parseCommonDadInt(commonDadInt));
                }
                return new TxSubmitReply { DadInts = { newReturnedDadInts } };
            }catch(Exception e){
            }

            return new TxSubmitReply();

        }
    }
}