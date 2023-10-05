using Grpc.Core;
using Grpc.Net.Client;
using System;
using L;
using System.Threading.Tasks;

namespace TransactionManager
{
    public class TransactionManager : TransactionManagerService.TransactionManagerServiceBase
    {
        private GrpcChannel channel;
        private HashSet<DadInt> _dadInts = new HashSet<DadInt>();
        private Lease _lease = new Lease();

        public TransactionManager() { }

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

        private static void Main(string[] args)
        {
            Console.WriteLine("Hello, World! transactionManager");
            foreach (string arg in args)
            {
                Console.WriteLine(arg);
            }
            Console.ReadLine();

        }

        public void Run(string[] args)
        {
            Main(args);
        }
    }
}