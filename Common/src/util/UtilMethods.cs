using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Common.exceptions;
using Common.structs;

namespace Common.util
{
    public static class UtilMethods
    {
        public static int getTimeUntilStart(TimeOnly startingTime){
            DateTime currTime = DateTime.Now;
            TimeSpan timeDiff = startingTime.ToTimeSpan() - currTime.TimeOfDay; // time diff between current time and starting time

            if(timeDiff.TotalMilliseconds < 0){//if starting time has already passed
                throw new InvalidStartingTimeException("Starting time is invalid (already passed)");
            }

            return (int)timeDiff.TotalMilliseconds;
        }

        public static ProtoDadInt parseCommonDadInt(DadInt dadInt)
        {
            ProtoDadInt newDadInt = new ProtoDadInt();
            newDadInt.Key = dadInt.Key;
            newDadInt.Value = dadInt.Value;
            return newDadInt;
        }

        public static DadInt parseProtoDadInt(ProtoDadInt dadInt)
        {
            return new DadInt
            {
                Key = dadInt.Key,
                Value = dadInt.Value,
            };
        }

        public static ProtoLease parseLeaseToProtoLease(Lease lease){
            ProtoLease protoLease = new ProtoLease();

            protoLease.TransactionManagerName = lease.AssignedTransactionManager;
            protoLease.DadIntsKeys.AddRange(lease.DadIntsKeys);

            return protoLease;
        }

        public static Lease parseProtoLeaseToLease(ProtoLease protoLease){

            return new Lease{
                AssignedTransactionManager = protoLease.TransactionManagerName,
                DadIntsKeys = protoLease.DadIntsKeys.ToHashSet()
            };
            
        }

    }
}