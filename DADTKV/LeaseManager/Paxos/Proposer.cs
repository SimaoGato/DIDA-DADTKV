using Grpc.Net.Client;

namespace LeaseManager.Paxos;

public class Proposer
{
    private List<PaxosService.PaxosServiceClient> _stubs =
        new List<PaxosService.PaxosServiceClient>();
    private bool _isLeader; // TODO: Implement leader logic
    public int _IDp; // TODO: Add logic to change the id
    private int _IDa = -1;
    public int _value; // TODO: Change to buffer for the leaseRequests
    // Keep in mind that this value might be updated ny an accept response
    // TODO: Maybe add new property of accepted value (?)
    private int _nServers;
  
    public Proposer(string myUrl, int nServers,  List<string> addresses)
    {
        _nServers = nServers;
        foreach (var addr in addresses)
        {
            if (addr != myUrl)
            {
                var channel = GrpcChannel.ForAddress(addr);
                var stub = new PaxosService.PaxosServiceClient(channel);
                _stubs.Add(stub);
            }
        }
    }

    public void PhaseOne()
    {
        // Want to propose a value, send prepare ID
        var prepare = new Prepare
        {
            IDp = _IDp
        };
        
        int count = 0;
        
        // Broadcast attempt
        foreach (var stub in _stubs)
        {
            Console.WriteLine("Paxos prepare");
            var promise = stub.PaxosPhaseOne(prepare);
            Console.WriteLine("Paxos promise received");
            Console.WriteLine("Value: {0}", _value);
            
            // Received a promise (with its ID)?
            if (promise.IDp == _IDp) // Yes, update count
            {
                count++;
                // Did it receive Promise _IDp accepted IDa, value?
                // It needs to update the _value with the Highest IDa (PreviousAcceptedID) that it got
                Console.WriteLine("Value: {0}", _value);
                if (promise.IDa != -1 && (promise.IDa > _IDa)) 
                {
                    Console.WriteLine("Change Value: {0}", _value);
                    _value = promise.Value; // Yes, update value
                    Console.WriteLine("Value has changed: {0}", _value);
                }
            }
        }
        
        Console.WriteLine("Count: {0}", count);
        Console.WriteLine("nServers: {0}", _nServers);
        Console.WriteLine("Value: {0}", _value);
        
        // Did it receive promises from a majority?
        if (count > _nServers / 2)
        {
            
            Console.WriteLine("Paxos phase 2");
            Console.WriteLine("Value: {0}", _value);
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
        Console.WriteLine("PhaseTwo Value: {0}", _value);
        var accept = new Accept
        {
            IDp = _IDp,
            Value = _value
        };
        
        Console.WriteLine("Value to be accepted: {0}", _value);
        // Broadcast attempt to accept
        // TODO: Maybe change to multi threading (?)
        foreach (var stub in _stubs)
        {
            var accepted = stub.PaxosPhaseTwo(accept);
            
            // Confirmation of acceptance ?
            if (accepted.IDp == _IDp) // Yes
            {
                // do something
            }
        }
    }
}