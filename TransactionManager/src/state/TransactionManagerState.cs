using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Common.structs;

namespace TransactionManager.src.state
{
    public class TransactionManagerState
    {

        public TransactionManagerState()
        {

        }

        public List<DadInt> TransactionHandler(string clientId, List<string> keysToBeRead, List<DadInt> dadIntsToBeWritten){
            List<DadInt> returnedDadInts = new List<DadInt>();

            DadInt tempDadInt = new DadInt("ola", 1);
            returnedDadInts.Add(tempDadInt);

            return returnedDadInts;
        }
    }
}