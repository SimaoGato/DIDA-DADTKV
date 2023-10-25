using Grpc.Core;

namespace LeaseManager;

public class TransactionManagerServiceImpl : LeaseService.LeaseServiceBase
{
    private LeaseManagerState _lmState;
    
    public TransactionManagerServiceImpl(LeaseManagerState lmState)
    {
        _lmState = lmState;
    }
    
    public override Task<LeaseResponse> RequestLease(LeaseRequest request, ServerCallContext context)
    {
        return Task.FromResult(DoLeaseRequest(request));
    }
    
    private LeaseResponse DoLeaseRequest(LeaseRequest request)
    {
        List<string> leaseRequested = request.Value.ToList();
        _lmState.AddLease(leaseRequested);

        LeaseResponse response = new LeaseResponse
        {
            Ack = true
        };

        return response;
    }
}