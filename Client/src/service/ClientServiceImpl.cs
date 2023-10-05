using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Grpc.Net.Client;

namespace Client.src.service
{
    public class ClientServiceImpl : ClientService.ClientServiceBase{

        private List<string> _tMsUrls;

        private int _id;

        private ClientService.ClientServiceClient _stub;

        public ClientServiceImpl(List<string> tMsUrls, int id){
            _tMsUrls = tMsUrls;
            _id = id;
            createStub();
        }

        private void createStub(){
            //if there are three servers, client 1 -> server 1, client 2 -> server 2, client 3 -> server 3, client 4 -> server 1, etc
            int serverToConnect = _id % _tMsUrls.Count; 

            GrpcChannel grpcChannel = GrpcChannel.ForAddress(_tMsUrls[serverToConnect]);

            _stub = new ClientService.ClientServiceClient(grpcChannel);
        }

        public List<DadInt> TxSubmit(string client, List<string> keys, List<DadInt> ds){
            /* estabelecer comunicação com transaction managers e pedir o submit (TO DO) */

            return new List<DadInt>();
        }

        public bool Status()
        {
            /* estabelecer comunicação com transaction managers e pedir status (TO DO) */

            return true;
        }
        
    }
}