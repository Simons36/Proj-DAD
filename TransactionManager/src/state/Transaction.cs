using Common.structs;

namespace TransactionManager.src.state
{
    public class Transaction
    {
        private List<string> _keysToBeRead;

        private List<DadInt> _dadIntsToBeWritten;

        private bool _hasFinished;

        public Transaction(List<string> keysToBeRead, List<DadInt> dadIntsToBeWritten){
            _keysToBeRead = keysToBeRead;
            _dadIntsToBeWritten = dadIntsToBeWritten;
            _hasFinished = false;
        }

        //getter for hasFinished
        public bool HasFinished{
            get { return _hasFinished; }
        }

        //getter for dad ints written
        public List<DadInt> DadIntsToBeWritten{
            get { return _dadIntsToBeWritten; }
        }

        public List<string> GetNecessaryKeys(){
            //return junction of keys to be read and keys of dadints in dadintstobewritten
            return _keysToBeRead.Concat(_dadIntsToBeWritten.ConvertAll(dadInt => dadInt.Key)).Distinct().ToList();

        }

        public List<DadInt> Execute(Dictionary<string, DadInt> dadIntsSet){
            List<DadInt> returnedDadInts = new List<DadInt>();

            foreach(string key in _keysToBeRead){
                if(dadIntsSet.ContainsKey(key)){
                    returnedDadInts.Add(dadIntsSet[key]);
                }
            }

            foreach(DadInt dadInt in _dadIntsToBeWritten){

                if(dadIntsSet.ContainsKey(dadInt.Key)){
                    dadIntsSet[dadInt.Key] = dadInt;
                }else{
                    Console.WriteLine("DadInt with key " + dadInt.Key + " was not found. Creating new one.");
                    dadIntsSet.Add(dadInt.Key, dadInt);
                }
            
            }

            _hasFinished = true;

            return returnedDadInts;
        }
    }
}