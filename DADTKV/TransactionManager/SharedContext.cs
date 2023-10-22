public class SharedContext
{
    private ManualResetEvent _leaseSignal = new ManualResetEvent(false);
    private Dictionary<string, ManualResetEvent> transactionSignals = new Dictionary<string, ManualResetEvent>();
    
    private ManualResetEvent _sharedContextSignal = new ManualResetEvent(false);

    public ManualResetEvent LeaseSignal => _leaseSignal;

    public void SetLeaseSignal()
    {
        _leaseSignal.Set();
    }
    
    // getter for sharedContextSignal so tm can send signal to proceed to next transaction
    public ManualResetEvent SharedContextSignal => _sharedContextSignal;
    
    public void SetSharedContextSignal()
    {
        _sharedContextSignal.Set();
    }
    
    // getter for transactionSignals, if dont have the key, create a new one
    public ManualResetEvent TransactionSignal(string clientId)
    {
        if (!transactionSignals.TryGetValue(clientId, out var signal))
        {
            signal = new ManualResetEvent(false);
            transactionSignals[clientId] = signal;
        }
        return signal;
    }
    
    public void SetTransactionSignal(string clientId)
    {
        if (transactionSignals.TryGetValue(clientId, out var signal))
        {
            signal.Set();
        }
    }
    
    public void RemoveTransactionSignal(string clientId)
    {
        transactionSignals.Remove(clientId);
        // print transactionSignals count left
        Console.WriteLine($"[SharedContext] {transactionSignals.Count} transaction signals left");
    }
    
    public void ResetTransactionSignal(string clientId)
    {
        if (transactionSignals.TryGetValue(clientId, out var signal))
        {
            signal.Reset();
        }
    }
    
    public int ExecuteTransactions(List<List<string>> leaseOrder)
    {
        var numOfTransactions = 0;
        foreach (var transaction in leaseOrder)
        {
            // clientId is the second element of the list
            var clientId = transaction[1];
            Console.WriteLine($"[SharedContext] {clientId} starting transaction");
            SetTransactionSignal(clientId);
            ResetTransactionSignal(clientId);
            // wait for signal to proceed to next transaction
            Console.WriteLine($"[SharedContext] {clientId} waiting for signal to proceed to next transaction");
            _sharedContextSignal.WaitOne();
            Console.WriteLine($"[SharedContext] {clientId} received signal to proceed to next transaction");
            //_sharedContextSignal.Reset();
            numOfTransactions++;
        }
        return numOfTransactions;
    }
}