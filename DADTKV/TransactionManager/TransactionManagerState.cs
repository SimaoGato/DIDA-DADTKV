using System.Runtime.InteropServices.ComTypes;
using System.Collections.Generic;
using System.Threading;

namespace TransactionManager;

public class TransactionManagerState
{
    private Dictionary<string, long> _dadIntsStorage = new Dictionary<string, long>();
    private int _numberOfResponsesFromLeaseManagers = 0;
    List<List<List<string>>> leasesPerLeaseManager;

    public TransactionManagerState()
    {
        leasesPerLeaseManager = new List<List<List<string>>>();
    }
    
    public int NumberOfResponsesFromLeaseManagers
    {
        get { return _numberOfResponsesFromLeaseManagers; }
    }
    
    public void WriteOperation(string key, long value)
    {
        _dadIntsStorage.Add(key, value);
    }
    
    public long ReadOperation(string key)
    {
        return _dadIntsStorage[key];
    }
    
    public bool ContainsKey(string key)
    {
        return _dadIntsStorage.ContainsKey(key);
    }
    
    public void ReceiveLeasesOrder(List<List<string>> leases)
    {
        leasesPerLeaseManager.Add(leases);
        _numberOfResponsesFromLeaseManagers++;
        //TODO check lease order consensus
    }
}