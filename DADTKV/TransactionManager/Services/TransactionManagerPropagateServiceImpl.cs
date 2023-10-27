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
        private Dictionary<string, long> _transactionsToPropagate = new ();
        private List<string> _transactionsReceived = new ();
            
        public TransactionManagerPropagateServiceImpl(TransactionManagerState transactionManagerState, 
                                                        TransactionManagerPropagateService tmPropagateService)
        {
            _transactionManagerState = transactionManagerState;
            _tmPropagateService = tmPropagateService;
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
                    Console.WriteLine("[PropagateServiceImpl] TM received propagate transactions");
                    
                    var transactionId = request.TransactionId;
                    if (!_transactionsReceived.Contains(transactionId))
                    {
                        Console.WriteLine($"[PropagateServiceImpl] TM received propagate transactions for the first time");
                        _transactionsReceived.Add(transactionId);
                        var transactionToPropagate = new Dictionary<string, long>();
                        foreach (var transaction in request.Transactions)
                        {
                            _transactionManagerState.WriteOperation(transaction.Key, transaction.Value);
                            transactionToPropagate.Add(transaction.Key, transaction.Value);
                        }
                        
                        _tmPropagateService.BroadcastTransaction(transactionId, transactionToPropagate);
                    }
                    else
                    {
                        Console.WriteLine($"[PropagateServiceImpl] TM already received propagate transactions");
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