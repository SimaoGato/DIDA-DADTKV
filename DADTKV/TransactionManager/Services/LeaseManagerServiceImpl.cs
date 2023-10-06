using Grpc.Core;
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
            _transactionManagerState = transactionManagerState;
            _numberOfLeaseManagers = numberOfLeaseManagers;
        }

        public override Task<SendLeaseResponse> SendLeases(SendLeaseRequest request, ServerCallContext context)
        {
            var response = DoSendLeases(request);
            return Task.FromResult(response);
        }

        private SendLeaseResponse DoSendLeases(SendLeaseRequest request)
        {
            var leases = request.Leases.Select(lease => lease.Value.ToList()).ToList();
            _transactionManagerState.ReceiveLeases(leases);

            return new SendLeaseResponse
            {
                Ack = true,
            };
        }
    }
}