namespace Client{
    public class Client{
        private static void Main(string[] args)
        {
            Console.WriteLine("Hello, World! client " + args.Length.ToString() );

            foreach (string arg in args)
            {
                Console.WriteLine(arg);
            }


            Console.ReadLine();
        }
    }

}
