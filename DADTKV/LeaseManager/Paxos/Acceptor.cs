using Grpc.Core;

namespace LeaseManager.Paxos;

public class Acceptor : PaxosService.PaxosServiceBase
{
    private int _leaderID = -1; 
    private int _IDp = -1;
    private int _IDa = -1;
    private List<List<string>> _value = new List<List<string>>();
    private LeaseManagerService _leaseManagerService;
    
    public Acceptor(LeaseManagerService leaseManagerService)
    {
        _leaseManagerService = leaseManagerService;
    }

    public int LeaderID
    {
        get { return _leaderID;  }
        set { _leaderID = value;}
    }
    
    public List<List<string>> Value
    {
        get { return _value;  }
        set { _value = value;}
    }
    
    public void PrepareForNextEpoch()
    {
        _IDp = -1;
        _IDa = -1;
        _value.Clear();
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
        Console.WriteLine("(Acceptor):Paxos prepare received with IDp: {0}", prepare.IDp);
        Promise promise = new Promise();
        
        // Did it promise to ignore requests with this ID?
        if (prepare.IDp < _IDp)
        {
            promise.IDp = -1; // Ignore request
            Console.WriteLine("(Acceptor):IGNORE, Prev Accepted ID: {0}", _IDp);
        }
        else // Will promise to ignore request with lower Id
        {
            _IDp = prepare.IDp;
            // Has it ever accepted anything?
            if (_IDa == -1) // No
            {
                Console.WriteLine("(Acceptor):Didn't accepted anything");
            }
            else // Yes
            {
                Console.WriteLine("(Acceptor):Acceptor has already accepted something IDa: {0} Value: {1}", _IDa, PrintLease(_value));
            }
            // TODO: _IDa Should change to -1 every new epoch
            promise.IDp = _IDp;
        }
        promise.IDa = _IDa;
        foreach (var leaseAux in _value)
        {
            Lease lease = new Lease();
            lease.Value.AddRange(leaseAux);
            promise.Value.Add(lease);
        }
        
        Console.WriteLine("(Acceptor):Paxos promise {0} to IDp:{1}", promise.IDp, prepare.IDp);
        return promise;
    }
    
    public Accepted DoPhaseTwo(Accept accept)
    {
        Console.WriteLine("(Acceptor):Paxos accept received with IDp: {0}", accept.IDp);
        Accepted accepted = new Accepted();
        // Did it promise to ignore requests with this ID?
        if (accept.IDp < _IDp)
        {
            accepted.IDp = -1; // Ignore request
            Console.WriteLine("Ignore request of IDp: {0}", accept.IDp);
        }
        else // Reply with accept Id and value 
        {
            _IDp = accept.IDp;
            _IDa = _IDp;
            
            List<List<string>> auxLease = new List<List<string>>();
            foreach (Lease lease in accept.Value)
            {
                List<string> list = new List<string>(lease.Value);
                auxLease.Add(list);
            }
            _value = auxLease;
            
            accepted.IDp = _IDp;
            foreach (var leaseAux in _value)
            {
                Lease lease = new Lease();
                lease.Value.AddRange(leaseAux);
                accepted.Value.Add(lease);
            }
            
            _leaderID = _IDp;
            Console.WriteLine("(Acceptor):Value accepted: {0} from IDp: {1}", PrintLease(_value), _IDp);
            _leaseManagerService.SendLeases(_value);
        }
        
        return accepted;
    }
    
    private string PrintLease(List<List<string>> value)
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