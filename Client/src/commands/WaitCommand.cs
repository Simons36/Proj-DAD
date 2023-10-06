using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace Client.src.commands
{
    public class WaitCommand : Command
    {
        private int _msToWait;

        public WaitCommand(int msToWait) : base()
        {
            _msToWait = msToWait;
        }

        public override void Execute()
        {
            Console.WriteLine("Executing wait command, waiting for " + _msToWait + "ms");
            Thread.Sleep(_msToWait);
            Console.WriteLine("Wait command executed");
            Console.WriteLine();
        }
        
    }
}