namespace TransactionManager;

public class LeaseHandler
{

    private readonly string _tmNick;
    private readonly ManualResetEvent _leaseSignal = new ManualResetEvent(false);
    private Dictionary<string, Queue<string>> _objectsQueue = new Dictionary<string, Queue<string>>();
    private Dictionary<string, bool> _requestedLeases = new Dictionary<string, bool>();
    private TransactionManagerLeaseService _transactionManagerLeaseService;
    private ManualResetEvent _leaseReceivedSignal;
    private ManualResetEvent _transactionSignal = new ManualResetEvent(false);
    private List<KeyValuePair<string, List<string>>> _ownedLeases = new List<KeyValuePair<string, List<string>>>();
    private TransactionManagerPropagateService _transactionManagerPropagateService;
    private ManualResetEvent _leaseReleasedSignal = new ManualResetEvent(false);
        
    public LeaseHandler(string tmNick, TransactionManagerLeaseService transactionManagerLeaseService, 
        ManualResetEvent leaseReceivedSignal, TransactionManagerPropagateService transactionManagerPropagateService)
    {
        _tmNick = tmNick;
        _transactionManagerLeaseService = transactionManagerLeaseService;
        _leaseReceivedSignal = leaseReceivedSignal;
        _transactionManagerPropagateService = transactionManagerPropagateService;
    }

    public void PushObjectQueue(string objectKey, string tmNick)
    {
        _objectsQueue.TryGetValue(objectKey, out var queue);
        if (queue == null)
        {
            queue = new Queue<string>();
            _objectsQueue[objectKey] = queue;
        }
        queue.Enqueue(tmNick);
    }
    
    public void PopObjectQueue(string objectKey)
    {
        _objectsQueue.TryGetValue(objectKey, out var queue);
        if (queue != null)
        {
            queue.Dequeue();
        }
    }
    
    private bool HasPermissionsToExecuteTransaction(List<string> objectsLockNeeded)
    {
        foreach (var objectKey in objectsLockNeeded)
        {
            _objectsQueue.TryGetValue(objectKey, out var queue);
            if (queue == null || queue.Count == 0 || queue.Peek() != _tmNick)
            {
                return false;
            }
        }
        return true;
    }
    
    private void RequestLease(string transactionId, List<string> objectsLockNeeded)
    {
        List<string> leaseRequest = new List<string>();
        leaseRequest.Add(transactionId);
        leaseRequest.AddRange(objectsLockNeeded);
        _transactionManagerLeaseService.RequestLease(_tmNick, leaseRequest);
        _requestedLeases[transactionId] = false;
    }
    
    private void WaitForPaxosResultSignal(string transactionId)
    {
        _leaseReceivedSignal.WaitOne();
        _leaseReceivedSignal.Reset();
        _requestedLeases[transactionId] = true;
    }
    
    private bool HasAlreadyRequestedLease(string transactionId)
    {
        _requestedLeases.TryGetValue(transactionId, out var hasRequested);
        return hasRequested;
    }
    
    private void AddOwnLease(string transactionId, List<string> objectsLockNeeded)
    {
        _ownedLeases.Add(new KeyValuePair<string, List<string>>(transactionId, objectsLockNeeded));
    }
    
    public void AskForPermissionToExecuteTransaction(string transactionId, List<string> objectsLockNeeded)
    {
        var canExecute = false;
        do
        {
            if(HasPermissionsToExecuteTransaction(objectsLockNeeded))
            {
                AddOwnLease(transactionId, objectsLockNeeded);
                // Timeout.Stop();
                canExecute = true;
            }
            else
            {
                if (!HasAlreadyRequestedLease(transactionId))
                {
                    RequestLease(transactionId, objectsLockNeeded);
                    WaitForPaxosResultSignal(transactionId);
                }
                if(HasPermissionsToExecuteTransaction(objectsLockNeeded))
                {
                    AddOwnLease(transactionId, objectsLockNeeded);
                    // Timeout.Stop();
                    canExecute = true;
                }
                else
                {
                    _leaseReleasedSignal.WaitOne();
                    _leaseReleasedSignal.Reset();
                }
                /*else
                {
                    // Timeout.Run();
                    WaitForMessagesOrTimeout();
                    if (ReceivedMessages())
                    {
                        ModifyWaitingQueue();
                    }
                    if (TimeoutExceeded())
                    {
                        ReleaseLease();
                    }
                }*/
            }
        } while (!canExecute);

        NotifyLeaseGranted();
        WaitForTransactionToFinishSignal();
        HandleLease(transactionId, objectsLockNeeded);
    }
    
    public void RemoveLease(List<string> objectsLockNeeded)
    {
        lock (this)
        {
            foreach (var objectKey in objectsLockNeeded)
            {
                _objectsQueue.TryGetValue(objectKey, out var queue);
                if (queue != null && queue.Count > 0)
                {
                    PopObjectQueue(objectKey);
                }
            }
        }
    }
    
    public void SetLeaseReleasedSignal()
    {
        _leaseReleasedSignal.Set();
    }

    public void HandleLease(string transactionId, List<string> objectsLockNeeded)
    {
        // if the queue of the objects only has the current TM, then don't need to remove. Otherwise, remove
        /*foreach (var objectKey in objectsLockNeeded)
        {
            _objectsQueue.TryGetValue(objectKey, out var queue);
            if (queue != null && queue.Count > 1 && queue.Peek() == _tmNick)
            {
                PopObjectQueue(objectKey);
            }
        }*/
        // Pop the objects from the queue if tmNick is the first one
        foreach (var objectKey in objectsLockNeeded)
        {
            _objectsQueue.TryGetValue(objectKey, out var queue);
            if (queue != null && queue.Count > 0 && queue.Peek() == _tmNick)
            {
                PopObjectQueue(objectKey);
            }
        }
        NotifyLeaseReleased(transactionId, objectsLockNeeded);
    }
    
    public void NotifyLeaseReleased(string transactionId, List<string> objectsLockNeeded)
    {
        _transactionManagerPropagateService.BroadcastLeaseReleased(transactionId, objectsLockNeeded);
    }
    
    private void WaitForTransactionToFinishSignal()
    {
        _transactionSignal.WaitOne();
        _transactionSignal.Reset();
    }
    
    public void TransactionFinished()
    {
        _transactionSignal.Set();
    }
    
    public ManualResetEvent WaitForLease()
    {
        lock (this)
        {
            return _leaseSignal;
        }
    }
    
    private void NotifyLeaseGranted()
    {
        lock (this)
        {
            _leaseSignal.Set();   
        }
    }
    
    public void ResetLeaseSignal()
    {
        lock (this)
        {
            _leaseSignal.Reset();
        }
    }

    public void ReceiveLeases(List<List<string>> leases)
    {
        foreach (var lease in leases)
        {
            var tmNick = lease[0];
            for (int i = 2; i < lease.Count; i++)
            {
                PushObjectQueue(lease[i], tmNick);
            }
        }
    }
    
    public void PrintObjectsQueue()
    {
        Console.WriteLine($"[LeaseHandler] Objects queue:");
        foreach (var objectKey in _objectsQueue.Keys)
        {
            Console.WriteLine($"[LeaseHandler] Object: {objectKey}");
            foreach (var tmNick in _objectsQueue[objectKey])
            {
                Console.WriteLine($"[LeaseHandler] TM: {tmNick}");
            }
        }
    }
    
}