using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LeaseManager.src.service.exceptions
{
    public class LeaseManagerTimedOutException : Exception
    {
        public LeaseManagerTimedOutException(string typeOfRequest) : 
        base("Another Lease Manager timed out in request" + typeOfRequest){}
        
    }
}