using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Grpc.Core;

namespace LeaseManager.src.service.exceptions
{
    public class ReadTimestampGreaterThanWriteTimestampRpcException : RpcException
    {
        public ReadTimestampGreaterThanWriteTimestampRpcException() : base(new Status(StatusCode.FailedPrecondition, "Read timestamp greater than write timestamp")){}
    }
}