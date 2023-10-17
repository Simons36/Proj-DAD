using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Common.structs;

namespace LeaseManager.src.paxos
{
    public struct PaxosMessageStruct
    {
        private int _writeTimestamp;

        private int _readTimestamp;

        private List<Lease>? _leases;

        private int _epoch;

        public PaxosMessageStruct(int writeTimestamp, int readTimestamp, List<Lease> leases, int epoch){
            _writeTimestamp = writeTimestamp;
            _readTimestamp = readTimestamp;
            _leases = leases;
            _epoch = epoch;
        }

        public PaxosMessageStruct(int writeTimestamp, int epoch){
            _writeTimestamp = writeTimestamp;
            _readTimestamp = -1;
            _leases = null;
            _epoch = epoch;
        }

        public PaxosMessageStruct(int writeTimestamp, List<Lease> leases, int epoch){
            _writeTimestamp = writeTimestamp;
            _leases = leases;
            _epoch = epoch;
        }

        public int WriteTimestamp
        {
            get { return _writeTimestamp; }
        }

        public int ReadTimestamp
        {
            get { return _readTimestamp; }
        }

        public List<Lease> Leases
        {
            get { return _leases; }
        }

        public int Epoch
        {
            get { return _epoch; }
        }


    }
}