using Grpc.Net.Client;

namespace LeaseManager.Paxos;

public class Proposer
{
    private Dictionary<string, PaxosService.PaxosServiceClient> _stubs =
        new Dictionary<string, PaxosService.PaxosServiceClient>();
    private bool _isLeader; // TODO: Implement leader logic
    private int _id; // TODO: Add logic to change the id
    private int _prevAcceptedId = -1;
    private int _value; // TODO: Change to buffer for the leaseRequests
                        // Keep in mind that this value might be updated ny an accept response
                        // TODO: Maybe add new property of accepted value (?)
    private int _nServers;
  
    public Proposer(string nick, int nServers, Dictionary<string, Uri> addresses)
    {
        _nServers = nServers;
        foreach (var addr in addresses)
        {
            if (addr.Key != nick)
            {
                var channel = GrpcChannel.ForAddress(addr.Value.ToString());
                var stub = new PaxosService.PaxosServiceClient(channel);
                _stubs[addr.Key] = stub;
            }
        }
    }

    public void PhaseOne()
    {
        // Want to propose a value, send prepare ID
        var prepare = new Prepare
        {
            Id = _id
        };
        
        int count = 0;
        
        // Broadcast attempt
        // TODO: Maybe change to multi threading (?)
        foreach (var stub in _stubs)
        {
            var promise = stub.Value.PaxosPhaseOne(prepare);
            
            // Received a promise (with its ID)?
            if (promise.Id == _id) // Yes, update count
            {
                count++;
                // Did it receive Promise IDp accepted IDa, value?
                // It needs to update the _value with the Highest IDa (PreviousAcceptedID) that it got
                if (promise.PreviousAcceptedId != -1 && promise.PreviousAcceptedId > _prevAcceptedId) 
                {
                    _value = promise.AcceptedValue; // Yes, update value
                }
            }
        }
        
        // Did it receive promises from a majority?
        if (count > _nServers / 2)
        {
            PhaseTwo(); // Yes, go to phase 2
        }
        else
        {
            // Retry again with higher ID ?
            // TODO: Maybe implement Timeout above this code (?)
        }
    }

    private void PhaseTwo()
    {
        var accept = new Accept
        {
            Id = _id,
            Value = _value
        };
        // Broadcast attempt to accept
        // TODO: Maybe change to multi threading (?)
        foreach (var stub in _stubs)
        {
            var accepted = stub.Value.PaxosPhaseTwo(accept);
            
            // Confirmation of acceptance ?
            if (accepted.Id == _id) // Yes
            {
                // do something
            }
        }
    }
}