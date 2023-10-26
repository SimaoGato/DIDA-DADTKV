namespace LeaseManager.Domain;

public class LeaseManagerState
{
    private readonly List<List<string>> _requestedLeases = new List<List<string>>();

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
                        break;
                    }
                }
            }
        }
    }
    
    public void PrintBuffer()
    {
        string result = "";
        foreach (var lease in _requestedLeases)
        {
            result = result + lease[1].Substring(0,9) + " | ";
        }
        
        if (result == "")
        {
            result += "{ Empty }";
        }
        else
        {
            result = result.Remove(result.Length - 3);
        }
        
        Console.WriteLine("(LM Leases to decide): " + result);
    }
    
}