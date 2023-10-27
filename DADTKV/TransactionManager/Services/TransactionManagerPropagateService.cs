using Grpc.Net.Client;
using System;
using System.Collections.Generic;

namespace TransactionManager
{
    public class TransactionManagerPropagateService
    {
        private readonly Dictionary<string, GrpcChannel> _transactionManagersGrpcChannels = new ();
        private Dictionary<string, TmService.TmServiceClient> _transactionManagersStubs = new ();
        //private Dictionary<string, string> _tmNickMap;
        private List<List<string>> _suspectedList;
        private string _tmNick;

        public TransactionManagerPropagateService(Dictionary<string, string> tmNickMap, string tmNick)
        {
            //_tmNickMap = tmNickMap;
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

            var abortFlag = true;
            foreach (var tmStub in _transactionManagersStubs)
            {
                try
                {
                    if (!IsSuspected(tmStub.Key))
                    {
                        var response = await tmStub.Value.PropagateTransactionAsync(request);
                        Console.WriteLine(
                            $"[TransactionManagerPropagateService] Transaction Response from tm {tmStub.Key}: {response.Ack}");
                        if (response.Ack) abortFlag = false;
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

        public async void BroadcastLease()
        {
            // TODO HOW TO SEND LEASE, WITH LIST<STRING> ??? -- not done
            var request = new Lease { };
            foreach (var tmStub in _transactionManagersStubs)
            {
                try
                {
                    var response = await tmStub.Value.PropagateLeaseAsync(request);
                    Console.WriteLine($"[TransactionManagerPropagateService] Lease Response from tm {tmStub.Key}: {response.Ack}");
                    // TODO WHAT TO DO WITH THE RESPONSE
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
