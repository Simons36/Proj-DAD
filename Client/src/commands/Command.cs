using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Client.src.commands
{
    public abstract class Command
    {
        public Command(){
        }

        public abstract void Execute();
    }
}