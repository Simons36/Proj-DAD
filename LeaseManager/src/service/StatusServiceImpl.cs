using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LeaseManager.src.paxos;

namespace LeaseManager.src.service
{
    public class StatusServiceImpl : StatusService.StatusServiceBase
    {
        PaxosImplementation _paxosClient;

        public StatusServiceImpl(PaxosImplementation paxosClient)
        {
            _paxosClient = paxosClient;
        }

        public override Task<StatusResponse> StatusCommand(RequestStatus request, Grpc.Core.ServerCallContext context)
        {
            _paxosClient.StatusCommandHandler();
            return Task.FromResult(new StatusResponse());
        }
    }
}