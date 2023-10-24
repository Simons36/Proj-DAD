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

            int currentEpoch = _paxos.RegisterLeaseRequest(requestedLease);

            List<Lease> receivedLeases = await _paxos.GetRequestReult(currentEpoch);

            //create lease reply from received leases
            LeaseReply reply = new LeaseReply();
            foreach(Lease lease in receivedLeases){
                reply.Leases.Add(UtilMethods.parseLeaseToProtoLease(lease));
            }
            reply.Epoch = currentEpoch;

            return reply;
        }

        public override Task<UnreceivedLeasesWarningReply> UnreceivedLeasesWarning(UnreceivedLeasesWarningRequest request, ServerCallContext context)
        {
            _paxos.WarningUnreceivedLeasesHandler(request.Epoch);
            return Task.FromResult(new UnreceivedLeasesWarningReply());
        }
    }
}