using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Client.src.service;
using Common.structs;
using Microsoft.VisualBasic;

namespace Client.src.commands
{
    public class TransactionCommand : Command
    {
        private ClientServiceImpl _clientService;

        private string _clientName;

        private List<string> _dadIntsToRead;

        private List<DadInt> _dadIntsToWrite;

        private int _id;

        private int _runNumber;

        public TransactionCommand(ClientServiceImpl clientService, string clientName, List<string> dadIntsToRead, List<DadInt> dadIntsToWrite, int id) : base()
        {
            _clientService = clientService;
            _clientName = clientName;
            _dadIntsToRead = dadIntsToRead;
            _dadIntsToWrite = dadIntsToWrite;
            _id = id;
            _runNumber = 1;
        }

        public override async void Execute()
        {
            Console.WriteLine("Executing transaction command " + _id + "." + _runNumber);

            List<DadInt> results;

            try{
                 results = await _clientService.TxSubmit(_clientName, _dadIntsToRead, _dadIntsToWrite);
            }
            catch (Exception e){
                Console.WriteLine(e);
                throw;
            }

            Console.WriteLine("Received from transaction:" + _id + "." + _runNumber);
            foreach (DadInt dadInt in results){
                Console.WriteLine("DadInt received: " + dadInt.ToString());
            }

            List<string> keysReceived = new List<string>();
            foreach(DadInt dadInt in results){
                keysReceived.Add(dadInt.Key);
            }

            foreach(string key in _dadIntsToRead){
                if(!keysReceived.Contains(key)){
                    Console.WriteLine("DadInt with key " + key + " is null");
                }
            }
            
            Console.WriteLine();

            _runNumber++;
        }
    }
}
