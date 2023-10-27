using LeaseManager.src.paxos;
using Grpc.Core;
using Common.structs;
using Common.util;

namespace LeaseManager.src.service {

    public class LeaseSolicitationServiceImpl : LeaseSolicitationService.LeaseSolicitationServiceBase
    {
        
        private PaxosImplementation _paxos;

        private bool _isServiceEnabled = true;

        public LeaseSolicitationServiceImpl(PaxosImplementation paxos) : base()
        {
            _paxos = paxos;
        }

        public void DisableService(){
            _isServiceEnabled = false;
        }

        public override async Task<LeaseReply> LeaseSolicitation(LeaseRequest request, ServerCallContext context)
        {
            if(!_isServiceEnabled){
                throw new RpcException(new Status(StatusCode.Unavailable, "Service is disabled"));
            }

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
    }
}