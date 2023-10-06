using Grpc.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TransactionManager
{
    public class LeaseManagerServiceImpl : LeaseResponseService.LeaseResponseServiceBase
    {
        private readonly TransactionManagerState _transactionManagerState;
        private readonly int _numberOfLeaseManagers;

        public LeaseManagerServiceImpl(TransactionManagerState transactionManagerState, int numberOfLeaseManagers)
        {
            if (transactionManagerState == null)
            {
                throw new ArgumentNullException(nameof(transactionManagerState), "TransactionManagerState cannot be null.");
            }

            if (numberOfLeaseManagers <= 0)
            {
                throw new ArgumentException("Number of Lease Managers must be greater than zero.", nameof(numberOfLeaseManagers));
            }

            _transactionManagerState = transactionManagerState;
            _numberOfLeaseManagers = numberOfLeaseManagers;
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
                var leases = request.Leases.Select(lease => lease.Value.ToList()).ToList();
                _transactionManagerState.ReceiveLeases(leases);

                return new SendLeaseResponse
                {
                    Ack = true,
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error while sending leases: {ex.Message}");
                return new SendLeaseResponse
                {
                    Ack = false,
                };
            }
        }
    }
}
