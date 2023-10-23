public class SharedContext
{
    private ManualResetEvent _leaseSignal = new ManualResetEvent(false);
    private Dictionary<string, ManualResetEvent> transactionSignals = new Dictionary<string, ManualResetEvent>();
    private bool _executingTransactions = false;
    
    private ManualResetEvent _sharedContextSignal = new ManualResetEvent(false);

    public ManualResetEvent LeaseSignal => _leaseSignal;

    public void SetLeaseSignal()
    {
        _leaseSignal.Set();
    }
    
    // getter for executingTransactions
    public bool ExecutingTransactions => _executingTransactions;
    
    // getter for sharedContextSignal so tm can send signal to proceed to next transaction
    public ManualResetEvent SharedContextSignal => _sharedContextSignal;
    
    public void SetSharedContextSignal()
    {
        lock (this)
        {
            _sharedContextSignal.Set();
        }
    }
    
    // getter for transactionSignals, if dont have the key, create a new one
    public ManualResetEvent TransactionSignal(string clientId)
    {
        lock (this)
        {
            if (!transactionSignals.TryGetValue(clientId, out var signal))
            {
                signal = new ManualResetEvent(false);
                transactionSignals[clientId] = signal;
            }
            Console.WriteLine($"[SharedContext] {transactionSignals.Count} transaction signals");
            return signal;
        }
    }
    
    public void SetTransactionSignal(string clientId)
    {
        lock (this)
        {
            if (transactionSignals.TryGetValue(clientId, out var signal))
            {
                signal.Set();
            }
        }

    }
    
    public void RemoveTransactionSignal(string clientId)
    {
        lock (this)
        {
            transactionSignals.Remove(clientId);
            // print transactionSignals count left
            Console.WriteLine($"[SharedContext] {transactionSignals.Count} transaction signals left");
        }

    }
    
    public void ResetTransactionSignal(string clientId)
    {
        lock (this)
        {
            if (transactionSignals.TryGetValue(clientId, out var signal))
            {
                signal.Reset();
            }
        }

    }
    
    public int ExecuteTransactions(List<List<string>> leaseOrder)
    {
        //_executingTransactions = true;
        var numOfTransactions = 0;
        Console.WriteLine($"[SharedContext] {leaseOrder.Count} transactions to execute");
        foreach (var transaction in leaseOrder)
        {
            // clientId is the second element of the list
            var server = transaction[0];
            //a transacao é minha?
            // se nao
            //      while (nao for minha vez)
            //              signal.waitone()
            //se sim
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
        //_executingTransactions = false;
        return numOfTransactions;
        
        
        // recebe grpc to tm que tinha a lease
        // propaga estadp
        // set do signal para este
    }
}