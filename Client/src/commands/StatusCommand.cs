using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Client.src.service;

namespace Client.src.commands
{
    public class StatusCommand : Command
    {
        private ClientServiceImpl _clientService;

        public StatusCommand(ClientServiceImpl clientService) : base()
        {
            _clientService = clientService;
        }

        public override void Execute()
        {
            Console.WriteLine("Executing status command");
            
            try{
                _clientService.Status();
            }
            catch (Exception e){
                Console.WriteLine(e);
                throw;
            }

            Console.WriteLine("Status command executed");
            Console.WriteLine();
        }
        
    }
}