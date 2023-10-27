using Grpc.Net.Client;
using Common.structs;
using Grpc.Core;

namespace Client.src.service
{
    public class ClientServiceImpl : ClientService.ClientServiceClient{

        private List<string> _tMsUrls;

        private int _id;

        private ClientService.ClientServiceClient _stub;

        private List<int> _crashedServers;

        private int _serverToConnect;

        private List<string> _leaseManagersUrls;

        public ClientServiceImpl(List<string> tMsUrls, int id, List<string> leaseManagersUrls){
            _tMsUrls = tMsUrls;
            _id = id;
            _leaseManagersUrls = leaseManagersUrls;
            _crashedServers = new List<int>();
            _serverToConnect = GetServerToConnect(_id);
            createStub();
        }

        private int GetServerToConnect(int _id){
            return (_id - 1) % _tMsUrls.Count;
        }

        private void createStub(){
            //if there are three servers, client 1 -> server 1, client 2 -> server 2, client 3 -> server 3, client 4 -> server 1, etc<

            //if it fails to connect to the server, it will try to connect to the next one
            //and so on, until it connects to one, and it will keep registering crashed servers
            //if crashed server list is the size of the total number of servers, it will throw an exception

            if(_crashedServers.Count == _tMsUrls.Count){
                throw new Exception("All servers are down");
            }

            GrpcChannel grpcChannel = GrpcChannel.ForAddress(_tMsUrls[_serverToConnect]);

            _stub = new ClientService.ClientServiceClient(grpcChannel);
        }


        public async Task<List<DadInt>> TxSubmit(string client, List<string> keysToRead, List<DadInt> dadIntsToWrite){
            List<ProtoDadInt> parsedDadInts = new List<ProtoDadInt>();

            foreach(DadInt unparsedDadInt in dadIntsToWrite){
                parsedDadInts.Add(new ProtoDadInt{ Key = unparsedDadInt.Key, Value = unparsedDadInt.Value});
            }
            
            List<ProtoDadInt> receivedList = new List<ProtoDadInt>();

            try{
                receivedList = (await _stub.TxSubmitAsync(new TxSubmitRequest { Client = client, ReadDads = { keysToRead }, WriteDads = { parsedDadInts } }))
                                             .DadInts.ToList();

            }catch(RpcException e){
                Console.WriteLine("Error: Transaction Manager is not available");
                _id++;
                _crashedServers.Add(_serverToConnect);
                _serverToConnect = GetServerToConnect(_id);
                createStub();
                throw e;
            }

            List<DadInt> commonDadInts = new List<DadInt>();
            foreach(ProtoDadInt dadInt in receivedList){
                commonDadInts.Add(new DadInt{ Key = dadInt.Key, Value = dadInt.Value});
            }

            return commonDadInts;
        }

        public void Status()
        {
            foreach(string url in _tMsUrls){
                try{
                    GrpcChannel grpcChannel = GrpcChannel.ForAddress(url);
                    StatusService.StatusServiceClient stub = new StatusService.StatusServiceClient(grpcChannel);
                    stub.StatusCommand(new RequestStatus());
                }catch(RpcException e){

                }
            }

            //same thing for lease managers
            foreach(string url in _leaseManagersUrls){
                try{
                    GrpcChannel grpcChannel = GrpcChannel.ForAddress(url);
                    StatusService.StatusServiceClient stub = new StatusService.StatusServiceClient(grpcChannel);
                    stub.StatusCommand(new RequestStatus());
                }catch(RpcException e){

                }
            }
        }
        
    }
}