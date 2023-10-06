using Grpc.Net.Client;
using System;
using System.Collections.Generic;

namespace TransactionManager
{
    public class TransactionManagerService
    {
        private readonly List<GrpcChannel> _leaseManagersGrpcChannels = new List<GrpcChannel>();
        private List<LeaseService.LeaseServiceClient> _leaseManagersStubs = new List<LeaseService.LeaseServiceClient>();

        public TransactionManagerService(List<string> lmAddresses)
        {
            foreach (var lmAddress in lmAddresses)
            {
                try
                {
                    var channel = GrpcChannel.ForAddress(lmAddress);
                    _leaseManagersGrpcChannels.Add(channel);
                    _leaseManagersStubs.Add(new LeaseService.LeaseServiceClient(channel));
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
                    ch.ShutdownAsync().Wait();
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
                    var response = lmStub.RequestLease(request);
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
    }
}
