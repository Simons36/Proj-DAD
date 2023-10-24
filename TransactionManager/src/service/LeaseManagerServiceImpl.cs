using System.Collections;
using System.Net;
using Common.structs;
using Common.util;
using Grpc.Core;
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

            Dictionary<LeaseSolicitationService.LeaseSolicitationServiceClient, Task<LeaseReply>> tasksPerLeaseManager = 
            new Dictionary<LeaseSolicitationService.LeaseSolicitationServiceClient, Task<LeaseReply>>();

            foreach(LeaseSolicitationService.LeaseSolicitationServiceClient client in _leaseManagersClients){
                tasksPerLeaseManager.Add(client, client.LeaseSolicitationAsync(request).ResponseAsync);
            }

            List<LeaseReply> responseList = new List<LeaseReply>();
            List<LeaseSolicitationService.LeaseSolicitationServiceClient> clientsToRemove = new List<LeaseSolicitationService.LeaseSolicitationServiceClient>();

            foreach(LeaseSolicitationService.LeaseSolicitationServiceClient key in tasksPerLeaseManager.Keys){
                try{
                    responseList.Add(await tasksPerLeaseManager[key]);
                }catch(RpcException){
                    Console.WriteLine("An error ocurred in one of the requests to the lease managers, removing it from further communications");
                    clientsToRemove.Add(key);
                }
            }

            foreach(LeaseSolicitationService.LeaseSolicitationServiceClient client in clientsToRemove){
                _leaseManagersClients.Remove(client);
            }


            List<Lease> parsedResponse = new List<Lease>();

            foreach(LeaseReply leaseReply in responseList){
                if(leaseReply.Leases.Count > 0){
                    foreach(ProtoLease protoLeaseInside in leaseReply.Leases){
                        parsedResponse.Add(UtilMethods.parseProtoLeaseToLease(protoLeaseInside));
                    }
                    break;
                }
            }

            return parsedResponse;
        }

        public void WarnUnreceivedLeases(int epoch){
            UnreceivedLeasesWarningRequest request = new UnreceivedLeasesWarningRequest();
            request.Epoch = epoch;
            List<LeaseSolicitationService.LeaseSolicitationServiceClient> clientsToRemove = new List<LeaseSolicitationService.LeaseSolicitationServiceClient>();

            foreach(LeaseSolicitationService.LeaseSolicitationServiceClient client in _leaseManagersClients){
                try{
                    client.UnreceivedLeasesWarning(request);
                }catch(RpcException){
                    Console.WriteLine("An error ocurred in one of the requests to the lease managers, removing it from further communications");
                    clientsToRemove.Add(client);
                }
            }

            foreach(LeaseSolicitationService.LeaseSolicitationServiceClient client in clientsToRemove){
                _leaseManagersClients.Remove(client);
            }
        }
        
    }
}