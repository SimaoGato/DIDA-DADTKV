using Grpc.Net.Client;
using System;
using System.Collections.Generic;

namespace TransactionManager
{
    public class TransactionManagerService
    {
        private readonly Dictionary<string, GrpcChannel> _leaseManagersGrpcChannels = new Dictionary<string, GrpcChannel>();
        private Dictionary<string, LeaseService.LeaseServiceClient> _leaseManagersStubs = new Dictionary<string, LeaseService.LeaseServiceClient>();
        private Dictionary<string, bool> _leaseManagersSuspected = new Dictionary<string, bool>();

        public TransactionManagerService(List<string> lmAddresses)
        {
            foreach (var lmAddress in lmAddresses)
            {
                try
                {
                    var channel = GrpcChannel.ForAddress(lmAddress);
                    _leaseManagersGrpcChannels.Add(lmAddress, channel);
                    _leaseManagersStubs.Add(lmAddress, new LeaseService.LeaseServiceClient(channel));
                    _leaseManagersSuspected.Add(lmAddress, false);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error while creating channel for address {lmAddress}: {ex.Message}");
                }
            }
        }

        public void CloseLeaseManagerStubs()
        {
            foreach (var ch in _leaseManagersGrpcChannels)
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

        public bool RequestLease(string transactionManagerId, List<string> objectsRequested)
        {
            var request = new LeaseRequest
            {
                Value = { transactionManagerId, objectsRequested }
            };

            foreach (var lmStub in _leaseManagersStubs)
            {
                try
                {
                    if (_leaseManagersSuspected[lmStub.Key] == false)
                    {
                        var response = lmStub.Value.RequestLease(request);
                        Console.WriteLine($"[TransactionManagerService] Response from lm {lmStub.Key}: {response.Ack}");
                        if (!response.Ack)
                        {
                            return false;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error while making RequestLease call: {ex.Message}");
                }
            }

            return true;
        }
        
        public void RemoveLeaseManagerStub(string lmServerAddress)
        {
            _leaseManagersGrpcChannels[lmServerAddress].ShutdownAsync().Wait();
            _leaseManagersGrpcChannels.Remove(lmServerAddress);
            _leaseManagersStubs.Remove(lmServerAddress);
        }
        
        public void ResetSuspects()
        {
            foreach (var lm in _leaseManagersSuspected)
            {
                _leaseManagersSuspected[lm.Key] = false;
            }
        }
        
        public void SuspectLeaseManager(string lmServerAddress)
        {
            _leaseManagersSuspected[lmServerAddress] = true;
        }
    }
}
