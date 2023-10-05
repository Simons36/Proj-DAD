using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Client.src.service
{
    public class ClientServiceImpl{

        List<string> _tMsUrls;

        public ClientServiceImpl(List<string> tMsUrls){
            foreach (string url in tMsUrls)
            {
                Console.WriteLine(url);
            }
        }

        public List<DadInt> TxSubmit(string client, List<string> keys, List<DadInt> ds){
            /* estabelecer comunicação com transaction managers e pedir o submit (TO DO) */

            return new List<DadInt>();
        }
        
    }
}