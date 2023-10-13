using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Common.structs
{
    public struct Lease
    {
        private string _transactionManagerName;
        private HashSet<string> _dadIntsKeys;

        public Lease(string transactionManagerName, List<DadInt> dadInts) { 
            _transactionManagerName = transactionManagerName;

            _dadIntsKeys = new HashSet<string>();

            foreach(DadInt dadInt in dadInts){
                _dadIntsKeys.Add(dadInt.Key);
            }
        }

        public void addDadInt(DadInt d)
        {
            _dadIntsKeys.Add(d.Key);
        }

        public string AssignedTransactionManager
        {
            get { return _transactionManagerName; }
            set { _transactionManagerName = value; }
        }

        public HashSet<string> DadIntsKeys
        {
            get { return _dadIntsKeys; }
            set { _dadIntsKeys = value; }
        }
    }
}