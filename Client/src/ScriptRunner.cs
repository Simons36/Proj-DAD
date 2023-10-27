using System.Diagnostics;
using System.Runtime.InteropServices;
using Client.src.commands;
using Client.src.service;
using Common.exceptions;
using Common.util;

namespace Client.src
{
    public class ScriptRunner
    {
        private string _name;

        private string _scriptPath;

        private int _numberTimeSlots;

        private int _timeSlotDuration;

        private TimeOnly _startingTime;

        private ClientServiceImpl _clientService;

        //list of commands to execute on loop
        private List<Command> _commands;

        private int _transactionCommandsCount;

        public ScriptRunner(string name, string script, int numberTimeSlots, int timeSlotDuration, TimeOnly startingTime, ClientServiceImpl clientService){
            _name = name;

            if(RuntimeInformation.IsOSPlatform(OSPlatform.Linux)){
                _scriptPath = "../Client/scripts/" + script;
            }
            else{
                _scriptPath = "..\\..\\..\\..\\Client\\scripts\\" + script;
            }

            _numberTimeSlots = numberTimeSlots;
            _timeSlotDuration = timeSlotDuration;
            _startingTime = startingTime;
            _clientService = clientService;
            _commands = new List<Command>();
            _transactionCommandsCount = 0;
        }

        public void RunScript(){
            readScript();

            if(_commands.Count == 0){
                Console.WriteLine("No commands to execute");
                return;
            }

            int timeToRun = _numberTimeSlots * _timeSlotDuration;

            int timeToWait = 0;

            try{
                timeToWait = UtilMethods.getTimeUntilStart(_startingTime);
            }catch(InvalidStartingTimeException e){
                throw e;
            }
            
            //wait until starting time
            Thread.Sleep((timeToWait) );
            Console.WriteLine("Starting script");


            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();

            int i = 0;
            
            while (stopwatch.Elapsed.TotalMilliseconds < timeToRun)
            {
                i = i % _commands.Count;

                _commands[i].Execute();

                i++;
            }

            stopwatch.Stop();
            Console.WriteLine("Script finished running");
        }

        public void readScript(){
            string[] lines = System.IO.File.ReadAllLines(_scriptPath);

            foreach(string line in lines){
                string[] lineSplit = line.Split(" ");

                switch(lineSplit[0].ElementAt(0)){
                    case '#':
                        break;
                    
                    case 'T':

                        //reads part

                        List<string> listKeysToRead = new List<string>();

                        if(!lineSplit[1].Equals("()")){

                            string keysToRead = lineSplit[1].Remove(0, 1);
                            keysToRead = keysToRead.Remove(keysToRead.Length - 1, 1);

                            string[] keys = keysToRead.Split(",");

                            listKeysToRead = new List<string>();

                            foreach(string key in keys){
                                string keyToAdd = key;
                                keyToAdd = keyToAdd.Remove(0, 1);
                                keyToAdd = keyToAdd.Remove(keyToAdd.Length - 1, 1);

                                listKeysToRead.Add(keyToAdd);
                            }
                        }

                        //writes part

                        List<Common.structs.DadInt> listDadIntsToWrite = new List<Common.structs.DadInt>();

                        //remove parenthesis
                        string dadIntsToWrite = lineSplit[2].Remove(0, 1);
                        dadIntsToWrite = dadIntsToWrite.Remove(dadIntsToWrite.Length - 1, 1);

                        //split pairs
                        string[] listUnparsedDadInts = dadIntsToWrite.Trim('<', '>').Split(',');
                        List<string> listParsedDadInts = new List<string>();

                        //write each pair
                        foreach(string str in listUnparsedDadInts){
                            if(str != ""){
                                string toAdd = str;
                                toAdd = str.Replace("<", "");
                                toAdd = toAdd.Replace(">", "");
                                toAdd = toAdd.Replace("\"", ""); //remove quotes

                                listParsedDadInts.Add(toAdd);
                            }
                        }

                        for(int i = 0; i < listParsedDadInts.Count; i += 2){
                            Common.structs.DadInt dadIntToAdd = new Common.structs.DadInt(listParsedDadInts[i], int.Parse(listParsedDadInts[i + 1]));

                            listDadIntsToWrite.Add(dadIntToAdd);
                        }

                        _transactionCommandsCount++;

                        //create command and store it
                        TransactionCommand transactionCommand = new TransactionCommand(
                                                                         _clientService, 
                                                                            _name, 
                                                                         listKeysToRead, 
                                                                        listDadIntsToWrite, 
                                                                                    _transactionCommandsCount);

                        Console.WriteLine("Transaction command " + _transactionCommandsCount + ":");
                        Console.WriteLine(" Keys read:");
                        foreach(string key in listKeysToRead){
                            Console.WriteLine("  - " + key);
                        }
                        Console.WriteLine(" Keys written:");
                        foreach(Common.structs.DadInt dadInt in listDadIntsToWrite){
                            Console.WriteLine("  - " + dadInt.Key + " -> " + dadInt.Value);
                        }
                        Console.WriteLine();


                        _commands.Add(transactionCommand);

                        break;

                    case 'W':
                        WaitCommand waitCommand = new WaitCommand(int.Parse(lineSplit[1]));
                        _commands.Add(waitCommand);
                        break;

                    case 'S':
                        StatusCommand statusCommand = new StatusCommand(_clientService);
                        _commands.Add(statusCommand);
                        break;

                    default:
                        break;
                }
            }
        }

    }
}
