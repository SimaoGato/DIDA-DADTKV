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
        private static int signalingTaskId = 0;
        
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
        
        // setter for signalingTaskId
        public void SetSignalingTaskId(int value) {
            signalingTaskId = value;
        }

        public override Task<SendLeaseResponse> SendLeases(SendLeaseRequest request, ServerCallContext context)
        {
            try
            {
                Console.WriteLine($"[LeaseManagerServiceImpl] {_tmNick} received leases AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA");
                var response = DoSendLeases(request);
                Console.WriteLine($"[LeaseManagerServiceImpl] {_tmNick} received leases BBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBB");
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
                    Console.WriteLine($"COUNT LEASES: {_transactionManagerState.GetLeasesPerLeaseManager().Count + 1}");
                    var leases = request.Leases.Select(lease => lease.Value.ToList()).ToList();
                    _transactionManagerState.ReceiveLeases(leases);
                    //if (_transactionManagerState.GetLeasesPerLeaseManager().Count >= (_numberOfLeaseManagers / 2) + 1) {
                    //Console.WriteLine($"[LeaseManagerServiceImpl] {_tmNick} received all leases");
                    //lock (this) {
                    // print signaallingTaskId 
                    //Console.WriteLine($"[LeaseManagerServiceImpl] {signalingTaskId}");
                    //if (!_sharedContext.ExecutingTransactions) {
                        Console.WriteLine(
                            $"[LeaseManagerServiceImpl] {_tmNick} sending signal: count {_transactionManagerState.GetLeasesPerLeaseManager().Count}");
                        _sharedContext.SetLeaseSignal();
                        //signalingTaskId++;
                        _sharedContext.LeaseSignal.Reset();
                        Console.WriteLine(
                            $"[LeaseManagerServiceImpl] {_tmNick} sent signal: count {_transactionManagerState.GetLeasesPerLeaseManager().Count}");
                    //}
                    //}
                    //}
                    
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
