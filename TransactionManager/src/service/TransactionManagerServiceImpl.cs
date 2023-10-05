using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Grpc.Core;
using Grpc.Net.Client;
using L;

namespace TransactionManager.src.service
{
    public class TransactionManagerServiceImpl : TransactionManagerService.TransactionManagerServiceBase
    {
        private int _id;
        private List<string> _lMsUrls;
        private HashSet<DadInt> _dadInts = new HashSet<DadInt>();
        private Lease _lease = new Lease();
        private TransactionManagerService.TransactionManagerServiceClient _stub;

        public TransactionManagerServiceImpl(List<string> lMsUrls, int id) 
        {
            _lMsUrls = lMsUrls;
            _id = id;
            createStub();
        }

        private void createStub()
        {
            int serverToConnect = _id % _lMsUrls.Count;

            // ^ aqui n interessa pois temos de mandar request a todos os lease managers, só pus pois tive de fazer isto à pressa sorry guys

            GrpcChannel grpcChannel = GrpcChannel.ForAddress(_lMsUrls[serverToConnect]);

            _stub = new TransactionManagerService.TransactionManagerServiceClient(grpcChannel);
        }

        public HashSet<DadInt> TransactionDadInts
        {
            get { return _dadInts; }
            set { _dadInts = value; }
        }

        public Lease TransactionLease
        {
            get { return _lease; }
            set { _lease = value; }
        }

        public void transactionAddDadInt(DadInt d)
        {
            _dadInts.Add(d);
        }

        public override Task<TxSubmitReply> TxSubmit(
            TxSubmitRequest request, ServerCallContext context)
        {
            return Task.FromResult(TxSub(request));
        }

        public TxSubmitReply TxSub(TxSubmitRequest request)
        {
            // fazer lógica (TO DO)

            TxSubmitReply reply = new TxSubmitReply();

            return reply;
        }

        public override Task<StatusReply> Status(
            StatusRequest request, ServerCallContext context)
        {
            return Task.FromResult(State(request));
        }

        public StatusReply State(StatusRequest request)
        {
            // fazer lógica (TO DO)

            StatusReply reply = new StatusReply();

            return reply;
        }

        public void LeaseSolicitation(HashSet<DadInt> ds)
        {
            // estabelecer comunicação com lease managers e pedir lease (TO DO)
        }
    }
}
