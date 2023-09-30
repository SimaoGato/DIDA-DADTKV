using Grpc.Core;

namespace LeaseManager.Paxos;

public class Acceptor : PaxosService.PaxosServiceBase
{
    private int _id = -1;
    private int _prevAcceptedId = -1;
    private int _value;
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
        Promise promise = new Promise();
        // Did it promise to ignore requests with this ID?
        if (prepare.Id < _id)
        {
            promise.Id = -1; // Ignore request
        }
        else // Will promise to ignore request with lower Id
        {
            _id = prepare.Id;
            // Has it ever accepted anything?
            if (_prevAcceptedId == -1) // No
            {
                promise.Id = _id;
            }
            else // Yes
            {
                promise.Id = _id;
                promise.PreviousAcceptedId = _prevAcceptedId;
                promise.AcceptedValue = _value;
            }
            // TODO: _prevAcceptedId Should change to -1 every new epoch
        }
        return promise;
    }
    
    private Accepted DoPhaseTwo(Accept accept)
    {
        Accepted accepted = new Accepted();
        // Did it promise to ignore requests with this ID?
        if (accept.Id < _id)
        {
            accepted.Id = -1; // Ignore request
        }
        else // Reply with accept Id and value 
        {
            _id = accept.Id;
            _prevAcceptedId = _id;
            _value = accept.Value;
            accepted.Id = _id;
            accepted.AcceptedValue = _value;
        }
        return accepted;
    }
}