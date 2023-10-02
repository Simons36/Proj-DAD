using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace src
{
    public class FailureStatus
    {   
        private bool[] _crashedStatus;

        private List<string[]> _crashedSuspicions;

        private Dictionary<string, int> _mapServersPosition;

        public FailureStatus(int numServers){
            _crashedStatus = new bool[numServers];
            for(int i = 0; i < numServers; i++){
                _crashedStatus[i] = false;
            }

            _crashedSuspicions = new List<string[]>();
            _mapServersPosition = new Dictionary<string, int>();
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

        //set the map of servers position
        public void setMapServersPosition(Dictionary<string, int> mapServersPosition){
            _mapServersPosition = mapServersPosition;
        }
    }
}