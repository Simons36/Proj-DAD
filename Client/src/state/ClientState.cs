using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Client.src.state
{
    public class ClientState
    {
        private string _name;

        private string _script;

        private int _numberTimeSlots;

        private int _timeSlotDuration;

        private TimeOnly _startingTime;

        public ClientState(string name, string script, int numberTimeSlots, int timeSlotDuration, TimeOnly startingTime){
            _name = name;
            _script = script;
            _numberTimeSlots = numberTimeSlots;
            _timeSlotDuration = timeSlotDuration;
            _startingTime = startingTime;
        }

    }
}
