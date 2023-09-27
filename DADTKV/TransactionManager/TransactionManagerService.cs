using Grpc.Net.Client;

namespace TransactionManager;

public class TransactionManagerService
{
    
    private LeaseService.LeaseServiceClient _transactionManagerStub;
    
    public TransactionManagerService(string serverHostname, int serverPort) 
    {
        var channel = GrpcChannel.ForAddress("http://" + serverHostname + ":" + serverPort.ToString());
        _transactionManagerStub = new LeaseService.LeaseServiceClient(channel);
    }

    public bool RequestLease(string transactionManagerId, List<string> objectsRequested)
    {
        var request = new LeaseRequest
        {
            TransactionManagerId = transactionManagerId,
            ObjectsRequested = { objectsRequested }
        };
        
        var response = _transactionManagerStub.RequestLease(request);
        
        Console.WriteLine("Response: {0}", response.Ack);

        return response.Ack;

    }
    
}