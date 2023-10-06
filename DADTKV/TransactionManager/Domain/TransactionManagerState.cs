using System;
using System.Collections.Generic;

namespace TransactionManager
{
    public class TransactionManagerState
    {
        private readonly Dictionary<string, long> _dataStorage = new Dictionary<string, long>();
        private List<List<List<string>>> _leasesPerLeaseManager = new List<List<List<string>>>();

        public void WriteOperation(string key, long value)
        {
            _dataStorage[key] = value;
        }

        public long ReadOperation(string key)
        {
            if (_dataStorage.TryGetValue(key, out var value))
            {
                return value;
            }
            throw new KeyNotFoundException($"Key '{key}' not found in data storage.");
        }


        public bool ContainsKey(string key)
        {
            return _dataStorage.ContainsKey(key);
        }

        public void ReceiveLeases(List<List<string>> leases)
        {
            _leasesPerLeaseManager.Add(leases);
            // TODO: Implement lease order consensus check
        }
    }
}