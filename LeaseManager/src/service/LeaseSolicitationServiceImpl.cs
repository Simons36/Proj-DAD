using LeaseManager.src.paxos;
using Grpc.Core;
using Common.structs;
using Common.util;

namespace LeaseManager.src.service {

    public class LeaseSolicitationServiceImpl : LeaseSolicitationService.LeaseSolicitationServiceBase
    {
        
        private PaxosImplementation _paxos;

        public LeaseSolicitationServiceImpl(PaxosImplementation paxos) : base()
        {
            _paxos = paxos;
        }

        public override async Task<LeaseReply> LeaseSolicitation(LeaseRequest request, ServerCallContext context)
        {
            Lease requestedLease = UtilMethods.parseProtoLeaseToLease(request.RequestedLease);

            List<Lease> receivedLeases = await _paxos.TMRequestHandler(requestedLease);

            Console.WriteLine("feuiwbfiuebfiuewb");

            //create lease reply from received leases
            LeaseReply reply = new LeaseReply();
            foreach(Lease lease in receivedLeases){
                reply.Leases.Add(UtilMethods.parseLeaseToProtoLease(lease));
            }

            return reply;
        }
    }
}