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
            try
            {
                _dataStorage[key] = value;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error writing data for key '{key}': {ex.Message}");
            }
        }

        public long ReadOperation(string key)
        {
            try
            {
                if (_dataStorage.TryGetValue(key, out var value))
                {
                    return value;
                }
                throw new KeyNotFoundException($"Key '{key}' not found in data storage.");
            }
            catch (KeyNotFoundException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error reading data for key '{key}': {ex.Message}");
            }
        }

        public bool ContainsKey(string key)
        {
            try
            {
                return _dataStorage.ContainsKey(key);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error checking if key '{key}' exists: {ex.Message}");
            }
        }

        public void ReceiveLeases(List<List<string>> leases)
        {
            try
            {
                _leasesPerLeaseManager.Add(leases);
                // TODO: Implement lease order consensus check
            }
            catch (Exception ex)
            {
                throw new Exception($"Error receiving leases: {ex.Message}");
            }
        }
    }
}
