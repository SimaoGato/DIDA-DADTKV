using Grpc.Net.Client;

namespace LeaseManager.Paxos;

public class Proposer
{
    private List<PaxosService.PaxosServiceClient> _stubs =
        new List<PaxosService.PaxosServiceClient>();
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

    public void StartPaxos()
    {
        var leaderId = (_acceptor.LeaderID % _nServers);
        
        Console.WriteLine("LeaderID: {0} Acceptor-LeaderID: {1}", leaderId, _acceptor.LeaderID);
        
        if (_acceptor.LeaderID == -1)
        {
            Console.WriteLine("START WITH PHASE ONE 1");
            PhaseOne();
        }
        else if (leaderId == (_IDp % _nServers))
        {
            Console.WriteLine("START WITH PHASE ONE 2");
            PhaseTwo();
        }
    }

    public async void PhaseOne()
    {
        Console.WriteLine("PHASE ONE 1 TIMEOUT: {0}", timeout);
        // Want to propose a value, send prepare ID
        var prepare = new Prepare
        {
            IDp = _IDp
        };
        
        List<Task<Promise>> sendTasks = new List<Task<Promise>>();
        sendTasks.Add(new Task<Promise>(() => _acceptor.DoPhaseOne(prepare)));
        foreach (var stub in _stubs)
        {
            sendTasks.Add(new Task<Promise>(() => stub.PaxosPhaseOne(prepare)));
        }

        foreach (var task in sendTasks)
        {
            Console.WriteLine("(Proposer):Paxos prepare with IDp: {0}", _IDp);
            task.Start(); 
        }
        
        await Task.WhenAll(sendTasks);

        int count = 0; // Count itself (the acceptor)
        foreach (var resultTask in sendTasks)
        {
            Promise promise = await resultTask;
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

    private async void PhaseTwo()
    {
        Console.WriteLine("PHASE TWO 2 TIMEOUT: {0}", timeout);
        Console.WriteLine("(Proposer):PhaseTwo Value: {0}", _value);
        var accept = new Accept
        {
            IDp = _IDp,
            Value = _value
        };
        
        List<Task<Accepted>> sendTasks = new List<Task<Accepted>>();
        sendTasks.Add(new Task<Accepted>(() => _acceptor.DoPhaseTwo(accept))); 
        foreach (var stub in _stubs)
        {
            sendTasks.Add(new Task<Accepted>(() => stub.PaxosPhaseTwo(accept)));
        }
                                                          
        foreach (var task in sendTasks)
        {
            Console.WriteLine("(Proposer):Value to be accepted: {0}", _value);
            task.Start(); 
        }
                                                                  
        await Task.WhenAll(sendTasks);

        int count = 0; // Count itself (the acceptor)
        foreach (var resultTask in sendTasks)
        {
            Accepted accepted = await resultTask;
            
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