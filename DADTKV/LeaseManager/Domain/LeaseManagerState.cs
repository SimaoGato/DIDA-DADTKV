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
        lock (this)
        {
            _requestedLeases.Add(lease);
        }
    }
    
    public void RemoveLeases(List<List<string>> leases)
    {
        lock (this)
        {
            foreach (var lease in leases)
            {
                foreach (var requestedLease in _requestedLeases)
                {
                    if (lease[1] == requestedLease[1]) // Check for the id of the lease
                    {
                        _requestedLeases.Remove(requestedLease);
                        Console.WriteLine("(LM State): Removed lease: " + lease[1].Substring(0,9));
                        break;
                    }
                }
            }

            string result = "";
            foreach (var lease in _requestedLeases)
            {
                result = result + lease[1].Substring(0,9) + " | ";
            }
            Console.WriteLine("(LM State After Remove): " + result);
            Console.WriteLine();
        }
    }
    
    public void PrintBuffer()
    {
        string result = "";
        foreach (var lease in _requestedLeases)
        {
            result = result + lease[1].Substring(0,9) + " | ";
        }
        Console.WriteLine("(LM State Content): " + result);
    }
    
    public void CleanRequestedLeases()
    {
        _requestedLeases.Clear();
    }
}