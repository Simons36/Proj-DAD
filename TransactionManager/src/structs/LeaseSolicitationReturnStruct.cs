using Common.structs;

namespace TransactionManager.src.structs
{
    public struct LeaseSolicitationReturnStruct
    {
        private int _epoch;

        private List<Lease> _leases;

        public LeaseSolicitationReturnStruct(int epoch, List<Lease> leases)
        {
            _epoch = epoch;
            _leases = leases;
        }

        public int Epoch
        {
            get => _epoch;
            set => _epoch = value;
        }

        public List<Lease> Leases
        {
            get => _leases;
            set => _leases = value;
        }
    }
}