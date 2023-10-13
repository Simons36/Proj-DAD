using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Client.src.service;
using Common.structs;

namespace Client.src.commands
{
    public class TransactionCommand : Command
    {
        private ClientServiceImpl _clientService;

        private string _clientName;

        private List<string> _dadIntsToRead;

        private List<DadInt> _dadIntsToWrite;

        public TransactionCommand(ClientServiceImpl clientService, string clientName, List<string> dadIntsToRead, List<DadInt> dadIntsToWrite) : base()
        {
            _clientService = clientService;
            _clientName = clientName;
            _dadIntsToRead = dadIntsToRead;
            _dadIntsToWrite = dadIntsToWrite;
        }

        public override void Execute()
        {
            Console.WriteLine("Executing transaction command");
            List<DadInt> results;

            try{
                 results = _clientService.TxSubmit(_clientName, _dadIntsToRead, _dadIntsToWrite);
            }
            catch (Exception e){
                Console.WriteLine(e);
                throw;
            }

            Console.WriteLine("Received from transaction:");
            foreach (DadInt dadInt in results){
                Console.Write("DadInt received: <" + dadInt.Key + "> " + dadInt.Value);
            }
            
            Console.WriteLine();
        }
    }
}
