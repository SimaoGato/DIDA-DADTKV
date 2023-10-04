using Grpc.Core;

namespace LeaseManager.Paxos;

public class Acceptor : PaxosService.PaxosServiceBase
{
    public int _IDp = -1;
    public int _IDa = -1;
    public int _value = -1;
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
                Console.WriteLine("(Acceptor):Acceptor has already accepted something IDa: {0} AValue: {1}", _IDa, _value);
            }
            // TODO: _IDa Should change to -1 every new epoch
            promise.IDp = _IDp;
        }
        promise.IDa = _IDa;
        promise.Value = _value;
        Console.WriteLine("(Acceptor):Paxos promise {0} to IDp:{1}", promise.IDp, prepare.IDp);
        return promise;
    }
    
    private Accepted DoPhaseTwo(Accept accept)
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
            _value = accept.Value;
            accepted.IDp = _IDp;
            accepted.Value = _value;
            Console.WriteLine("(Acceptor):Value accepted: {0} from IDp: {1}", _value, _IDp);
        }
        
        return accepted;
    }
}