namespace TransactionManager;

public class ClientRequestHandler
{
    Queue<Request> _requestQueue = new Queue<Request>();
    TransactionManagerState _transactionManagerState;
    private Dictionary<string, ManualResetEvent> transactionSignals = new ();
    public bool isUpdating { get; set; }
    public bool isCrashed { get; set; }
    public ManualResetEvent canClose { get; set; }
    public LeaseHandler _leaseHandler;
    private TransactionManagerPropagateService _tmPropagateService;
    
    public ClientRequestHandler(TransactionManagerState transactionManagerState, LeaseHandler leaseHandler, 
                                TransactionManagerPropagateService tmPropagateService)
    {
        _transactionManagerState = transactionManagerState;
        isUpdating = false;
        isCrashed = false;
        canClose = new ManualResetEvent(false);
        _leaseHandler = leaseHandler;
        _tmPropagateService = tmPropagateService;
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
    
    public Dictionary<string, long> ExecuteTransaction()
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

        var transactionsToBroadcast = new Dictionary<string, long>();
        foreach (var dadInt in objectsToWrite)
        {
            string key = dadInt.Key;
            long value = dadInt.Value;
            
            _transactionManagerState.WriteOperation(key, value);
            transactionsToBroadcast.Add(key, value);
        }
        NotifyTransactionCompletion(transactionId);
        return transactionsToBroadcast;
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

    public async void ProcessTransactions()
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
                    var transactionId = requestToCheck.TransactionId;
                    var objectsLockNeeded = requestToCheck.ObjectsLockNeeded;
                    
                    // Check if has necessary lease
                    Task.Run(() => _leaseHandler.AskForPermissionToExecuteTransaction(transactionId, objectsLockNeeded));
                    _leaseHandler.WaitForLease().WaitOne();
                    _leaseHandler.ResetLeaseSignal();
                    
                    while (isUpdating)
                    {
                        Thread.Sleep(250);
                        Console.WriteLine("Waiting for update to finish");
                    }

                    if (!isCrashed)
                    {
                        var transactions = ExecuteTransaction();
                        if (transactions.Count != 0)
                        {
                            _tmPropagateService.BroadcastTransaction(transactionId, transactions);
                            _tmPropagateService.WaitForMajorityResponse();
                        }

                        _leaseHandler.TransactionFinished();
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