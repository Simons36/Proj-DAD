using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Client.src.exceptions
{
    public class InvalidStartingTimeException : Exception
    {
        public InvalidStartingTimeException(string message) : base(message)
        {
        }
    }
}