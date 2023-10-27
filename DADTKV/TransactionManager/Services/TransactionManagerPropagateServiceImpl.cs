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
                    _transactionsToPropagate.Clear();
                    foreach (var transaction in request.Transactions)
                    {
                        if (!_transactionManagerState.ContainsDadInt(transaction.Key, transaction.Value))
                        {
                            Console.Write("hi: " + transaction.Key);
                            _transactionManagerState.WriteOperation(transaction.Key, transaction.Value);
                            _transactionsToPropagate.Add(transaction.Key, transaction.Value);
                        }
                    }

                    Console.WriteLine();
                    _transactionManagerState.PrintObjects();
                    Thread.Sleep(1000);

                    Console.Write("[PropagateServiceImpl] TM broadcast saved transactions: ");
                    foreach (var x in _transactionsToPropagate)
                    {
                        Console.Write(x.Key + "-" + x.Value + "//");
                    }

                    Console.WriteLine();

                }
                if (_transactionsToPropagate.Count == 0) Console.WriteLine("nothing to propagate");

                if (_transactionsToPropagate.Count != 0)
                {
                    _tmPropagateService.BroadcastTransaction(_transactionsToPropagate);
                }

                // TODO USE ABORTFLAG
                _transactionsToPropagate.Clear(); // clear transactions to propagate
                return new PropagateResponse { Ack = true };
            }
            catch (Exception)
            {
                return new PropagateResponse { Ack = false };
            }
        }
    }
}