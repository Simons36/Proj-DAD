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

        private bool _isServiceEnabled = true;

        public ClientServiceImpl(TransactionManagerState state){
            _state = state;

        }

        public void DisableService(){
            _isServiceEnabled = false;
        }

        public override async Task<TxSubmitReply> TxSubmit(TxSubmitRequest request, ServerCallContext context){
            if(!_isServiceEnabled){
                throw new RpcException(new Status(StatusCode.Unavailable, "Service is disabled"));
            }

            try{
                Console.WriteLine("Received TxSubmit request from client " + request.Client + " with " + request.ReadDads.Count + " dads to be read and " + request.WriteDads.Count + " dads to be written");

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