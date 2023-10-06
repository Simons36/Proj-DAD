using Grpc.Core;
using Grpc.Net.Client;
using System;
using Common;
using System.Threading.Tasks;
using TransactionManager.src.service;
using TransactionManager.src;
using System.Security.AccessControl;
using TransactionManager.src.state;

namespace TransactionManager
{
    public class TransactionManager
    {
        private static void Main(string[] args)
        {
            string name = "", thisUrl = "";
            int timeslotNumber = 0, duration = 0, id = 0;
            TimeOnly startingTime = new TimeOnly();
            List<string> tMsUrls = new List<string>();

            Console.WriteLine("Starting DADKTV transaction manager process. Received arguments:");

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
                        thisUrl = args[i + 1];
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

            //TransactionManagerServiceImpl transactionService = new TransactionManagerServiceImpl(thisUrl, tMsUrls);
            TransactionManagerState state = new TransactionManagerState();
            ClientServiceImpl clientService = new ClientServiceImpl(state);

            startServer(thisUrl, clientService);


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

        private static void startServer(string thisUrl, ClientServiceImpl clientService)
        {
            string[] splitString = thisUrl.Split(":");

            string hostname = splitString[1];
            hostname = hostname.Remove(0, 2);

            int port = int.Parse(splitString[2]);

            Console.WriteLine("Starting server on " + hostname);
            Console.WriteLine("Starting server on " + port);

            try{
                Server server = new Server
                {
                    Services = { ClientService.BindService(clientService) },
                    Ports = { new ServerPort(hostname, port, ServerCredentials.Insecure) }
                };
                server.Start();
            }catch(Exception e){
                Console.WriteLine("Error starting server: " + e.Message);
            }


            Console.WriteLine("Server listening on " + hostname + ":" + port);
            while (true);
        }
    }
}