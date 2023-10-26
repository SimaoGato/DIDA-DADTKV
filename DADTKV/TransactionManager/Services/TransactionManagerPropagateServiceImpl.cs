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
        public TransactionManagerPropagateServiceImpl()
        {
            
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
                // if message not in delivered_messages
                // save to state
                // message to true in delivered_messages
                // broadcast state modification
                return (new PropagateResponse { Ack = true });
            }
            catch (Exception)
            {
                return new PropagateResponse { Ack = false };
            }
        }
    }
}