using Grpc.Net.Client;
using Common.util;
using System.Net.Cache;
using Grpc.Core;
using Common.structs;

namespace TransactionManager.src.service
{
    public class TransactionManagerInternalServiceClient : TransactionManagerInternalService.TransactionManagerInternalServiceClient
    {
        private List<TransactionManagerInternalService.TransactionManagerInternalServiceClient> _tMsClients;

        public TransactionManagerInternalServiceClient(List<string> tMsUrls, string thisUrl)
        {
            _tMsClients = new List<TransactionManagerInternalService.TransactionManagerInternalServiceClient>();

            SetupChannels(tMsUrls, thisUrl);
        }

        private void SetupChannels(List<string> tMsUrls, string thisUrl)
        {
            foreach(string url in tMsUrls){

                if(url == thisUrl){
                    continue;
                }

                try{
                    GrpcChannel channel = GrpcChannel.ForAddress(url);
                    _tMsClients.Add(new TransactionManagerInternalService.TransactionManagerInternalServiceClient(channel));
                }catch(IOException e){
                    Console.WriteLine("Could not connect to lease manager at " + url + ": " + e.Message);
                    continue;
                }catch(Exception e){
                    Console.WriteLine("Error: " + e.Message);
                    continue;
                }

            }
        }

        public async void CommunicateTransactionHasBeenDone(List<string> keysUsedInExecution, List<string> keysWithLeasesToGiveUpOn, List<DadInt> dadIntsToBeWritten){
            List<ProtoDadInt> listProtoDadInts = new List<ProtoDadInt>();

            Console.WriteLine();
            Console.WriteLine();
            Console.WriteLine();
            foreach(string str in keysUsedInExecution){
                Console.WriteLine("Passing on key " + str);
            }
            Console.WriteLine();
            foreach(string str in keysWithLeasesToGiveUpOn){
                Console.WriteLine("Giving up on key " + str);
            }
            Console.WriteLine();
            Console.WriteLine();
            Console.WriteLine();

            foreach(DadInt dadInt in dadIntsToBeWritten){
                listProtoDadInts.Add(UtilMethods.parseCommonDadInt(dadInt));
            }

            ExecutedTransactionRequest request = new ExecutedTransactionRequest{
                TransactionKeys = { keysUsedInExecution },
                GaveUpLeasesOnThisKey = { keysWithLeasesToGiveUpOn },
                DadIntsWritten = { listProtoDadInts }
            };

            try{
                List<Task<ExecutedTransactionResponse>> responseTask = new List<Task<ExecutedTransactionResponse>>();
                
                foreach(TransactionManagerInternalService.TransactionManagerInternalServiceClient client in _tMsClients){
                    responseTask.Add(client.ExecutedTransactionAsync(request).ResponseAsync);
                }

                await Task.WhenAll(responseTask);
                
            }catch(RpcException){
            }
                Console.WriteLine("HAHAHAHHAHAHHAHHAHAHHAHAHHA");

        }
    }
}