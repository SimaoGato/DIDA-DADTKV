using Grpc.Net.Client;

namespace TransactionManager;

public class TransactionManagerService
{
    
    private List<LeaseService.LeaseServiceClient> _leaseManagersStubs = new List<LeaseService.LeaseServiceClient>();
    
    public TransactionManagerService(List<string> lmAddresses)
    {
        foreach (var lmAddress in lmAddresses)
        {
            var channel = GrpcChannel.ForAddress(lmAddress);
            _leaseManagersStubs.Add(new LeaseService.LeaseServiceClient(channel));
        }
    }

    public bool RequestLease(string transactionManagerId, List<string> objectsRequested)
    {
        var request = new LeaseRequest
        {
            Value = { transactionManagerId, objectsRequested }
        };
        
        foreach (var lmStub in _leaseManagersStubs)
        {
            var response = lmStub.RequestLease(request);
            if (!response.Ack)
            {
                return false;
            }
        }

        return true;
    }
    
}