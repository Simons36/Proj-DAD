
namespace LeaseManager.src.service {

    public class LeaseManagerServiceImpl : LeaseSolicitationService.LeaseSolicitationServiceBase
    {
        private string name;
        private int timeslotNumber;
        private int duration;
        private TimeOnly startingTime;
        private List<string> leaseManagerUrls;

        public LeaseManagerServiceImpl(string name, int timeslotNumber, int duration, TimeOnly startingTime, List<string> leaseManagerUrls)
        {
            this.name = name;
            this.timeslotNumber = timeslotNumber;
            this.duration = duration;
            this.startingTime = startingTime;
            this.leaseManagerUrls = leaseManagerUrls;
        }
    }
}