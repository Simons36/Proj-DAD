using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using src;

namespace src
{
    public class ConfigReader{

        //blueprint of a command: "-- dotnet run --project {pathToProject} -n {processName} -e {extraArgument} -c {timeslot that server crashes} 
        //                                                                 -s {timeSlot where suspicion occurs} {suspected server} -nr {number of time slots}
        //                                                                 -d {duration of a time slot} -t {starting time of test}

        // TODO: Add a way to read the config file and parse it into a list of ProcessStartInfo objects
        private List<ProcessStartInfo> _processes;

        private Dictionary<string, string> _processesArgumentsMap; //key: process name, value: arguments for console line

        private string _configPath;

        private int _numberOfTimeSlots;

        private DateTime _startingTime;

        private int _timeSlotDuration;

        private int _serverCount;

        private Dictionary<int, string> _mapServersPosition;


        private List<string> _serversThatCrashed;


        public ConfigReader(string configPath){
            _processes = new List<ProcessStartInfo>();
            _configPath = configPath;
            _serverCount = 0;
            _mapServersPosition = new Dictionary<int, string>();
            _processesArgumentsMap = new Dictionary<string, string>();
            _serversThatCrashed = new List<string>();
        }

        // Getters
        public List<ProcessStartInfo> Processes{
            get{
                return _processes;
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

                    List<string> keys = new List<string>(_processesArgumentsMap.Keys);
                    foreach(string key in keys){
                        _processesArgumentsMap[key] += $" -nr {_numberOfTimeSlots.ToString()} -d {_timeSlotDuration.ToString()} -t {_startingTime.TimeOfDay.ToString()}";

                        string arguments = _processesArgumentsMap[key];

                        ProcessStartInfo processStartInfo = new ProcessStartInfo{
                            FileName = "gnome-terminal",
                            Arguments = arguments,
                            RedirectStandardOutput = true,
                            UseShellExecute = false,
                            CreateNoWindow = true
                        };

                        _processes.Add(processStartInfo);
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
                            _mapServersPosition.Add(_mapServersPosition.Count, processName);
                            break;

                        case "L":
                            // Lease Manager
                            pathToProject = LauncherPaths.LeaseManagerPath;
                            _serverCount++;
                            _mapServersPosition.Add(_mapServersPosition.Count, processName);
                            break;

                        case "C":
                            // Client
                            pathToProject = LauncherPaths.ClientPath;
                            break;

                        default:
                            throw new Exception("Invalid process type");
                        
                    };

                    string extraArgument = splitLine[3];

                    string argumentsLine = $"-- dotnet run --project {pathToProject} -n {processName} -e {extraArgument}";
                    _processesArgumentsMap.Add(processName, argumentsLine);

                    Console.WriteLine($"Read process with name {processName} of type {splitLine[2]} with extra argument {extraArgument}");

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

                    

                    for(int i = 2; i < _serverCount + 2; i++){
                        if(splitLine[i].Equals("C")){
                            string _serverName = _mapServersPosition[i - 2];
                            if(!_serversThatCrashed.Contains(_serverName)){
                                _processesArgumentsMap[_serverName] += $" -c {timeSlot}"; //introduce time slot to crash
                                _serversThatCrashed.Add(_serverName);
                            }
                        }
                    }

                    for(int i = _serverCount + 2; i < splitLine.Length; i++){

                        // Remove the first and last character (parenthesis)
                        splitLine[i] = splitLine[i].Remove(0, 1);
                        splitLine[i] = splitLine[i].Remove(splitLine[i].Length - 1, 1);
                        // Split the string into two parts, the server that suspects and the suspected server
                        string[] splitServers = splitLine[i].Split(",");

                        // Add the crash suspicion to the failure status object
                        _processesArgumentsMap[splitServers[0]] += $" -s {timeSlot} {splitServers[1]}";
                    }

                    }
                    break;
                
                default:
                    // Invalid line, ignore
                    break;
            };

            

        }
        
    }
}