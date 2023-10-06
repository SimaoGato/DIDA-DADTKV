using Grpc.Net.Client;

namespace LeaseManager;

public class LeaseManagerService
{
    List<GrpcChannel> _channels = new List<GrpcChannel>();
    private List<LeaseResponseService.LeaseResponseServiceClient> _transactionManagersStubs = 
        new List<LeaseResponseService.LeaseResponseServiceClient>();

    public LeaseManagerService(List<string> tmAddresses)
    {
        foreach (var tmAddress in tmAddresses)
        {
            var channel = GrpcChannel.ForAddress(tmAddress);
            _transactionManagersStubs.Add(new LeaseResponseService.LeaseResponseServiceClient(channel));
            _channels.Add(channel);
        }
    }
    
    public bool SendLeases(List<List<string>> leases)
    {
        var request = new SendLeaseRequest();

        foreach (var l in leases)
        {
            var lease = new LeaseOrder();
            lease.Value.AddRange(l);
            request.Leases.Add(lease);
        }

        foreach (var tmStub in _transactionManagersStubs)
        {
            var response = tmStub.SendLeases(request);
            if (!response.Ack)
            {
                return false;
            }
        }

        return true;

    }
    
    public void CloseStubs()
    {
        foreach (var ch in _channels)
        {
            ch.ShutdownAsync().Wait();
        }
    }
    
}