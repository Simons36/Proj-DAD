using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Common.structs
{
    public struct DadInt
    {
        private string _key;
        private int _value;

        public DadInt(string Key, int Value) {
            _key = Key;
            _value = Value;
        }

        public string Key
        {
            get { return _key; }
            set { _key = value; }
        }

        public int Value
        {
            get { return _value; }
            set { _value = value; }
        }

        public override string ToString(){
            return "<" + _key + "> " + _value.ToString();
        }
    }
}