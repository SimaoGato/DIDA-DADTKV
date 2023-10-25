namespace TransactionManager;

public class ClientRequestHandler
{
    Queue<Request> _requestQueue = new Queue<Request>();
    TransactionManagerState _transactionManagerState;
    private Dictionary<string, ManualResetEvent> transactionSignals = new Dictionary<string, ManualResetEvent>();
    
    public ClientRequestHandler(TransactionManagerState transactionManagerState)
    {
        _transactionManagerState = transactionManagerState;
    }
    
    public ManualResetEvent WaitForTransaction(string transactionId)
    {
        lock (this)
        {
            if (!transactionSignals.TryGetValue(transactionId, out var signal))
            {
                signal = new ManualResetEvent(false);
                transactionSignals[transactionId] = signal;
            }
            return signal;
        }
    }
    
    public void NotifyTransactionCompletion(string transactionId)
    {
        lock (this)
        {
            if (transactionSignals.TryGetValue(transactionId, out var signal))
            {
                signal.Set();
            }
        }

    }
    
    public void PushRequest(Request request)
    {
        lock (_requestQueue)
        {
            
            _requestQueue.Enqueue(request);
        }
    }
    
    public Request PopRequest()
    {
        return _requestQueue.Dequeue();
    }
    
    public Request CheckTopRequest()
    {
        return _requestQueue.Peek();
    }
    
    public bool IsEmpty()
    {
        return _requestQueue.Count == 0;
    }
    
    public void ExecuteTransaction()
    {
        Request requestToExecute = PopRequest();
        var transactionId = requestToExecute.TransactionId;
        var objectsToRead = requestToExecute.ObjectsToRead;
        var objectsToWrite = requestToExecute.ObjectsToWrite;
        foreach (string dadIntKey in objectsToRead)
        {
            if (_transactionManagerState.ContainsKey(dadIntKey))
            {
                DadInt dadInt = new DadInt
                {
                    Key = dadIntKey,
                    Value = _transactionManagerState.ReadOperation(dadIntKey)
                };
                requestToExecute.AddTransactionResult(dadInt);
            }
        }
        foreach (var dadInt in objectsToWrite)
        {
            string key = dadInt.Key;
            long value = dadInt.Value;
            
            _transactionManagerState.WriteOperation(key, value);
        }
        NotifyTransactionCompletion(transactionId);
        Console.WriteLine("Transaction {0} completed", transactionId);
    }
    
    public void PrintQueue()
    {
        Console.WriteLine("Printing queue");
        foreach (var item in _requestQueue)
        {
            item.PrintRequest();
        }
    }
    
    public void ProcessTransactions()
    {
        while (true)
        {
            Thread.Sleep(2000);
            if (!IsEmpty())
            {
                ExecuteTransaction();
                Console.WriteLine("Transaction executed");
            }
        }
    }
}