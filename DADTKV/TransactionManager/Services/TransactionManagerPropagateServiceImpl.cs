using Grpc.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace TransactionManager
{
    public class TransactionManagerPropagateServiceImpl : TmService.TmServiceBase
    {
        private readonly TransactionManagerState _transactionManagerState;
        private TransactionManagerPropagateService _tmPropagateService;
        private LeaseHandler _leaseHandler;
            
        public TransactionManagerPropagateServiceImpl(TransactionManagerState transactionManagerState, 
                                                        TransactionManagerPropagateService tmPropagateService,
                                                        LeaseHandler leaseHandler)
        {
            _transactionManagerState = transactionManagerState;
            _tmPropagateService = tmPropagateService;
            _leaseHandler = leaseHandler;
        }

        public override Task<PropagateResponse> PropagateTransaction(Transaction request, ServerCallContext context)
        {
            try
            {
                return Task.FromResult(DoPropagateTransaction(request));
            }
            catch (Exception)
            {
                return Task.FromResult(new PropagateResponse { Ack = false });
            }
        }

        private PropagateResponse DoPropagateTransaction(Transaction request)
        {
            try
            {
                lock (this)
                {
                    var transactionId = request.TransactionId;
                    if (!_tmPropagateService.HasReceivedTransaction(transactionId))
                    {
                        _tmPropagateService.AddTransactionReceived(transactionId);
                        var transactionToPropagate = new Dictionary<string, long>();
                        foreach (var transaction in request.Transactions)
                        {
                            _transactionManagerState.WriteOperation(transaction.Key, transaction.Value);
                            transactionToPropagate.Add(transaction.Key, transaction.Value);
                        }
                        _tmPropagateService.BroadcastTransaction(transactionId, transactionToPropagate);
                    }
                    
                }
                // TODO USE ABORTFLAG
                return new PropagateResponse { Ack = true };
            }
            catch (Exception)
            {
                return new PropagateResponse { Ack = false };
            }
        }
        
        public override Task<PropagateResponse> ReleaseLease(ObjectsNeeded request, ServerCallContext context)
        {
            try
            {
                return Task.FromResult(DoReleaseLease(request));
            }
            catch (Exception)
            {
                return Task.FromResult(new PropagateResponse { Ack = false });
            }
        }
        
        private PropagateResponse DoReleaseLease(ObjectsNeeded request)
        {
            try
            {
                lock (this)
                {
                    var objectsToRelease = new List<string>();
                    foreach (var obj in request.DadInt)
                    {
                        objectsToRelease.Add(obj);
                    }
                    Console.WriteLine($"[TransactionManagerPropagateServiceImpl] Forced to release lease for objects: {string.Join(", ", objectsToRelease)}");
                    _leaseHandler.ReleaseLease(objectsToRelease);
                }
                return new PropagateResponse { Ack = true };
            }
            catch (Exception)
            {
                return new PropagateResponse { Ack = false };
            }
        }
        
        public override Task<PropagateResponse> PropagateLease(Lease request, ServerCallContext context)
        {
            try
            {
                return Task.FromResult(DoPropagateLease(request));
            }
            catch (Exception)
            {
                return Task.FromResult(new PropagateResponse { Ack = false });
            }
        }
        
        private PropagateResponse DoPropagateLease(Lease request)
        {
            try
            {
                lock (this)
                {
                    var leaseId = request.LeaseId;
                    if (!_tmPropagateService.HasReceivedLeaseReleased(leaseId))
                    {
                        _tmPropagateService.AddLeaseReleasedReceived(leaseId);
                        var objectsToPropagate = new List<string>();
                        foreach (var obj in request.Value)
                        {
                            objectsToPropagate.Add(obj);
                        }
                        _leaseHandler.RemoveLease(objectsToPropagate);
                        _leaseHandler._LeaseReleased = true;
                        _leaseHandler.SetLeaseReleasedSignal();
                        _tmPropagateService.BroadcastLeaseReleased(leaseId, objectsToPropagate);
                    }
                    
                }
                // TODO USE ABORTFLAG
                return new PropagateResponse { Ack = true };
            }
            catch (Exception)
            {
                return new PropagateResponse { Ack = false };
            }
        }
        
    }
}