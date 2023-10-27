using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LeaseManager.src.paxos.exceptions
{
    public class ReadTimestampGreaterThanWriteTimestampException : Exception
    {
        public ReadTimestampGreaterThanWriteTimestampException() : base(){}
    
    }
}