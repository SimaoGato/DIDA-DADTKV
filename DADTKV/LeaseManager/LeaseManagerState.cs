namespace LeaseManager;

public class LeaseManagerState
{
    private List<List<string>> _requestedLeases = new List<List<string>>();

    public List<List<string>> RequestedLeases
    {
        get { return _requestedLeases; }
    }
    
    public void AddLease(List<string> lease)
    {
        _requestedLeases.Add(lease);
    }
    
    public void CleanRequestedLeases()
    {
        _requestedLeases.Clear();
    }
}