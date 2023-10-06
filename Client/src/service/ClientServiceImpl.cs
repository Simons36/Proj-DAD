using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Grpc.Net.Client;
using Client.src.service.util;

namespace Client.src.service
{
    public class ClientServiceImpl : ClientService.ClientServiceClient{

        private List<string> _tMsUrls;

        private int _id;

        private ClientService.ClientServiceClient _stub;

        private List<int> _crashedServers;

        private int _serverToConnect;

        public ClientServiceImpl(List<string> tMsUrls, int id){
            _tMsUrls = tMsUrls;
            _id = id;
            _crashedServers = new List<int>();
            _serverToConnect = (_id - 1) % _tMsUrls.Count;
            createStub();
        }

        private void createStub(){
            //if there are three servers, client 1 -> server 1, client 2 -> server 2, client 3 -> server 3, client 4 -> server 1, etc<

            //if it fails to connect to the server, it will try to connect to the next one
            //and so on, until it connects to one, and it will keep registering crashed servers
            //if crashed server list is the size of the total number of servers, it will throw an exception

            GrpcChannel grpcChannel = GrpcChannel.ForAddress(_tMsUrls[_serverToConnect]);

            _stub = new ClientService.ClientServiceClient(grpcChannel);
        }


        public List<Common.DadInt> TxSubmit(string client, List<string> keysToRead, List<Common.DadInt> dadIntsToWrite){
            List<DadInt> parsedDadInts = new List<DadInt>();

            foreach(Common.DadInt unparsedDadInt in dadIntsToWrite){
                parsedDadInts.Add(new DadInt{ Key = unparsedDadInt.Key, Value = unparsedDadInt.Value});
            }

            List<DadInt> receivedList = _stub.TxSubmit(new TxSubmitRequest { Client = client, ReadDads = { keysToRead }, WriteDads = { parsedDadInts } })
                                             .DadInts.ToList();

            List<Common.DadInt> commonDadInts = new List<Common.DadInt>();
            foreach(DadInt dadInt in receivedList){
                commonDadInts.Add(new Common.DadInt{ Key = dadInt.Key, Value = dadInt.Value});
            }

            return commonDadInts;
        }

        public bool Status()
        {
            /* estabelecer comunicação com transaction managers e pedir status (TO DO) */

            return true;
        }
        
    }
}