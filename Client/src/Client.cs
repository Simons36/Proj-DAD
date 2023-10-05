using Grpc.Core;
using Grpc.Net.Client;
using Client.src.service;
using DInt;
using Client.src;

namespace Client
{
    public class Client
    {
        private static void Main(string[] args)
        {
            //parameters parsed from arguments received to create client state
            string name = "", script = "";
            int timeslotNumber = 0, duration = 0, id = 0;
            TimeOnly startingTime = new TimeOnly();
            List<string> tMsUrls = new List<string>();

            Console.WriteLine("Starting DADKTV client process. Received arguments:");

            for (int i = 0; i < args.Length; i++)
            {
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

                    case "-id":
                        id = int.Parse(args[i + 1]);
                        break;

                    default:
                        break;
                }
            }

            Console.WriteLine("");

            ClientServiceImpl clientService = new ClientServiceImpl(tMsUrls, id);
            ScriptRunner clientState = new ScriptRunner(name, script, timeslotNumber, duration, startingTime, clientService);

            Console.ReadKey();
        }

        private static bool isNotPrefix(string arg)
        {
            if (arg.Equals("-n") || arg.Equals("-e") || arg.Equals("-nr") || arg.Equals("-d") || arg.Equals("-t") || arg.Equals("-u"))
            {
                return false;
            }
            return true;
        }

        


        
    }

}