using Grpc.Net.Client;
using System;
using System.Collections.Generic;

namespace TransactionManager
{
    public class TransactionManagerPropagateService
    {
        private readonly Dictionary<string, GrpcChannel> _transactionManagersGrpcChannels = new ();
        private Dictionary<string, TmService.TmServiceClient> _transactionManagersStubs = new ();
        private List<List<string>> _suspectedList;
        private List<string> _transactionsReceived = new ();
        private List<string> _leaseReleasedReceived = new ();
        private string _tmNick;
        private int _majorityCount;
        private ManualResetEvent _majorityResponseSignal = new ManualResetEvent(false);
        private Dictionary<string, string> _tmNickMap;

        public TransactionManagerPropagateService(Dictionary<string, string> tmNickMap, string tmNick)
        {
            _suspectedList = new List<List<string>>();
            _tmNickMap = tmNickMap;
            _tmNick = tmNick;
            foreach (var tm in tmNickMap)
            {
                try
                {
                    if (tmNick != tm.Key)
                    {
                        var channel = GrpcChannel.ForAddress(tm.Value);
                        _transactionManagersGrpcChannels.Add(tm.Value, channel);
                        _transactionManagersStubs.Add(tm.Value, new TmService.TmServiceClient(channel));
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error while creating channel for address {tm.Value}: {ex.Message}");
                }
            }
            _majorityCount = (_transactionManagersStubs.Count / 2) + 1;
        }
        
        public bool HasReceivedTransaction(string transactionId)
        {
            lock (_transactionsReceived)
            {
                return _transactionsReceived.Contains(transactionId);
            }
        }
        
        public bool HasReceivedLeaseReleased(string leaseId)
        {
            lock (_leaseReleasedReceived)
            {
                return _leaseReleasedReceived.Contains(leaseId);   
            }
        }
        
        public void AddTransactionReceived(string transactionId)
        {
            lock (_transactionsReceived)
            {
                // add transaction to received list, no duplicates
                if (!_transactionsReceived.Contains(transactionId))
                {
                    _transactionsReceived.Add(transactionId);
                }
            }
        }
        
        public void AddLeaseReleasedReceived(string leaseId)
        {
            lock (_leaseReleasedReceived)
            {
                // add lease to received list, no duplicates
                if (!_leaseReleasedReceived.Contains(leaseId))
                {
                    _leaseReleasedReceived.Add(leaseId);
                }
            }
        }

        public void CloseTransactionManagerStubs()
        {
            foreach (var ch in _transactionManagersGrpcChannels)
            {
                try
                {
                    ch.Value.ShutdownAsync().Wait();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error while shutting down channel: {ex.Message}");
                }
            }
        }

        public async void BroadcastTransaction(string transactionId, Dictionary<string, long> transactions)
        {
            if (transactions.Count == 0) return;
            
            var request = new Transaction();
            request.TransactionId = transactionId;
            foreach (var dadInt in transactions)
            {
                request.Transactions.Add(new DadIntObj {Key = dadInt.Key, Value = dadInt.Value});
            }
            AddTransactionReceived(transactionId);
            
            var responseCount = 0;
            
            var abortFlag = true;
            foreach (var tmStub in _transactionManagersStubs)
            {
                try
                {
                    if (!IsSuspected(tmStub.Key))
                    {
                        var response = await tmStub.Value.PropagateTransactionAsync(request);
                        //Console.WriteLine(
                         //   $"[TransactionManagerPropagateService] Transaction Response from tm {tmStub.Key}: {response.Ack}");
                        if (response.Ack) {
                            // abortFlag = false;
                            responseCount++;
                            if (responseCount >= _majorityCount)
                            {
                                NotifyMajorityResponse();
                                responseCount = 0;
                            }
                        }
                    }
                    else
                    {
                        Console.WriteLine("TM is suspected or crashed");
                    }
                }
                catch (Exception)
                {
                    Console.WriteLine($"Error while making BroadcastTransaction call");
                }
            }
            //return abortFlag;
        }

        public void WaitForMajorityResponse()
        {
            _majorityResponseSignal.WaitOne();
            _majorityResponseSignal.Reset();
        }
        
        public void NotifyMajorityResponse()
        {
            _majorityResponseSignal.Set();
        }

        public async void BroadcastLeaseReleased(string leaseId, List<string> objects)
        {
            var request = new Lease
            {
                Value = { objects },
                LeaseId = leaseId
            };
            AddLeaseReleasedReceived(leaseId);
            
            var abortFlag = true;
            foreach (var tmStub in _transactionManagersStubs)
            {
                try
                {
                    if (!IsSuspected(tmStub.Key))
                    {
                        var response = await tmStub.Value.PropagateLeaseAsync(request);
                        // Console.WriteLine(
                        //     $"[TransactionManagerPropagateService] Lease released Response from tm {tmStub.Key}: {response.Ack}");
                        if (response.Ack) abortFlag = false;
                    }
                    else
                    {
                        Console.WriteLine("TM is suspected or crashed");
                    }
                }
                catch (Exception)
                {
                    Console.WriteLine($"Error while making BroadcastLease call");
                }
            }
        }
        
        public void RemoveTransactionManagerStub(string tmServerAddress)
        {
            _transactionManagersGrpcChannels[tmServerAddress].ShutdownAsync().Wait();
            _transactionManagersGrpcChannels.Remove(tmServerAddress);
            _transactionManagersStubs.Remove(tmServerAddress);
            // tmServerAddress is the value. Grab the key
            var tmNick = "";
            foreach (var tm in _tmNickMap)
            {
                if (tm.Value == tmServerAddress)
                {
                    tmNick = tm.Key;
                    break;
                }
            }
        }

        public void ReleaseLease(string tmNick, List<string> objects)
        {
            // print objects
            Console.WriteLine($"[TransactionManagerPropagateService] Forcing {tmNick} release of lease for objects: {string.Join(",", objects)}");
            var url = _tmNickMap[tmNick];
            var tmStub = _transactionManagersStubs[url];
            var request = new ObjectsNeeded
            {
                DadInt = { objects }
            };
            var response = tmStub.ReleaseLease(request);
            Console.WriteLine($"[TransactionManagerPropagateService] ReleaseLease response from tm {tmNick}: {response.Ack}");
        }

        public void RemoveTransactionFromRequestedLeases(string transactionId)
        {
            lock (_leaseReleasedReceived)
            {
                if (_leaseReleasedReceived.Contains(transactionId))
                {
                    Console.WriteLine($"[TransactionManagerPropagateService] Removing transaction {transactionId} from requested leases");
                    _leaseReleasedReceived.Remove(transactionId);
                }
            }
        }
        
        public void CreateSuspectedList(string suspects)
        {
            if (suspects.Length == 0) return;
            var parts = suspects.Split("+");
            foreach (var part in parts)
            {
                var nicks = part.Trim('(',')').Split(",");
                //Console.WriteLine("suspects: " + nicks[0] + "-" + nicks[1]);
                _suspectedList.Add(new List<string> {nicks[0], nicks[1]});
            }
        }

        public void ClearSuspectedList()
        {
            _suspectedList = new List<List<string>>();
        }
        
        private bool IsSuspected(string nick)
        {
            var suspect = new List<string> { _tmNick, nick };
            if (_suspectedList.Any(sublist => sublist.SequenceEqual(suspect)))
            {
                Console.WriteLine($"[Broadcast] {_tmNick} suspects {nick}");
                return true;
            }

            return false;
        }
        
    }
}
