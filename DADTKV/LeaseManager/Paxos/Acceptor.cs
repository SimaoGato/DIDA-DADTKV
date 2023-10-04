using Grpc.Core;

namespace LeaseManager.Paxos;

public class Acceptor : PaxosService.PaxosServiceBase
{
    public int _IDp = -1;
    private int _IDa = -1;
    public int _value;
    public override Task<Promise> PaxosPhaseOne(Prepare prepare, ServerCallContext context)
    {
        return Task.FromResult(DoPhaseOne(prepare));
    }
   
    public override Task<Accepted> PaxosPhaseTwo(Accept accept, ServerCallContext context)
    {
        return Task.FromResult(DoPhaseTwo(accept));
    }

    private Promise DoPhaseOne(Prepare prepare)
    {
        Console.WriteLine("Paxos prepare received");
        Promise promise = new Promise();
        
        // Did it promise to ignore requests with this ID?
        if (prepare.IDp < _IDp)
        {
            promise.IDp = -1; // Ignore request
            Console.WriteLine("IGNORE, Prev Accepted ID: {0}", _IDa);
        }
        else // Will promise to ignore request with lower Id
        {
            _IDp = prepare.IDp;
            // Has it ever accepted anything?
            if (_IDa == -1) // No
            {
                Console.WriteLine("Didnt accepted anything");
            }
            else // Yes
            {
                Console.WriteLine("Acceptor has already accepted something");
            }
            // TODO: _IDa Should change to -1 every new epoch
            promise.IDp = _IDp;
        }
        promise.IDa = _IDa;
        promise.Value = _value;
        Console.WriteLine("Paxos promise");
        return promise;
    }
    
    private Accepted DoPhaseTwo(Accept accept)
    {
        Console.WriteLine("Paxos accept received");
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
            _value = accept.Value;
            accepted.IDp = _IDp;
            accepted.Value = _value;
        }
        
        Console.WriteLine("Value accepted: {0}", _value);
        return accepted;
    }
}