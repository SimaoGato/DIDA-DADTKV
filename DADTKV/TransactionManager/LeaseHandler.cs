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
    private TransactionManagerPropagateService _transactionManagerPropagateService;
    private ManualResetEvent _leaseReleasedSignal = new ManualResetEvent(false);
    
    private bool _SignalGiven { get; set; }
    public bool _TimeoutExcedded { get; set; }
    public bool _LeaseReleased { get; set; }
        
    public LeaseHandler(string tmNick, TransactionManagerLeaseService transactionManagerLeaseService, 
        ManualResetEvent leaseReceivedSignal, TransactionManagerPropagateService transactionManagerPropagateService)
    {
        _tmNick = tmNick;
        _transactionManagerLeaseService = transactionManagerLeaseService;
        _leaseReceivedSignal = leaseReceivedSignal;
        _transactionManagerPropagateService = transactionManagerPropagateService;
    }

    private void PushObjectQueue(string objectKey, string tmNick)
    {
        _objectsQueue.TryGetValue(objectKey, out var queue);
        if (queue == null)
        {
            queue = new Queue<string>();
            _objectsQueue[objectKey] = queue;
        }
        queue.Enqueue(tmNick);
    }
    
    private void PopObjectQueue(string objectKey)
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
    
    public bool HasToRequestLease(List<string> objectsLockNeeded)
    {
        foreach (var objectKey in objectsLockNeeded)
        {
            _objectsQueue.TryGetValue(objectKey, out var queue);
            
            if (queue == null || queue.Count == 0)
            {
                return true;
            }

            if (queue != null && !queue.Contains(_tmNick))
            {
                return true;
            }
        }

        return false;
    }
    
    public void AskForPermissionToExecuteTransaction(string transactionId, List<string> objectsLockNeeded)
    {
        var canExecute = false;
        _SignalGiven = false;
        _TimeoutExcedded = false;
        _LeaseReleased = false;
        do
        {
            if(HasPermissionsToExecuteTransaction(objectsLockNeeded))
            {
                canExecute = true;
            }
            else
            {
                if (HasToRequestLease(objectsLockNeeded))
                {
                    _transactionManagerPropagateService.RemoveTransactionFromRequestedLeases(transactionId);
                    RequestLease(transactionId, objectsLockNeeded);
                    WaitForPaxosResultSignal(transactionId);
                }
                if(HasPermissionsToExecuteTransaction(objectsLockNeeded))
                {
                    canExecute = true;
                }
                else
                {
                    _SignalGiven = false;

                    Task.Run(() => LeaseTimer());

                    while (!_TimeoutExcedded && !_LeaseReleased)
                    {
                        _leaseReleasedSignal.WaitOne();
                        _leaseReleasedSignal.Reset();
                    }
                    
                    _SignalGiven = true;

                    if (_TimeoutExcedded)
                    {
                        _TimeoutExcedded = false;
                        ForceToReleaseLease(objectsLockNeeded);
                    }

                    if (_LeaseReleased)
                    {
                        _LeaseReleased = false;
                    }
                    
                }
                
            }
        } while (!canExecute);

        NotifyLeaseGranted();
        WaitForTransactionToFinishSignal();
        HandleLease(transactionId, objectsLockNeeded);
    }
    
    // async timer
    private async Task LeaseTimer()
    {
        Thread.Sleep(10000);
        if (!_SignalGiven)
        {
            await Task.Delay(10000);
            _TimeoutExcedded = true;
            _leaseReleasedSignal.Set();
        }
    }

    public void ForceToReleaseLease(List<string> objectsLockNeeded)
    {
        Dictionary<string, List<string>> objectsToRelease = new Dictionary<string, List<string>>();
        foreach(var objectKey in objectsLockNeeded)
        {
            _objectsQueue.TryGetValue(objectKey, out var queue);
            if (queue != null && queue.Count > 0 && queue.Peek() != _tmNick)
            {
                var tmNick = queue.Peek();
                if (!objectsToRelease.ContainsKey(tmNick))
                {
                    objectsToRelease[tmNick] = new List<string>();
                }
                objectsToRelease[tmNick].Add(objectKey);
            }
        }
        // for each tmNick, send a release lease request
        foreach (var tmNick in objectsToRelease.Keys)
        {
            _transactionManagerPropagateService.ReleaseLease(tmNick, objectsToRelease[tmNick]);
        }
    }
    
    public void ReleaseLease(List<string> objectsLockNeeded)
    {
        var objectsReleased = new List<string>();
        lock (this)
        {
            foreach (var objectKey in objectsLockNeeded)
            {
                _objectsQueue.TryGetValue(objectKey, out var queue);
                if (queue != null && queue.Count > 0 && queue.Peek() == _tmNick)
                {
                    PopObjectQueue(objectKey);
                    objectsReleased.Add(objectKey);
                }
            }
        }
        string transactionId = Guid.NewGuid().ToString();
        if(objectsReleased.Count > 0) NotifyLeaseReleased(transactionId, objectsReleased);
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

    private void HandleLease(string transactionId, List<string> objectsLockNeeded)
    {
        var objectsReleased = new List<string>();
        foreach (var objectKey in objectsLockNeeded)
        {
            _objectsQueue.TryGetValue(objectKey, out var queue);
            if (queue != null && queue.Count > 1 && queue.Peek() == _tmNick)
            {
                PopObjectQueue(objectKey);
                objectsReleased.Add(objectKey);
            }
        }
        if(objectsReleased.Count > 0) NotifyLeaseReleased(transactionId, objectsReleased);
    }
    
    private void NotifyLeaseReleased(string transactionId, List<string> objectsLockNeeded)
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
        lock (_objectsQueue)
        {
            Console.WriteLine($"[LeaseHandler] Objects queue: {_objectsQueue.Count}");
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
    
}