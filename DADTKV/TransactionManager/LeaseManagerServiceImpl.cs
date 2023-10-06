using Grpc.Core;

namespace TransactionManager;

public class LeaseManagerServiceImpl : LeaseResponseService.LeaseResponseServiceBase
{
    
    private int _numberOfLeaseManagers;
    private TransactionManagerState _transactionManagerState;
    public bool canExecuteTransactions = false;
    
    public LeaseManagerServiceImpl(TransactionManagerState transactionManagerState, int numberOfLeaseManagers)
    {
        _transactionManagerState = transactionManagerState;
        _numberOfLeaseManagers = numberOfLeaseManagers;
    }
 
    public override Task<SendLeaseResponse> SendLeases(SendLeaseRequest request, ServerCallContext context)
    {
        return Task.FromResult(DoSendLeases(request));
    }

    private SendLeaseResponse DoSendLeases(SendLeaseRequest request)
    {
        List<List<string>> leases = new List<List<string>>();
        
        foreach (var lease in request.Leases)
        {
            leases.Add(lease.Value.ToList());
        }

        _transactionManagerState.ReceiveLeasesOrder(leases);

        var response = new SendLeaseResponse
        {
            Ack = true,
        };

        return response;
    }
    
}