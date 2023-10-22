using Grpc.Core;

namespace LeaseManager.Paxos;

public class Acceptor : PaxosService.PaxosServiceBase
{
    private readonly int _lmId;
    private readonly int _nServers;
    private int _leaderID = -1; 
    private int _IDp = -1;
    private int _IDa = -1;
    private List<List<string>> _value = new List<List<string>>();
    private readonly LeaseManagerService _leaseManagerService;
    
    public int LeaderID
    {
        get { return _leaderID;  }
    }
    
    public Acceptor(int lmId, int nServers, LeaseManagerService leaseManagerService)
    {
        _lmId = lmId;
        _leaseManagerService = leaseManagerService;
        _nServers = nServers;
    }
    
    public override Task<Promise> PaxosPhaseOne(Prepare prepare, ServerCallContext context)
    {
        return Task.FromResult(DoPhaseOne(prepare));
    }
   
    public override Task<Accepted> PaxosPhaseTwo(Accept accept, ServerCallContext context)
    {
        return Task.FromResult(DoPhaseTwo(accept));
    }

    public Promise DoPhaseOne(Prepare prepare)
    {
        // Ignore calls of suspects
        if ((prepare.IDp % _nServers) != _lmId && Suspects.IsSuspected(Suspects.GetNickname(prepare.IDp % _nServers)))
        {
            Console.WriteLine("(Acceptor): I am ignoring a Prepare call from ID: {0}", prepare.IDp);
            return new Promise() { IDp = -1 };
        }
        
        Promise promise = new Promise();
        
        // Did it promise to ignore requests with this ID?
        if (prepare.IDp < _IDp)
        {
            promise.IDp = -1; // Ignore request
        }
        else // Will promise to ignore request with lower Id
        {
            _IDp = prepare.IDp;
            promise.IDp = _IDp;
            _leaderID = _IDp; // Now it responds to a new leader
        }
        promise.IDa = _IDa;
        SetPromiseValue(promise);
        return promise;
    }
    
    public Accepted DoPhaseTwo(Accept accept)
    {
        if ((accept.IDp % _nServers) != _lmId && Suspects.IsSuspected(Suspects.GetNickname(accept.IDp % _nServers)))
        {
            Console.WriteLine("(Acceptor): I am ignoring an Accept call from ID: {0}", accept.IDp);
            return new Accepted() { IDp = -1 };
        }
        
        Accepted accepted = new Accepted();
        
        // Did it promise to ignore requests with this ID?
        if (accept.IDp < _IDp)
        {
            accepted.IDp = -1; // Ignore request
        }
        else // Reply with accept Id and value 
        {
            _IDp = accept.IDp;
            _IDa = _IDp;
            _value = UpdateValue(accept);
            accepted.IDp = _IDp;
            SetAcceptedValue(accepted);
            Console.WriteLine("(Acceptor): Value accepted: {0} from IDp: {1}", PrintLease(_value), _IDp);
            _leaseManagerService.SendLeases(_value);
        }
        
        return accepted;
    }
    
    private void SetPromiseValue(Promise promise)
    {
        foreach (var leaseAux in _value)
        {
            Lease lease = new Lease();
            lease.Value.AddRange(leaseAux);
            promise.Value.Add(lease);
        }
    }

    private static List<List<string>> UpdateValue(Accept accept)
    {
        List<List<string>> auxLease = new List<List<string>>();
        foreach (Lease lease in accept.Value)
        {
            List<string> list = new List<string>(lease.Value);
            auxLease.Add(list);
        }

        return auxLease;
    }

    private void SetAcceptedValue(Accepted accepted)
    {
        foreach (var leaseAux in _value)
        {
            Lease lease = new Lease();
            lease.Value.AddRange(leaseAux);
            accepted.Value.Add(lease);
        }
    }
    
    public void PrepareForNextEpoch()
    {
        _IDp = -1;
        _IDa = -1;
        _value.Clear();
    }
    
    private static string PrintLease(List<List<string>> value)
    {
        string result = "";
        foreach (var lease in value)
        {
            string leaseAux = "";
            foreach (var str in lease)
            {
                leaseAux = leaseAux + " " + str;
            }
            result = result + leaseAux + " | ";
        }
    
        return result;
    }
}