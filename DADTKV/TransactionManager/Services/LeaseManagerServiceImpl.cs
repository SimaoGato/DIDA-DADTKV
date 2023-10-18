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
        private SharedContext _sharedContext;
        private string _tmNick;

        public LeaseManagerServiceImpl(string tmNick, TransactionManagerState transactionManagerState, int numberOfLeaseManagers,
            SharedContext sharedContext)
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
            _sharedContext = sharedContext;
        }

        public override Task<SendLeaseResponse> SendLeases(SendLeaseRequest request, ServerCallContext context)
        {
            try
            {
                Console.WriteLine($"[LeaseManagerServiceImpl] {_tmNick} received leases");
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
                    var leases = request.Leases.Select(lease => lease.Value.ToList()).ToList();
                    _transactionManagerState.ReceiveLeases(leases);

                    if (_transactionManagerState.GetLeasesPerLeaseManager().Count == _numberOfLeaseManagers)
                    {
                        Console.WriteLine("[LeaseManagerServiceImpl] {0} sending signal to start transaction", _tmNick);
                        _sharedContext.SetLeaseSignal();

                        _sharedContext.LeaseSignal.Reset();

                        _transactionManagerState.ClearLeasesPerLeaseManager();
                    }

                    return new SendLeaseResponse
                    {
                        Ack = true,
                    };
                }
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
