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

        public ConfigReader(string configPath){
            _processes = new List<ProcessStartInfo>();
            _configPath = configPath;
        }

        public void ReadConfig(){
            Console.WriteLine("Reading config file at: " + _configPath);

            try{
                using (StreamReader sr = new StreamReader(_configPath)){
                    string line;

                    while ((line = sr.ReadLine()) != null){
                        Console.WriteLine(line);
                    }

                }
            }
            catch (IOException e){
                Console.WriteLine($"An error occurred: {e.Message}");
            }
        }
        
    }
}