using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using src;

namespace src
{
    public class ConfigReader{

        // TODO: Add a way to read the config file and parse it into a list of ProcessStartInfo objects
        private List<ProcessStartInfo> _processes;

        private string _configPath;

        private int _numberOfTimeSlots;

        private DateTime _startingTime;

        private int _timeSlotDuration;

        private Dictionary<int, FailureStatus> _failureStatusMap; //failure struct for each time slot

        private int _serverCount;

        public ConfigReader(string configPath){
            _processes = new List<ProcessStartInfo>();
            _configPath = configPath;
            _serverCount = 0;
            _failureStatusMap = new Dictionary<int, FailureStatus>();
        }

        // Getters
        public List<ProcessStartInfo> Processes{
            get{
                return _processes;
            }
        }

        public int NumberOfTimeSlots{
            get{
                return _numberOfTimeSlots;
            }
        }

        public DateTime StartingTime{
            get{
                return _startingTime;
            }
        }

        public int TimeSlotDuration{
            get{
                return _timeSlotDuration;
            }
        }

        public void ReadConfig(){
            Console.WriteLine("Reading config file at: " + _configPath);

            try{
                using (StreamReader sr = new StreamReader(_configPath)){
                    string line;

                    while ((line = sr.ReadLine()) != null){
                        ReadConfigLine(line);
                    }

                }
            }
            catch (IOException e){
                Console.WriteLine($"An error occurred: {e.Message}");
            }
        }

        private void ReadConfigLine(string line){
            //check if it is a comment (starts with #)
            char firstChar = line[0];
            Dictionary<string, int> mapServersPosition = new Dictionary<string, int>();

            switch(firstChar){
                case '#':
                    // Comment, ignore
                    break;
                case 'P':
                    {
                    // Process, add to list
                    string[] splitLine = line.Split(" ");
                    string processName = splitLine[1];
                    string pathToProject; 

                    switch(splitLine[2]){

                        case "T":
                            // Transaction Manager
                            pathToProject = LauncherPaths.TransactionManagerPath;
                            _serverCount++;
                            mapServersPosition.Add(processName, mapServersPosition.Count);
                            break;

                        case "L":
                            // Lease Manager
                            pathToProject = LauncherPaths.LeaseManagerPath;
                            _serverCount++;
                            mapServersPosition.Add(processName, mapServersPosition.Count);
                            break;

                        case "C":
                            // Client
                            pathToProject = LauncherPaths.ClientPath;
                            break;

                        default:
                            throw new Exception("Invalid process type");
                        
                    };

                    string extraArgument = splitLine[3];


                    ProcessStartInfo process = new ProcessStartInfo(){
                        FileName = "gnome-terminal",
                        Arguments = $"-- dotnet run --project {pathToProject} {processName} {extraArgument}",
                        RedirectStandardOutput = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    };

                    Console.WriteLine($"Read process with name {processName} of type {splitLine[2]} with extra argument {extraArgument}");

                    _processes.Add(process);
                    }
                    break;

                case 'S':
                    {
                    // Time slot, set number of time slots
                    string[] splitLine = line.Split(" ");
                    _numberOfTimeSlots = int.Parse(splitLine[1]);

                    Console.WriteLine($"Number of time slots: {_numberOfTimeSlots}");
                    }
                    break;

                case 'T':
                    {
                    // Starting time, set starting time
                    string[] splitLine = line.Substring(2).Split(":");

                    _startingTime = new DateTime(1, 1, 1, int.Parse(splitLine[0]), int.Parse(splitLine[1]), int.Parse(splitLine[2]));

                    Console.WriteLine($"Starting time: {line.Substring(2)}");
                    }
                    break;

                case 'D':
                    {
                    // Time slot duration, set time slot duration
                    string[] splitLine = line.Split(" ");
                    _timeSlotDuration = int.Parse(splitLine[1]);

                    Console.WriteLine($"Time slot duration: {_timeSlotDuration} ms");
                    }
                    break;

                case 'F':
                    {
                    // Failure, add to failure status map
                    string[] splitLine = line.Split(" ");
                    int timeSlot = int.Parse(splitLine[1]);

                    FailureStatus failureStatus = new FailureStatus(_serverCount);
                    failureStatus.setMapServersPosition(mapServersPosition);

                    for(int i = 2; i < _serverCount + 2; i++){
                        if(splitLine[i].Equals("C")){
                            string serverCrashed = mapServersPosition.FirstOrDefault(x => x.Value == i-2).Key;
                            Console.WriteLine($"Server {serverCrashed} crashed");
                            failureStatus.setCrashed(i - 2);
                        }
                    }

                    for(int i = _serverCount + 2; i < splitLine.Length; i++){
                        splitLine[i].Remove(0);
                        splitLine[i].Remove(splitLine[i].Length - 1);

                        string[] splitServers = splitLine[i].Split(",");

                        failureStatus.addCrashSuspicion(splitServers[0], splitServers[1]);
                    }

                    _failureStatusMap.Add(timeSlot, failureStatus);

                    }
                    break;
                
                default:
                    // Invalid line, ignore
                    break;
            };

            

        }
        
    }
}