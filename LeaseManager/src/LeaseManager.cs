using Grpc.Core;
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
            List<string> leaseManagerUrls = new List<string>();

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

            
            writeArguments(name, timeslotNumber, duration, startingTime, currLM, leaseManagerUrls, crashingTimeSlot, suspectedServers);

            // var server = new Server
            // {
            //     Services = { LeaseManagerService.BindService(new LeaseManagerServiceImpl(name, timeslotNumber, duration, startingTime, leaseManagerUrls)) },
            //     Ports = { new ServerPort("localhost", int.Parse(currLM.Split(':')[2]), ServerCredentials.Insecure) }
            // };
            
            //server.Start();

            Console.WriteLine("Lease Manager server listening on port " + currLM.Split(':')[2]);

            while (true);
        }

        private static void writeArguments(string name, int timeslotNumber, int duration, TimeOnly startingTime, string currLM,
                                    List<string> leaseManagerUrls, int crashingTimeSlot, Dictionary<string, List<int>> suspectedServers){
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


            foreach (var url in leaseManagerUrls)
            {
                Console.WriteLine("  - " + url);
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
    }
}
