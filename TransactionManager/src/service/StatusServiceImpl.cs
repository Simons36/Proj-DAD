using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TransactionManager.src.state;

namespace TransactionManager.src.service
{
    public class StatusServiceImpl : StatusService.StatusServiceBase
    {
        TransactionManagerState _transactionManagerState;

        public StatusServiceImpl(TransactionManagerState transactionManagerState)
        {
            _transactionManagerState = transactionManagerState;
        }

        public override Task<StatusResponse> StatusCommand(RequestStatus request, Grpc.Core.ServerCallContext context)
        {
            _transactionManagerState.StatusCommandHandler();
            return Task.FromResult(new StatusResponse());
        }
    }
}