using Grpc.Core;
using Grpc.Net.Client;
using System;
using Common;
using System.Threading.Tasks;
using TransactionManager.src.service;
using TransactionManager.src;
using System.Security.AccessControl;
using TransactionManager.src.state;
using Common.structs;

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
            List<string> leaseManagersUrls = new List<string>();

            Console.WriteLine("Starting DADKTV transaction manager process.\nReceived arguments:\n");

            for (int i = 0; i < args.Length; i++)
            {
                string arg = args[i];

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

                    case "--lease-urls":
                        for(int k = i + 1; (k < args.Length) && (isNotPrefix(args[k])); k++){
                            leaseManagersUrls.Add(args[k]);
                        }
                        break;

                    default:
                        break;
                }
            }

            WriteArguments(name, thisUrl, startingTime, tMsUrls, leaseManagersUrls);

            //TransactionManagerServiceImpl transactionService = new TransactionManagerServiceImpl(thisUrl, tMsUrls);
            LeaseManagerServiceImpl leaseService = new LeaseManagerServiceImpl(leaseManagersUrls);
            TransactionManagerInternalServiceClient transactionManagerInternalServiceClient = new TransactionManagerInternalServiceClient(tMsUrls, thisUrl);
            TransactionManagerState state = new TransactionManagerState(leaseService, name, startingTime, 
                                        duration, timeslotNumber, transactionManagerInternalServiceClient);
            ClientServiceImpl clientService = new ClientServiceImpl(state);
            TransactionManagerInternalServiceServer transactionManagerInternalServiceServer = new TransactionManagerInternalServiceServer(state);
            

            try{
                Server server = startServer(thisUrl, clientService, transactionManagerInternalServiceServer, state);

                state.StartTransactionManager();

                DisposeServer(clientService, transactionManagerInternalServiceServer);

            }catch(Exception e){
                Console.WriteLine("Error: " + e.Message);
            }

            Console.WriteLine("Press any key to stop the server...");
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
                arg.Equals("--lease-urls"))
            {
                return false;
            }
            return true;
        }

        private static Server startServer(string thisUrl, ClientServiceImpl clientService, TransactionManagerInternalServiceServer transactionManagerInternalServiceServer, TransactionManagerState transactionManagerState)
        {
            string[] splitString = thisUrl.Split(":");

            string hostname = splitString[1];
            hostname = hostname.Remove(0, 2); //remove the // from the hostname

            int port = int.Parse(splitString[2]);

            Server server;

            try{
                server = new Server
                {
                    Services = { ClientService.BindService(clientService), TransactionManagerInternalService.BindService(transactionManagerInternalServiceServer), StatusService.BindService(new StatusServiceImpl(transactionManagerState)) },
                    Ports = { new ServerPort(hostname, port, ServerCredentials.Insecure) }
                };
                server.Start();
            }catch(Exception){
                throw;
            }

            Console.WriteLine("Server listening on " + hostname + ":" + port);
            return server;
        }

        private static void DisposeServer(ClientServiceImpl clientService, TransactionManagerInternalServiceServer transactionManagerInternalServiceClient){
            clientService.DisableService();
            transactionManagerInternalServiceClient.DisableService();
        }

        private static void WriteArguments(string name, string thisUrl, TimeOnly startingTime, List<string> tMsUrls, List<string> leaseManagersUrls){
            Console.WriteLine("Name: " + name);
            Console.WriteLine("This server's url: " + thisUrl);
            Console.WriteLine();
            Console.WriteLine("Starting time: " + startingTime);
            Console.WriteLine();
            Console.WriteLine("Transaction Manager's urls: ");
            foreach (string url in tMsUrls)
                Console.WriteLine("  - " + url);
            Console.WriteLine();
            Console.WriteLine("Lease Manager's urls: ");
            foreach(string url in leaseManagersUrls)
                Console.WriteLine("  - " + url);
            Console.WriteLine();
        }
    }
}