using System.Data;
using System.Data.Common;
using Grpc.Core;
using LeaseManager.src.paxos;
using LeaseManager.src.service;

namespace LeaseManager.src
{
    public class LeaseManager
    {
        private static void Main(string[] args)
        {
            string name = "";
            int timeslotNumber = 0,
                duration = 0;
            string currLM = "";
            TimeOnly startingTime = new TimeOnly();
            Dictionary<string, int> leaseManagerNameToId = new Dictionary<string, int>();
            Dictionary<string, string> leaseManagerNameToUrl = new Dictionary<string, string>();

            int crashingTimeSlot = -1;

            //map of suspected lease managers
            Dictionary<string, List<int>> suspectedServers = new Dictionary<string, List<int>>();

            // Extract and process the arguments
            for (int i = 0; i < args.Length; i++)
            {
                string arg = args[i];

                switch (arg)
                {   
                    case "-n":
                        name = args[i + 1];
                        break;

                    case "-e":
                        currLM = args[i + 1];
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

                    case "--lease-urls":
                        for (int k = i + 1; k < args.Length; k += 2)
                        {

                            string leaseName = args[k];
                            string leaseUrl = args[k + 1];
                            if (isNotPrefix(leaseName))
                            {
                                leaseManagerNameToUrl.Add(leaseName, leaseUrl);
                                leaseManagerNameToId.Add(leaseName, leaseManagerNameToUrl.Count);
                            }
                            else
                            {
                                // Break if the next argument is a prefix
                                break;
                            }
                        }
                        break;
                    
                    case "-s":

                        string key = args[i + 2];
                        int timeSlot = int.Parse(args[i + 1]);

                        //if doesnt contain key, add it and initialize list
                        if(!suspectedServers.ContainsKey(key)){
                            suspectedServers.Add(key , new List<int>());
                        }
                        
                        //add time slot
                        suspectedServers[key].Add(timeSlot);

                        break;

                    case "-crashed":

                        crashingTimeSlot = int.Parse(args[i + 1]);

                        break;
                    default:
                        break;
                }
            }

            
            writeArguments(name, timeslotNumber, duration, startingTime, currLM, leaseManagerNameToUrl, crashingTimeSlot, suspectedServers);

            int id = 0;

            //get the id of this lease manager (for paxos leader election)
            foreach(string key in leaseManagerNameToUrl.Keys){
                if(leaseManagerNameToUrl[key].Equals(currLM)){
                    id = leaseManagerNameToId[key];
                }
            }

            if(id == 0){
                throw new Exception("Something went wrong while getting the id of this lease manager");
            }

            //paxos state class
            PaxosInternalServiceClient paxosInternalServiceClient = new PaxosInternalServiceClient(name, leaseManagerNameToUrl, (leaseManagerNameToUrl.Count / 2) + 1);
            PaxosImplementation paxos = new PaxosImplementation(timeslotNumber, duration, startingTime, leaseManagerNameToId, 
                                                                crashingTimeSlot, suspectedServers, id, paxosInternalServiceClient);

            //transaction manager communication
            LeaseSolicitationServiceImpl leaseSolicitationService = new LeaseSolicitationServiceImpl(paxos);

            //other lease managers communication (paxos)
            PaxosInternalServiceServer paxosInternalServiceServer = new PaxosInternalServiceServer(paxos);

            string hostname = currLM.Split(':')[1].Remove(0, 2);
            int port = int.Parse(currLM.Split(':')[2]);

            try{

                var server = new Server
                {
                    Services = { LeaseSolicitationService.BindService(leaseSolicitationService),
                                PaxosInternalService.BindService(paxosInternalServiceServer)},

                    Ports = { new ServerPort(hostname, port,ServerCredentials.Insecure) }
                };

                server.Start();

            }catch(IOException e){
                
                Console.WriteLine("Error while trying to start server: " + e.Message);
            }


            Console.WriteLine("Lease Manager server listening on port " + port);

            try{
                paxos.Start(); //start paxos algorithm
            }catch(Exception e){
                Console.WriteLine("Error while trying to start server: " + e.Message);
            }


            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
        }

        private static void writeArguments(string name, int timeslotNumber, int duration, TimeOnly startingTime, string currLM,
                                    Dictionary<string, string> leaseManagerNameToUrl, int crashingTimeSlot, Dictionary<string, List<int>> suspectedServers){
            // Display the extracted values for verification
            Console.WriteLine("Received Arguments:");
            Console.WriteLine("Name: " + name);
            Console.WriteLine();
            Console.WriteLine("Timeslot Number: " + timeslotNumber);
            Console.WriteLine("Duration: " + duration);
            Console.WriteLine("Starting Time: " + startingTime);
            Console.WriteLine();
            Console.WriteLine("This server url: " + currLM);
            Console.WriteLine("Lease Manager URLs:");

            foreach(string key in leaseManagerNameToUrl.Keys){
                Console.WriteLine("  - " + key + ": " + leaseManagerNameToUrl[key]);
            }

            Console.WriteLine();

            if(crashingTimeSlot != -1)
                Console.WriteLine("This server crashes at time slot: " + crashingTimeSlot.ToString());
            else
                Console.WriteLine("This server won't crash in this run.");

            Console.WriteLine();

            if(suspectedServers.Count != 0){

                Console.WriteLine("Time slots it will suspect servers:");
                foreach(string key in suspectedServers.Keys){
                    Console.WriteLine("  - " + key + " at time slots: " + string.Join(", ", suspectedServers[key]));
                }

            }else{
                Console.WriteLine("This server won't suspect any servers in this run");
            }


            Console.WriteLine();
        }

        private static bool isNotPrefix(string arg)
        {
            if (arg.Equals("-n")  || 
                arg.Equals("-e")  || 
                arg.Equals("-nr") || 
                arg.Equals("-d")  || 
                arg.Equals("-t")  || 
                arg.Equals("-u")  ||
                arg.Equals("-id") ) return false;
            
            return true;
        }

    }
}
