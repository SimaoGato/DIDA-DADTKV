namespace TransactionManager;

public class ClientRequestHandler
{
    Queue<Request> _requestQueue = new Queue<Request>();
    TransactionManagerState _transactionManagerState;
    private Dictionary<string, ManualResetEvent> transactionSignals = new Dictionary<string, ManualResetEvent>();
    public bool isUpdating { get; set; }
    public bool isCrashed { get; set; }
    public ManualResetEvent canClose { get; set; }
    
    public ClientRequestHandler(TransactionManagerState transactionManagerState)
    {
        _transactionManagerState = transactionManagerState;
        isUpdating = false;
        isCrashed = false;
        canClose = new ManualResetEvent(false);
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
    
    public void AbortAllTransactions()
    {
        DadInt dadInt = new DadInt
        {
            Key = "abort",
            Value = 0 // code for crash
        };
        while (!IsEmpty())
        {
            Request requestToAbort = PopRequest();
            requestToAbort.AddTransactionResult(dadInt);
            NotifyTransactionCompletion(requestToAbort.TransactionId);
        }
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
        try
        {
            while (true)
            {
                Thread.Sleep(1);
                if(!IsEmpty())
                {
                    // Check first request in queue
                    Request requestToCheck = CheckTopRequest();
                    var objectsLockNeeded = requestToCheck.ObjectsLockNeeded;
                    
                    // Check if has necessary lease
                    // leaseHandler.AskForLease(objectsLockNeeded).waitOne();
                    
                    while (isUpdating)
                    {
                        Thread.Sleep(250);
                        Console.WriteLine("Waiting for update to finish");
                    }

                    if (!isCrashed)
                    {
                        ExecuteTransaction();
                    }
                }

                if (isCrashed)
                {
                    AbortAllTransactions();
                    canClose.Set();
                }
            }
        } catch (ThreadInterruptedException)
        {
            Console.WriteLine("Worker thread was interrupted.");
        }
    }
}