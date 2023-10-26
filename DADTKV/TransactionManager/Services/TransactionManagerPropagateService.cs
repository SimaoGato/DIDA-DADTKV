using Grpc.Net.Client;
using System;
using System.Collections.Generic;

namespace TransactionManager
{
    public class TransactionManagerPropagateService
    {
        private readonly Dictionary<string, GrpcChannel> _transactionManagersGrpcChannels = new ();
        private Dictionary<string, TmService.TmServiceClient> _transactionManagersStubs = new ();

        public TransactionManagerPropagateService(List<string> tmAddresses)
        {
            foreach (var tmAddress in tmAddresses)
            {
                try
                {
                    var channel = GrpcChannel.ForAddress(tmAddress);
                    _transactionManagersGrpcChannels.Add(tmAddress, channel);
                    _transactionManagersStubs.Add(tmAddress, new TmService.TmServiceClient(channel));
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error while creating channel for address {tmAddress}: {ex.Message}");
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

        public async void BroadcastTransaction(Dictionary<string, long> transaction)
        {
            var request = new Transaction();
            foreach (var dadInt in transaction)
            {
                request.Transactions.Add(new DadIntObj {Key = dadInt.Key, Value = dadInt.Value});
            }
            foreach (var tmStub in _transactionManagersStubs)
            {
                try
                {
                    var response = await tmStub.Value.PropagateTransactionAsync(request);
                    Console.WriteLine($"[TransactionManagerPropagateService] Response from tm {tmStub.Key}: {response.Response}");
                    // if (!response.Ack)
                    // {
                    //     return false;
                    // }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error while making BroadcastTransaction call: {ex.Message}");
                }
            }
        }

        
        
        public void RemoveTransactionManagerStub(string tmServerAddress)
        {
            _transactionManagersGrpcChannels[tmServerAddress].ShutdownAsync().Wait();
            _transactionManagersGrpcChannels.Remove(tmServerAddress);
            _transactionManagersStubs.Remove(tmServerAddress);
        }
    }
}
