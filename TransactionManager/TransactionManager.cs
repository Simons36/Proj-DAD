using System;
using static DInt.DadInt;

namespace TransactionManager
{
    public class TransactionManager
    {
        private HashSet<DadInt> _dadInts;

        public TransactionManager(){
            _dadInts = new HashSet<DadInt>();
        }

        public HashSet<DadInt> DadInts{
            get { return _dadInts; }
            set { _dadInts = value}
        }

        public void addDadInt(DadInt d){
            _dadInts.add(d);
        }

        private static void Main(string[] args)
        {
            Console.WriteLine("Hello, World! transactionManager");
            foreach (string arg in args)
            {
                Console.WriteLine(arg);
            }
            Console.ReadLine();

        }

        public void Run(string[] args)
        {
            Main(args);
        }
    }
}

