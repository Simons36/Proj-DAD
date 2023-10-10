using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace src.ConfigReader
{
    public class ConfigReader
    {
        //blueprint of a command: "-- dotnet run --project {pathToProject} -n {processName} -e {extraArgument} -c {timeslot that server crashes} 
        //                                                                 -s {timeSlot where suspicion occurs} {suspected server} -nr {number of time slots}
        //                                                                 -d {duration of a time slot} -t {starting time of test}
        //                                                                 !CLIENT ONLY! -u {url tm1} {url tm2} ... {url tmN}
        private List<ProcessStartInfo> _processes;

        private Dictionary<int,string> _clientsArgumentsMap; //key: client id, value: arguments for console line

        private Dictionary<int,string> _transactionManagersArgumentsMap; //key: transaction manager id, value: arguments for console line

        private Dictionary<int,string> _leaseManagersArgumentsMap; //key: lease manager id, value: arguments for console line

        private string _configPath;

        private int _numberOfTimeSlots;

        private DateTime _startingTime;

        private int _timeSlotDuration;

        private List<int> _serversThatCrashed; //stores id of servers that crashed

        private List<string> _transactionManagersUrls; //for clients

        private bool _isWindows;

        private string _clientPath;

        private string _transactionManagerPath;

        private string _leaseManagerPath;

        private List<string> _leaseManagerUrls;

        //Linux Constructor
        public ConfigReader(string configPath){
            _configPath = configPath;
            
            _processes = new List<ProcessStartInfo>();

            _serversThatCrashed = new List<int>();
            _transactionManagersUrls = new List<string>();

            _clientsArgumentsMap = new Dictionary<int, string>();
            _transactionManagersArgumentsMap = new Dictionary<int, string>();
            _leaseManagersArgumentsMap = new Dictionary<int, string>();
            _leaseManagerUrls = new List<string>();
            _isWindows = false;
        }

        //Windows Constructor
        public ConfigReader(string configPath, string[] processesPaths){
            _configPath = configPath;
            
            _processes = new List<ProcessStartInfo>();

            _serversThatCrashed = new List<int>();
            _transactionManagersUrls = new List<string>();

            _clientsArgumentsMap = new Dictionary<int, string>();
            _transactionManagersArgumentsMap = new Dictionary<int, string>();
            _leaseManagersArgumentsMap = new Dictionary<int, string>();

            _clientPath = processesPaths[0];
            _transactionManagerPath = processesPaths[1];
            _leaseManagerPath = processesPaths[2];
            _leaseManagerUrls = new List<string>();

            _isWindows = true;
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

                    string toAppend = $" -nr {_numberOfTimeSlots.ToString()} -d {_timeSlotDuration.ToString()} -t {_startingTime.TimeOfDay.ToString()}";

                    appendToArguments(toAppend);
                    
                    //add tm urls to client arguments
                    foreach(int key in _clientsArgumentsMap.Keys){
                        _clientsArgumentsMap[key] += " -u";
                        foreach(string url in _transactionManagersUrls){
                            _clientsArgumentsMap[key] += $" {url}";
                        }
                    }

                    //add tm urls to transaction manager arguments
                    foreach(int key in _transactionManagersArgumentsMap.Keys){
                        _transactionManagersArgumentsMap[key] += " -u";
                        foreach(string url in _transactionManagersUrls){
                            _transactionManagersArgumentsMap[key] += $" {url}";
                        }
                    }

                    //add lm urls to lease manager arguments
                    foreach(int key in _leaseManagersArgumentsMap.Keys)
                    {
                        _leaseManagersArgumentsMap[key] += " -nl";
                        foreach(string url in _leaseManagerUrls)
                        {
                            _leaseManagersArgumentsMap[key] += $" {url}";
                        }
                    }
                    


                    if(!_isWindows){

                        List<string> arguments = new List<string>(_clientsArgumentsMap.Values)
                                                    .Concat(_transactionManagersArgumentsMap.Values)
                                                    .Concat(_leaseManagersArgumentsMap.Values)
                                                    .ToList();

                        foreach(string argument in arguments){
                            ProcessStartInfo processStartInfo = new ProcessStartInfo
                            {
                                FileName = "gnome-terminal",
                                Arguments = argument,
                                RedirectStandardOutput = true,
                                UseShellExecute = false,
                                CreateNoWindow = true
                            };

                            _processes.Add(processStartInfo);
                        }

                    }else{
                        foreach(string argument in _clientsArgumentsMap.Values){
                            ProcessStartInfo processStartInfo = new ProcessStartInfo
                            {
                                FileName = _clientPath,
                                Arguments = argument,
                                UseShellExecute = true,
                                CreateNoWindow = true
                            };

                            _processes.Add(processStartInfo);
                        }

                        foreach(string argument in _transactionManagersArgumentsMap.Values){
                            ProcessStartInfo processStartInfo = new ProcessStartInfo
                            {
                                FileName = _transactionManagerPath,
                                Arguments = argument,
                                UseShellExecute = true,
                                CreateNoWindow = true
                            };

                            _processes.Add(processStartInfo);
                        }

                        foreach(string argument in _leaseManagersArgumentsMap.Values){
                            

                            ProcessStartInfo processStartInfo = new ProcessStartInfo
                            {
                                FileName = _leaseManagerPath,
                                Arguments = argument,
                                UseShellExecute = true,
                                CreateNoWindow = true
                            };

                            _processes.Add(processStartInfo);
                        }
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
                    string pathToProject = "";
                    string extraArgument = splitLine[3];
                    string argumentsLine = "";

                    switch(splitLine[2]){

                        case "T":

                            if(!_isWindows){
                                pathToProject = LauncherPaths.TransactionManagerPath;
                                argumentsLine += $"-- dotnet run --project ";
                            }
                            
                            argumentsLine += $"{pathToProject} -n {processName} -e {extraArgument}";

                            _transactionManagersUrls.Add(extraArgument);

                            _transactionManagersArgumentsMap.Add(_transactionManagersArgumentsMap.Count + 1, argumentsLine);
                            
                            break;

                        case "L":

                            if(!_isWindows){
                                pathToProject = LauncherPaths.LeaseManagerPath;
                                argumentsLine += $"-- dotnet run --project ";
                            }
                            
                            argumentsLine += $"{pathToProject} -n {processName} -e {extraArgument}";

                            _leaseManagerUrls.Add(extraArgument);

                            //id of lease managers starts counting after transaction managers' last id
                            _leaseManagersArgumentsMap.Add(_transactionManagersArgumentsMap.Count + _leaseManagersArgumentsMap.Count + 1, argumentsLine);
                            break;

                        case "C":
                            
                            if(!_isWindows){
                                pathToProject = LauncherPaths.ClientPath;
                                argumentsLine += $"-- dotnet run --project ";
                            }

                            argumentsLine += $"{pathToProject} -n {processName} -e {extraArgument}";

                            _clientsArgumentsMap.Add(_clientsArgumentsMap.Count + 1, argumentsLine);

                            break;

                        default:
                            throw new Exception("Invalid process type");
                        
                    };

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
                    string[] splitLine = line.Split(" ");
                    int timeSlot = int.Parse(splitLine[1]);

                    int serverCount = _leaseManagersArgumentsMap.Count + _transactionManagersArgumentsMap.Count;

                    
                    for(int i = 2; i < serverCount + 2; i++){

                        if(splitLine[i].Equals("C")){
                            
                            int id = i - 1;
                            if(!_serversThatCrashed.Contains(id)){
                                
                                if(_transactionManagersArgumentsMap.ContainsKey(id))
                                    _transactionManagersArgumentsMap[id] += $" -crashed {timeSlot}"; //introduce time slot to crash
                                else{
                                    _leaseManagersArgumentsMap[id] += $" -crashed {timeSlot}"; //introduce time slot to crash

                                }
                                _serversThatCrashed.Add(id);

                            }

                        }
                    }

                    for(int i = serverCount + 2; i < splitLine.Length; i++){

                        // Remove the first and last character (parenthesis)
                        splitLine[i] = splitLine[i].Remove(0, 1);
                        splitLine[i] = splitLine[i].Remove(splitLine[i].Length - 1, 1);
                        // Split the string into two parts, the server that suspects and the suspected server

                        
                        string[] splitServers = splitLine[i].Split(",");

                        int suspectingServer = getServerId(splitServers[0]); //get id of server that suspects

                        if(_transactionManagersArgumentsMap.ContainsKey(suspectingServer)){
                            _transactionManagersArgumentsMap[suspectingServer] += $" -s {timeSlot} {splitServers[1]}";
                        }else{
                            _leaseManagersArgumentsMap[suspectingServer] += $" -s {timeSlot} {splitServers[1]}";
                        }
                    }

                    }
                    break;
                
                default:
                    // Invalid line, ignore
                    break;
            };
        }
    

        protected int getServerId(string serverName){
            List<int> keys = new List<int>(_transactionManagersArgumentsMap.Keys);

            foreach(int key in keys){
                string[] splitArguments = _transactionManagersArgumentsMap[key].Split(" ");

                for(int i = 0; i < splitArguments.Length; i++){

                    if(splitArguments[i].Equals("-n")){
                        if(splitArguments[i+1].Equals(serverName)){
                            return key;
                        }

                        break;
                    }

                }
            }

            keys = new List<int>(_leaseManagersArgumentsMap.Keys);

            foreach(int key in keys){
                string[] splitArguments = _leaseManagersArgumentsMap[key].Split(" ");

                for(int i = 0; i < splitArguments.Length; i++){

                    if(splitArguments[i].Equals("-n")){
                        if(splitArguments[i+1].Equals(serverName)){
                            return key;
                        }

                        break;
                    }

                }
            }

            return -1;
        }

        private void appendToArguments(string toAppend){
            foreach(int key in _clientsArgumentsMap.Keys){
                _clientsArgumentsMap[key] += toAppend;
                _clientsArgumentsMap[key] += $" -id {key}"; //include numeric id
            }

            foreach(int key in _transactionManagersArgumentsMap.Keys){
                _transactionManagersArgumentsMap[key] += toAppend;
            }

            foreach(int key in _leaseManagersArgumentsMap.Keys){
                _leaseManagersArgumentsMap[key] += toAppend;
            }
        }
    }
}