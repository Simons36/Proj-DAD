using Client.src.state;

namespace Client{
    public class Client{
        private static void Main(string[] args)
        {
            //prefixes of arguments received
            string namePrefix = "-n", scriptPrefix = "-e", timeslotNumberPrefix = "-nr", durationPrefix = "-d",
                   startingTimePrefix = "-t", tMsUrlPrefix = "-u";

            //parameters parsed from arguments received to create client state
            string name = "", script = "";
            int timeslotNumber = 0, duration = 0;
            TimeOnly startingTime = new TimeOnly();
            List<string> tMsUrls = new List<string>();

            Console.WriteLine("Starting DADKTV client process. Received arguments:");

            for (int i = 0; i < args.Length; i++){
                string arg = args[i];

                Console.Write(arg + " ");

                switch (arg)
                {
                    case "-n":
                        name = args[i + 1];
                        break;

                    case "-e":
                        script = args[i + 1];
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

                    case "-u":
                        for (int k = i + 1; (k < args.Length) && (isNotPrefix(args[k])); k++)
                        {
                            tMsUrls.Add(args[k]);
                        }
                        break;

                    default:
                        break;
                }
            }

            Console.WriteLine("");

            ClientState clientState = new ClientState(name, script, timeslotNumber, duration, startingTime, tMsUrls);

            Console.ReadKey();
        }

        private static bool isNotPrefix(string arg)
        {
            if(arg.Equals("-n") || arg.Equals("-e") || arg.Equals("-nr") || arg.Equals("-d") || arg.Equals("-t") || arg.Equals("-u"))
            {
                return false;
            }
            return true;
        }

    }

}
