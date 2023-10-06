using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TransactionManager.src.service.util
{
    public static class DadIntParser
    {
        //this fucnctions parses proto DadInts to our DadInts and vice-versa

        public static DadInt parseCommonDadInt(Common.DadInt dadInt)
        {
            DadInt newDadInt = new DadInt();
            newDadInt.Key = dadInt.Key;
            newDadInt.Value = dadInt.Value;
            return newDadInt;
        }

        public static Common.DadInt parseProtoDadInt(DadInt dadInt)
        {
            return new Common.DadInt
            {
                Key = dadInt.Key,
                Value = dadInt.Value,
            };
        }
    }
}