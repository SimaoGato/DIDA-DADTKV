using System.ComponentModel;

namespace LeaseManager;

public static class Suspects
{
    private static Dictionary<string, bool> _suspectsNicks = new Dictionary<string, bool>();
    private static Dictionary<int, string> _idsToNicksMap = new Dictionary<int, string>();

    public static void SetMaps(Dictionary<int, string> map)
    {
        foreach (var pair in map)
        {
            _idsToNicksMap.Add(pair.Key, pair.Value);
            _suspectsNicks.Add(pair.Value, false);
        }
    }
    
    public static string GetNickname(int id)
    {
        return _idsToNicksMap[id];
    }
    
    public static void SetSuspected(string nickname)
    {
        if (_suspectsNicks.ContainsKey(nickname))
        {
            _suspectsNicks[nickname] = true;
        }
    }
    
    public static bool IsSuspected(string nickname)
    {
        return _suspectsNicks[nickname];
    }
    
    public static void ResetSuspects()
    {
        foreach (var pair in _suspectsNicks)
        {
            _suspectsNicks[pair.Key] = false;
        }
    }
}