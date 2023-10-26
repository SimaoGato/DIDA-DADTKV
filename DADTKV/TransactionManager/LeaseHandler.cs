namespace TransactionManager;

public class LeaseHandler
{
    
    private readonly ManualResetEvent _leaseSignal = new ManualResetEvent(false);
    
    public LeaseHandler()
    {
        
    }
    
    public void AskForLease(List<string> objectsLockNeeded)
    {
        Thread.Sleep(1500);
        NotifyLeaseGranted();
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
    
}