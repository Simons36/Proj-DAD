using Grpc.Net.Client;
using Common.util;
using System.Net.Cache;
using Grpc.Core;
using Common.structs;
using TransactionManager.src.state.utilityClasses;
using TransactionManager.src.structs;

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

        public void CommunicateTransactionHasBeenDone(List<DadInt> dadIntsWritten, List<LeaseTransactionManagerStruct> freedLeases){
            List<ProtoDadInt> listProtoDadInts = new List<ProtoDadInt>();

            foreach(DadInt dadInt in dadIntsWritten){
                listProtoDadInts.Add(UtilMethods.parseCommonDadInt(dadInt));
            }

            List<TransactionManagerLease> protoFreedLeases = new List<TransactionManagerLease>();

            foreach(LeaseTransactionManagerStruct lease in freedLeases){
                protoFreedLeases.Add(new TransactionManagerLease{
                    Key = lease.Key,
                    Index = lease.Index
                });
            }

            ExecutedTransactionRequest request = new ExecutedTransactionRequest{
                DadIntsWritten = { listProtoDadInts },
                FreedLeases = { protoFreedLeases }
                
            };

            
            

            // try{

            //     Parallel.ForEach(_tMsClients, client =>
            //     {
                foreach(TransactionManagerInternalService.TransactionManagerInternalServiceClient client in _tMsClients){
                    try
                    {
                        client.ExecutedTransaction(request);
                    }
                    catch (RpcException e)
                    {
                        Console.WriteLine("Error" + e.Message);
                    }
                }
                // });

            // }
            // catch (AggregateException)
            // {
            //     Console.WriteLine("One or more tasks encountered exceptions");
            // }



        }
    }
}