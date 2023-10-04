namespace LeaseManager{
    public class LeaseManager
    {
        private static void Main(string[] args)
        {
            Console.WriteLine("Hello, World! leaseManager");
            foreach(string arg in args)
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

