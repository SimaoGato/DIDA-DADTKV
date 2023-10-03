using System.Runtime.InteropServices.ComTypes;

namespace TransactionManager;

public class TransactionManagerState
{
    private Dictionary<string, long> _dadIntsStorage = new Dictionary<string, long>();

    public TransactionManagerState()
    {
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
}