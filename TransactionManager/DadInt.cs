﻿namespace DInt
{
    public class DadInt
    {
        private string _key;
        private int _value;

        public DadInt() { }

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
    }
}
