using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TransactionManager.src.state
{
    public class TransactionManagerState
    {

        public TransactionManagerState()
        {

        }

        public List<Common.DadInt> TransactionHandler(string clientId, List<string> keysToBeRead, List<Common.DadInt> dadIntsToBeWritten){
            List<Common.DadInt> returnedDadInts = new List<Common.DadInt>();

            Common.DadInt tempDadInt = new Common.DadInt("ola", 1);
            returnedDadInts.Add(tempDadInt);

            return returnedDadInts;
        }
    }
}