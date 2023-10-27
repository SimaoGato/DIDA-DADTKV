using Grpc.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace TransactionManager
{
    public class LeaseManagerServiceImpl : LeaseResponseService.LeaseResponseServiceBase
    {
        private readonly TransactionManagerState _transactionManagerState;
        private readonly int _numberOfLeaseManagers;
        private string _tmNick;
        private int _round = 1;
        private ManualResetEvent _leaseReceivedSignal;
        private LeaseHandler _leaseHandler;
        
        public LeaseManagerServiceImpl(string tmNick, TransactionManagerState transactionManagerState, 
            int numberOfLeaseManagers, LeaseHandler leaseHandler, ManualResetEvent leaseReceivedSignal)
        {
            if (transactionManagerState == null)
            {
                throw new ArgumentNullException(nameof(transactionManagerState), "TransactionManagerState cannot be null.");
            }

            if (numberOfLeaseManagers <= 0)
            {
                throw new ArgumentException("Number of Lease Managers must be greater than zero.", nameof(numberOfLeaseManagers));
            }

            _tmNick = tmNick;
            _transactionManagerState = transactionManagerState;
            _numberOfLeaseManagers = numberOfLeaseManagers;
            _leaseReceivedSignal = leaseReceivedSignal;
            _leaseHandler = leaseHandler;
        }

        public override Task<SendLeaseResponse> SendLeases(SendLeaseRequest request, ServerCallContext context)
        {
            try
            {
                var response = DoSendLeases(request);
                return Task.FromResult(response);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error while sending leases: {ex.Message}");
                var errorResponse = new SendLeaseResponse
                {
                    Ack = false,
                };
                return Task.FromResult(errorResponse);
            }
        }

        private SendLeaseResponse DoSendLeases(SendLeaseRequest request)
        {
            try
            {
                lock (this)
                {
                    if (request.Round == _round)
                    { 
                        Console.WriteLine($"[LeaseManagerServiceImpl] Received leases from Paxos round {_round}.");
                        var leases = request.Leases.Select(lease => lease.Value.ToList()).ToList();
                        _leaseHandler.ReceiveLeases(leases);
                        _leaseReceivedSignal.Set();
                        _round++; // Now i want only to see leases of the next round
                    }

                    return new SendLeaseResponse { Ack = true };
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[LeaseManagerServiceImpl] Error while sending leases: {ex.Message}");
                return new SendLeaseResponse
                {
                    Ack = false,
                };
            }
        }
    }
}