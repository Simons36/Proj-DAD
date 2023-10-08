using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Client.src.service;

namespace Client.src.commands
{
    public class TransactionCommand : Command
    {
        private ClientServiceImpl _clientService;

        private string _clientName;

        private List<string> _dadIntsToRead;

        private List<Common.DadInt> _dadIntsToWrite;

        public TransactionCommand(ClientServiceImpl clientService, string clientName, List<string> dadIntsToRead, List<Common.DadInt> dadIntsToWrite) : base()
        {
            _clientService = clientService;
            _clientName = clientName;
            _dadIntsToRead = dadIntsToRead;
            _dadIntsToWrite = dadIntsToWrite;
        }

        public override void Execute()
        {
            Console.WriteLine("Executing transaction command");
            List<Common.DadInt> results;

            try{
                 results = _clientService.TxSubmit(_clientName, _dadIntsToRead, _dadIntsToWrite);
            }
            catch (Exception e){
                Console.WriteLine(e);
                throw;
            }

            Console.WriteLine("Received from transaction:");
            foreach (Common.DadInt dadInt in results){
                Console.Write("DadInt received: <" + dadInt.Key + "> " + dadInt.Value);
            }
            
            Console.WriteLine();
        }
    }
}
