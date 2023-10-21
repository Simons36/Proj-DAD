using System.Diagnostics.Metrics;
using System.Reflection.Metadata.Ecma335;
using Common.structs;
using Grpc.Core;
using Grpc.Net.Client;
using LeaseManager.src.paxos;
using LeaseManager.src.service.exceptions;
using LeaseManager.src.service.util;

namespace LeaseManager.src.service
{
    public class PaxosInternalServiceClient : PaxosInternalService.PaxosInternalServiceClient
    {

        private List<PaxosInternalService.PaxosInternalServiceClient> _leaseManagersClients;

        private int _majorityNumber;

        //Acceptable delay in milliseconds of the paxos requests response
        private int _acceptableDelayPaxos;

        public PaxosInternalServiceClient(string serverName, Dictionary<string, string> leaseManagerNameToUrl, int majorityNumber, int timeSlotDuration) : base(){

            List<string> leaseManagerUrls = new List<string>();
            _leaseManagersClients = new List<PaxosInternalService.PaxosInternalServiceClient>();

            foreach(string key in leaseManagerNameToUrl.Keys){
                if(key != serverName){
                    leaseManagerUrls.Add(leaseManagerNameToUrl[key]);
                }
            }

            _majorityNumber = majorityNumber;
            
            //number to multiply timeslotduration to get acceptable delay
            double acceptableDelayMultiplier = 1.5;
            _acceptableDelayPaxos = (int) (acceptableDelayMultiplier * timeSlotDuration);

            InitChannels(leaseManagerUrls);

        }

        public void InitChannels(List<string> leaseManagerUrls){
            
            foreach(string url in leaseManagerUrls){

                try{
                    GrpcChannel channel = GrpcChannel.ForAddress(url);

                    _leaseManagersClients.Add(new PaxosInternalService.PaxosInternalServiceClient(channel));

                }catch(IOException e){
                    Console.WriteLine("Could not connect to lease manager at " + url + ": " + e.Message);
                    continue;
                }

                Console.WriteLine("Connected to lease manager at " + url);

            }

        }

        public async Task<List<PaxosMessageStruct>> broadcastPrepareMessage(PaxosMessageStruct message){

            List<PaxosMessageStruct> returnList = new List<PaxosMessageStruct>();

            PrepareMessage prepareMessage = PaxosMessagesParser.ParsePaxosMessageStructToPrepareMessage(message);

            List<Task<PromiseMessage>> tasks = new List<Task<PromiseMessage>>();

            foreach(PaxosInternalService.PaxosInternalServiceClient client in _leaseManagersClients){
                tasks.Add(client.PrepareAsync(prepareMessage).ResponseAsync);
            }

            foreach(Task<PromiseMessage> task in tasks){
                try{
                    PromiseMessage promiseMessage = await task;
                    PaxosMessageStruct paxosMessageStruct = PaxosMessagesParser.ParsePromiseMessageToPaxosMessageStruct(promiseMessage);
                    
                    returnList.Add(paxosMessageStruct);
                }catch(RpcException e){
                    if(e.StatusCode == StatusCode.FailedPrecondition){
                        Console.WriteLine("One lease manager had read timestamp higher than write timestamp, ignoring...");
                    }else{
                        Console.WriteLine("An error ocurred in one of the requests to the lease managers, probably crashed");
                    }
                }


                if((returnList.Count + 1) >= _majorityNumber){ // number of promises + 1 (this server)
                    return returnList;
                }
            }

            throw new Exception("Could not get majority of promise messages");

        }

        public async Task<List<PaxosMessageStruct>> broadcastAcceptMessage(PaxosMessageStruct message){
            
            List<PaxosMessageStruct> returnList = new List<PaxosMessageStruct>();
            
            AcceptMessage acceptMessage = PaxosMessagesParser.ParsePaxosMessageStructToAcceptMessage(message);

            List<Task<AcceptedMessage>> tasks = new List<Task<AcceptedMessage>>();

            foreach(PaxosInternalService.PaxosInternalServiceClient client in _leaseManagersClients){
                tasks.Add(client.AcceptAsync(acceptMessage).ResponseAsync);
            }


            foreach(Task<AcceptedMessage> task in tasks){
                try{
                    if(await Task.WhenAny(task, Task.Delay(_acceptableDelayPaxos)) == task){

                        AcceptedMessage acceptedMessage = await task;

                        PaxosMessageStruct paxosMessageStruct = PaxosMessagesParser.ParseAcceptedMessageToPaxosMessageStruct(acceptedMessage);

                        returnList.Add(paxosMessageStruct);

                        if((returnList.Count + 1) >= _majorityNumber){ // number of promises + 1 (this server)
                            return returnList;
                        }

                    }else{
                        throw new LeaseManagerTimedOutException("Accept");
                    }

                }catch(LeaseManagerTimedOutException e){
                    throw e;
                }catch(RpcException e){
                    if(e.StatusCode == StatusCode.FailedPrecondition){
                        Console.WriteLine("One lease manager had read timestamp higher than write timestamp, ignoring...");
                    }else{
                        Console.WriteLine("An error ocurred in one of the requests to the lease managers, probably crashed");
                    }
                }
            }

            throw new Exception("Could not get majority of promise messages");

        }



        
    }
}