using Grpc.Core;

namespace LeaseManager;

public class TransactionManagerServiceImpl : LeaseService.LeaseServiceBase
{
    List<List<string>> _requestedLeases = new List<List<string>>();
    
    public override Task<LeaseResponse> RequestLease(LeaseRequest request, ServerCallContext context)
    {
        return Task.FromResult(DoLeaseRequest(request));
    }
    
    private LeaseResponse DoLeaseRequest(LeaseRequest request)
    {
        List<string> leaseRequested = new List<string>();
        
        string transactionManagerId = request.TransactionManagerId;
        
        leaseRequested.Add(transactionManagerId);
        leaseRequested.AddRange(request.ObjectsRequested);
        
        _requestedLeases.Add(leaseRequested);
        
        //print out the requested lease
        Console.WriteLine("Lease Requested: {0}", string.Join(", ", leaseRequested));

        LeaseResponse response = new LeaseResponse
        {
            Ack = true
        };

        return response;
    }
}