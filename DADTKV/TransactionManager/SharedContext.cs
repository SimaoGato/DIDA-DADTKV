public class SharedContext
{
    private ManualResetEvent _leaseSignal = new ManualResetEvent(false);

    public ManualResetEvent LeaseSignal => _leaseSignal;

    public void SetLeaseSignal()
    {
        _leaseSignal.Set();
    }
}