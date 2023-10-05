using DInt;

namespace L
{
    public class Lease
    {
        private string _key;
        private HashSet<DadInt> _dadInts;

        public Lease() { }

        public void addDadInt(DadInt d)
        {
            _dadInts.Add(d);
        }

        public string LeaseKey
        {
            get { return _key; }
            set { _key = value; }
        }

        public HashSet<DadInt> DadInts
        {
            get { return _dadInts; }
            set { _dadInts = value; }
        }
    }
}
