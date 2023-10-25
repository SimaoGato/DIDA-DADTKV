﻿using System;
using System.Collections.Generic;

namespace TransactionManager
{
    public class TransactionManagerState
    {
        private readonly Dictionary<string, long> _dataStorage = new Dictionary<string, long>();
        private List<List<List<string>>> _leasesPerLeaseManager = new List<List<List<string>>>();

        public void WriteOperation(string key, long value)
        {
            lock (_dataStorage)
            {
                try
                {
                    // Console transaction that is being made
                    //Console.WriteLine($"[TransactionManagerState] Writing object: {key} with value {value}");
                    _dataStorage[key] = value;
                }
                catch (Exception ex)
                {
                    throw new Exception($"Error writing data for key '{key}': {ex.Message}");
                }
            }

        }

        public long ReadOperation(string key)
        {
            lock (_dataStorage)
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
        }
        
        public void PrintObjects()
        {
            lock (_dataStorage)
            {
                try
                {
                    //Console.WriteLine("[TransactionManagerState] Objects in data storage:");
                    foreach (var key in _dataStorage.Keys)
                    {
                        //Console.WriteLine($"[TransactionManagerState] Key: {key}, Value: {_dataStorage[key]}");
                    }
                }
                catch (Exception ex)
                {
                    throw new Exception($"Error printing objects: {ex.Message}");
                }    
            }
        }

        public bool ContainsKey(string key)
        {
            lock (_dataStorage)
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
        
        // method to return leasesPerLeaseManager
        public List<List<List<string>>> GetLeasesPerLeaseManager()
        {
            return _leasesPerLeaseManager;
        }
        
        // method to clear leasesPerLeaseManager
        public void ClearLeasesPerLeaseManager()
        {
            _leasesPerLeaseManager.Clear();
            //Console.WriteLine($"[TransactionManagerState] cleared leasesPerLeaseManager");
        }
    }
}
