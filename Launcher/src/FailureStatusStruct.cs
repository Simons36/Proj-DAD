using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Launcher.src
{
    public struct FailureStatusStruct
    {
        private int _timeSlot;
        
        private bool[] _crashedStatus;

        private List<string[]> _crashedSuspicions;

        public FailureStatusStruct(int timeSlot, int numServers){
            _timeSlot = timeSlot;
            _crashedStatus = new bool[numServers];
            for(int i = 0; i < numServers; i++){
                _crashedStatus[i] = false;
            }
        }

        public int TimeSlot{
            get { return _timeSlot; }
        }

        public void setCrashed(int serverId){
            _crashedStatus[serverId] = true;
        }

        public void addCrashSuspicion(string serverThatSuspects, string suspectedServer){
            string[] crashSuspicion = new string[2];
            
            crashSuspicion[0] = serverThatSuspects;
            crashSuspicion[1] = suspectedServer;

            _crashedSuspicions.Add(crashSuspicion);
        }
    }
}