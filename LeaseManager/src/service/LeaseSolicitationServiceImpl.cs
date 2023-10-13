using LeaseManager.src.paxos;
using Grpc.Core;


namespace LeaseManager.src.service {

    public class LeaseSolicitationServiceImpl : LeaseSolicitationService.LeaseSolicitationServiceBase
    {
        
        private PaxosImplementation _paxos;

        public LeaseSolicitationServiceImpl(PaxosImplementation paxos) : base()
        {
            _paxos = paxos;
        }

        public override Task<LeaseReply> LeaseSolicitation(LeaseRequest request, ServerCallContext context)
        {
            _paxos.TMRequestHandler();
            return Task.FromResult(new LeaseReply());
        }
    }
}