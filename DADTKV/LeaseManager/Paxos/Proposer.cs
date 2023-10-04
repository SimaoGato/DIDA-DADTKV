using Grpc.Net.Client;

namespace LeaseManager.Paxos;

public class Proposer
{
    private List<PaxosService.PaxosServiceClient> _stubs =
        new List<PaxosService.PaxosServiceClient>();
    private bool _isLeader; // TODO: Implement leader logic
    public int _IDp; 
    private int _IDa = -1;
    public int _value; // TODO: Change to buffer for the leaseRequests
    // Keep in mind that this value might be updated ny an accept response
    // TODO: Maybe add new property of accepted value (?)
    private int _nServers;
    private Acceptor _acceptor;
    private int timeout = 1;
  
    public Proposer(int iDp, int nServers,  List<string> addresses, Acceptor acceptor)
    {
        _IDp = iDp;
        _nServers = nServers;
        foreach (var addr in addresses)
        {
            var channel = GrpcChannel.ForAddress(addr);
            var stub = new PaxosService.PaxosServiceClient(channel);
            _stubs.Add(stub);
        }

        _acceptor = acceptor;
    }

    public void PhaseOne()
    {
        // Want to propose a value, send prepare ID
        var prepare = new Prepare
        {
            IDp = _IDp
        };
        
        int count = 0; // Count itself (the acceptor)
        //-------------------------------------------------- 
        var promise = _acceptor.DoPhaseOne(prepare);
        
        // Received a promise (with its ID)?
        if (promise.IDp == _IDp) // Yes, update count
        {
            count++;
            // Did it receive Promise _IDp accepted IDa, value?
            // It needs to update the _value with the Highest IDa (PreviousAcceptedID) that it got
            if (promise.IDa != -1 && (promise.IDa > _IDa)) 
            {
                _value = promise.Value; // Yes, update value
                Console.WriteLine("(Proposer):Value has changed: {0}", _value);
            }
        }
        //--------------------------------------------------
        // Broadcast attempt
        foreach (var stub in _stubs)
        {
            Console.WriteLine("(Proposer):Paxos prepare with IDp: {0}", _IDp);
            promise = stub.PaxosPhaseOne(prepare);
            Console.WriteLine("(Proposer):Promise received with IDp: {0}", promise.IDp);
            
            // Received a promise (with its ID)?
            if (promise.IDp == _IDp) // Yes, update count
            {
                count++;
                // Did it receive Promise _IDp accepted IDa, value?
                // It needs to update the _value with the Highest IDa (PreviousAcceptedID) that it got
                if (promise.IDa != -1 && (promise.IDa > _IDa)) 
                {
                    _value = promise.Value; // Yes, update value
                    Console.WriteLine("(Proposer):Value has changed: {0}", _value);
                }
            }
        }
        Console.WriteLine("(Proposer):Count: {0} | nServers: {1} | Value: {2}", count, _nServers, _value);
        
        // Did it receive promises from a majority?
        if (count > _nServers / 2)
        {
            timeout = 1; 
            Console.WriteLine("(Proposer):Paxos phase 2 with value: {0}", _value);
            PhaseTwo(); // Yes, go to phase 2
        }
        else
        {
            // Timeout to avoid live lock
            _IDp = _IDp + _nServers;
            Console.WriteLine("(Proposer):Wait Timeout: {0}", timeout);
            Thread.Sleep(timeout * 1000); 
            timeout = timeout * 2;
            
            // Retry again with higher ID 
            Console.WriteLine("(Proposer):Retrying paxos with new ID: {0}", _IDp);
            PhaseOne();
        }
    }

    private void PhaseTwo()
    {
        Console.WriteLine("(Proposer):PhaseTwo Value: {0}", _value);
        var accept = new Accept
        {
            IDp = _IDp,
            Value = _value
        };
        
        Console.WriteLine("(Proposer):Value to be accepted: {0}", _value);
        // Broadcast attempt to accept
        // TODO: Maybe change to multi threading (?)
        int count = 0;
        foreach (var stub in _stubs)
        {
            var accepted = stub.PaxosPhaseTwo(accept);
            
            // Confirmation of acceptance ?
            if (accepted.IDp == _IDp) // Yes
            {
                // do something
                count++;
            }
        }
        
        if (count > _nServers / 2)
        {
            Console.WriteLine("(Proposer):FINISH PAXOS WITH VALUE: {0} (my id: {1})", _value, _IDp); 
        } 
    }
}