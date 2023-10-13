using System.Collections;
using System.Net;
using Common.structs;
using Common.util;
using Grpc.Net.Client;

namespace TransactionManager.src.service
{
    public class LeaseManagerServiceImpl : LeaseSolicitationService.LeaseSolicitationServiceClient
    {
        private List<LeaseSolicitationService.LeaseSolicitationServiceClient> _leaseManagersClients;

        public LeaseManagerServiceImpl(List<string> leaseManagersUrls) : base() {

            _leaseManagersClients = new List<LeaseSolicitationService.LeaseSolicitationServiceClient>();

            foreach(string url in leaseManagersUrls){
                
                try{
                    GrpcChannel channel = GrpcChannel.ForAddress(url);
                    _leaseManagersClients.Add(new LeaseSolicitationService.LeaseSolicitationServiceClient(channel));
                }catch(IOException e){
                    Console.WriteLine("Could not connect to lease manager at " + url + ": " + e.Message);
                    continue;
                }catch(Exception e){
                    Console.WriteLine("Error: " + e.Message);
                    continue;
                }

            }

        }

        public async Task<List<Lease>> LeaseSolicitation(Lease lease){

            LeaseRequest request = new LeaseRequest();
            ProtoLease protoLease = UtilMethods.parseLeaseToProtoLease(lease);

            request.RequestedLease = protoLease;

            List<Task<LeaseReply>> tasks = new List<Task<LeaseReply>>();

            List<LeaseSolicitationService.LeaseSolicitationServiceClient> clientsToRemove = new List<LeaseSolicitationService.LeaseSolicitationServiceClient>();
            foreach(LeaseSolicitationService.LeaseSolicitationServiceClient client in _leaseManagersClients){
                try{
                    tasks.Add(client.LeaseSolicitationAsync(request).ResponseAsync);

                }catch(IOException e){

                    Console.WriteLine("Could not connect to lease manager at " + client + ": " + e.Message);
                    clientsToRemove.Add(client);
                    continue;
                }
                //TODO: imlement rest
            }

            //remove unresponsive lease managers
            foreach(LeaseSolicitationService.LeaseSolicitationServiceClient client in clientsToRemove){
                _leaseManagersClients.Remove(client);
            }



            List<LeaseReply> responseList = new List<LeaseReply>();
            
            try{//wait for all responses
                responseList = (await Task.WhenAll(tasks)).ToList();
            }catch(Exception e){
                Console.WriteLine("Error: " + e.Message);
            }


            List<Lease> parsedResponse = new List<Lease>();

            //get the first non-empty response (not empty responses are the ones from the learners)
            foreach(LeaseReply reply in responseList){
                if(reply.Leases.Count > 0){
                    foreach(ProtoLease protoLeaseInside in reply.Leases){
                        parsedResponse.Add(UtilMethods.parseProtoLeaseToLease(protoLeaseInside));
                    }
                    break;
                }
            }

            return parsedResponse;
        }
        
    }
}