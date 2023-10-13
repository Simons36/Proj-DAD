using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Grpc.Core;
using LeaseManager.src.service;
using Common.util;
using Common.exceptions;
using Common.structs;

namespace LeaseManager.src.paxos
{
    public class PaxosImplementation
    {
        private List<TimeOnly> _epochStartingTimes;

        private Dictionary<string, int> _leaseManagerNameToId;

        private int _crashingTimeSlot;

        private Dictionary<string, List<int>> _suspectedServers;

        private int _id;

        private int _currentEpoch;

        private List<Lease> _currentEpochReceivedLeases;

        public PaxosImplementation(int timeslotNumber, int duration, TimeOnly startingTime, Dictionary<string, int> leaseManagerNameToId, 
                                    int crashingTimeSlot, Dictionary<string, List<int>> suspectedServers, int id){
            
            _leaseManagerNameToId = leaseManagerNameToId;
            _crashingTimeSlot = crashingTimeSlot;
            _suspectedServers = suspectedServers;

            _epochStartingTimes = new List<TimeOnly>();
            _epochStartingTimes.Add(startingTime);

            for(int i = 0; i < timeslotNumber; i++){
                _epochStartingTimes.Add(_epochStartingTimes[i].Add(TimeSpan.FromMilliseconds(duration)));
            }

            _id = id;
            _currentEpochReceivedLeases = new List<Lease>();

        }

        public void Start(){

            _currentEpoch = -1;

            int timeToWait = 0;

            try{
                
                foreach(TimeOnly epochStart in _epochStartingTimes)
                    timeToWait = UtilMethods.getTimeUntilStart(epochStart);
                
                Thread.Sleep(timeToWait);

                Task advanceEpoch = new Task(() => AdvanceEpoch());
                advanceEpoch.Start();


            }catch(InvalidStartingTimeException e){
                throw e;
            }
           
        }

        public void TMRequestHandler(){
           Console.WriteLine("AAAAAAA");
        }

        public async void AdvanceEpoch(){
            _currentEpoch++;
            
            if(_currentEpoch != 0){

                OrderPreviousEpochRequests(); //start executing paxos algorithm on previous epoch requests

                lock(this){
                    _currentEpochReceivedLeases.Clear();
                }
            }

        }

        public async void OrderPreviousEpochRequests(){

        }


    }
}