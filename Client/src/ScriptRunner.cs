using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Client.src.service;

namespace Client.src
{
    public class ScriptRunner
    {
        private string _name;

        private string _script;

        private int _numberTimeSlots;

        private int _timeSlotDuration;

        private TimeOnly _startingTime;

        private ClientServiceImpl _clientService;

        public ScriptRunner(string name, string script, int numberTimeSlots, int timeSlotDuration, TimeOnly startingTime, ClientServiceImpl clientService){
            _name = name;
            _script = script;
            _numberTimeSlots = numberTimeSlots;
            _timeSlotDuration = timeSlotDuration;
            _startingTime = startingTime;
            _clientService = clientService;
        }

        // private string _scriptPath;

        // public ScriptReader(string script){
        //     _scriptPath = "..\\scripts\\" + script;

        //     if(RuntimeInformation.IsOSPlatform(OSPlatform.Linux)){
        //         //replace \\ with /
        //         _scriptPath = _scriptPath.Replace("\\", "/");
        //     }
        // }
    }
}