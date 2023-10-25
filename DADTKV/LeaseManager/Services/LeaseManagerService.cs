using Grpc.Net.Client;

namespace LeaseManager;

public class LeaseManagerService
{
    private Dictionary<string, GrpcChannel> _channels; 
    private Dictionary<string, LeaseResponseService.LeaseResponseServiceClient> _transactionManagersStubs;

    public LeaseManagerService(Dictionary<string, string> servers)
    {
        _channels = new Dictionary<string, GrpcChannel>();
        _transactionManagersStubs = new Dictionary<string, LeaseResponseService.LeaseResponseServiceClient>();
        foreach (var server in servers)
        {
            var channel = GrpcChannel.ForAddress(server.Value);
            _transactionManagersStubs.Add(server.Key, new LeaseResponseService.LeaseResponseServiceClient(channel));
            _channels.Add(server.Key, channel);
        }
    }
    
    public bool SendLeases(int round, List<List<string>> leases)
    {
        var request = new SendLeaseRequest { Round = round };

        foreach (var l in leases)
        {
            var lease = new LeaseOrder();
            lease.Value.AddRange(l);
            request.Leases.Add(lease);
        }

        foreach (var tmStub in _transactionManagersStubs)
        {
            var response = tmStub.Value.SendLeases(request);
            if (!response.Ack)
            {
                return false;
            }
        }

        return true;

    }
    
    public void RemoveStub(string nickname)
    {
        _channels[nickname].ShutdownAsync().Wait();
        _channels.Remove(nickname);
        _transactionManagersStubs.Remove(nickname);
    }
    
    public void CloseStubs()
    {
        foreach (var ch in _channels)
        {
            ch.Value.ShutdownAsync().Wait();
        }
    }
    
}