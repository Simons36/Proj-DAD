namespace LeaseManager
{
    public class LeaseManager
    {
        private static void Main(string[] args)
        {
            Console.WriteLine("Hello, World! leaseManager");
            string name = "";
            int timeslotNumber = 0,
                duration = 0;
            TimeOnly startingTime = new TimeOnly();
            List<string> leaseManagerUrls = new List<string>();

            // Extract and process the arguments
            for (int i = 0; i < args.Length; i++)
            {
                string arg = args[i];

                switch (arg)
                {
                    case "-n":
                        name = args[i + 1];
                        break;

                    case "-nr":
                        timeslotNumber = int.Parse(args[i + 1]);
                        break;

                    case "-d":
                        duration = int.Parse(args[i + 1]);
                        break;

                    case "-t":
                        startingTime = TimeOnly.Parse(args[i + 1]);
                        break;

                    case "-nl":
                        for (int k = i + 1; k < args.Length; k++)
                        {
                            string nextArg = args[k];
                            if (nextArg.StartsWith("http://"))
                            {
                                leaseManagerUrls.Add(nextArg);
                            }
                            else
                            {
                                // Break if the next argument does not start with "http://"
                                break;
                            }
                        }
                        break;

                    default:
                        break;
                }
            }

            // Display the extracted values for verification
            Console.WriteLine("\nExtracted values:");
            Console.WriteLine("Name: " + name);
            Console.WriteLine("Timeslot Number: " + timeslotNumber);
            Console.WriteLine("Duration: " + duration);
            Console.WriteLine("Starting Time: " + startingTime);
            Console.WriteLine("Lease Manager URLs:");
            foreach (var url in leaseManagerUrls)
            {
                Console.WriteLine(url);
            }
            Console.ReadLine();
        }
    }
}
