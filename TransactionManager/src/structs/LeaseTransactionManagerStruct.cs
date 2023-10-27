using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TransactionManager.src.structs
{
    public class LeaseTransactionManagerStruct
    {

        private string _key;

        private int _index; //Corresponds to the number of the lease in the totally ordered lease list


        public LeaseTransactionManagerStruct(string key, int index){
            _key = key;
            _index = index;
        }

        public string Key
        {
            get { return _key; }
        }

        public int Index
        {
            get { return _index; }
            set { _index = value; }
        }
    }
}