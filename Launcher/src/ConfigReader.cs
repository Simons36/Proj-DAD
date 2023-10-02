using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace src
{
    public class ConfigReader{

        // TODO: Add a way to read the config file and parse it into a list of ProcessStartInfo objects
        private List<ProcessStartInfo> _processes;

        private string _configPath;

        private int _numberOfTimeSlots;

        DateTime _startingTime;

        int _timeSlotDuration;

        public ConfigReader(string configPath){
            _processes = new List<ProcessStartInfo>();
            _configPath = configPath;
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
                            break;

                        case "L":
                            // Lease Manager
                            pathToProject = LauncherPaths.LeaseManagerPath;
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

                    Console.WriteLine($"Time slot duration: {_timeSlotDuration}");
                    }
                    break;
                
                default:
                    // Invalid line, ignore
                    break;
            };

            

        }
        
    }
}