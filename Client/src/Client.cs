using Grpc.Core;
using Grpc.Net.Client;
using Client.src.service;
using Common;
using Client.src;
using Common.exceptions;

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

                switch (arg)
                {
                    case "-n":
                        Console.WriteLine();
                        name = args[i + 1];

                        Console.WriteLine("Name: " + name);
                        break;

                    case "-e":
                        script = args[i + 1];

                        Console.WriteLine("Script: " + script);
                        break;

                    case "-nr":
                        Console.WriteLine();
                        timeslotNumber = int.Parse(args[i + 1]);

                        Console.WriteLine("Number of timeslots: " + timeslotNumber);
                        break;

                    case "-d":
                        duration = int.Parse(args[i + 1]);

                        Console.WriteLine("Duration of each time slot (ms): " + duration);
                        break;

                    case "-t":
                        startingTime = TimeOnly.Parse(args[i + 1]);

                        Console.WriteLine("Starting time: " + startingTime);
                        break;

                    case "-u":
                        Console.WriteLine();
                        Console.WriteLine("Transaction Managers' URLs:");
                        for (int k = i + 1; (k < args.Length) && (isNotPrefix(args[k])); k++)
                        {
                            Console.WriteLine("  - " + args[k]);
                            tMsUrls.Add(args[k]);
                        }
                        break;

                    case "-id":
                        Console.WriteLine();
                        id = int.Parse(args[i + 1]);

                        Console.WriteLine("Client ID (for internal use): " + id);
                        break;

                    default:
                        break;
                }
            }

            Console.WriteLine();

            ClientServiceImpl clientService = new ClientServiceImpl(tMsUrls, id);
            ScriptRunner scriptRunner = new ScriptRunner(name, /*script*/ "DADTKV_client_script_sample.txt", timeslotNumber, duration, startingTime, clientService);

            try{
                scriptRunner.RunScript();
            }catch(InvalidStartingTimeException e){
                Console.WriteLine(e.Message);
            }catch(Exception e){
                Console.WriteLine(e.Message);
            }

            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
        }

        private static bool isNotPrefix(string arg)
        {
            if (arg.Equals("-n")  || 
                arg.Equals("-e")  || 
                arg.Equals("-nr") || 
                arg.Equals("-d")  || 
                arg.Equals("-t")  || 
                arg.Equals("-u")  || 
                arg.Equals("-ul") || 
                arg.Equals("-id") ) return false;
            
            return true;
        }

        


        
    }

}