using Grpc.Net.Client;
using System;
using System.Collections.Generic;

namespace TransactionManager
{
    public class TransactionManagerLeaseService
    {
        private readonly Dictionary<string, GrpcChannel> _leaseManagersGrpcChannels = new Dictionary<string, GrpcChannel>();
        private Dictionary<string, LeaseService.LeaseServiceClient> _leaseManagersStubs = new Dictionary<string, LeaseService.LeaseServiceClient>();

        public TransactionManagerLeaseService(List<string> lmAddresses)
        {
            foreach (var lmAddress in lmAddresses)
            {
                try
                {
                    var channel = GrpcChannel.ForAddress(lmAddress);
                    _leaseManagersGrpcChannels.Add(lmAddress, channel);
                    _leaseManagersStubs.Add(lmAddress, new LeaseService.LeaseServiceClient(channel));
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
                    var response = lmStub.Value.RequestLease(request);
                    if (!response.Ack)
                    {
                        return false;
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
        
    }
}
