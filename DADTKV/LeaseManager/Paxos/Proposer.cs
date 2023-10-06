using Grpc.Net.Client;

namespace LeaseManager.Paxos;

public class Proposer
{
    private List<GrpcChannel> _channels = new List<GrpcChannel>();
    private List<PaxosService.PaxosServiceClient> _stubs =
        new List<PaxosService.PaxosServiceClient>();
    private int _IDp; 
    private int _IDa = -1;
    private List<List<string>> _value = new List<List<string>>();
    private int _nServers;
    private Acceptor _acceptor;
    private int _timeout = 1;

    public List<List<string>> Value
    {
        get { return _value; }
        set { _value = value; }
    }
    
    public void PrepareForNextEpoch()
    {
        _IDa = -1;
        _value.Clear();
    }
  
    public Proposer(int iDp, int nServers,  List<string> addresses, Acceptor acceptor)
    {
        _IDp = iDp;
        _nServers = nServers;
        foreach (var addr in addresses)
        {
            var channel = GrpcChannel.ForAddress(addr);
            var stub = new PaxosService.PaxosServiceClient(channel);
            _channels.Add(channel);
            _stubs.Add(stub);
        }

        _acceptor = acceptor;
    }

    public void StartPaxos()
    {
        var leaderId = (_acceptor.LeaderID % _nServers);
        
        //Console.WriteLine("LeaderID: {0} Acceptor-LeaderID: {1}", leaderId, _acceptor.LeaderID);
        
        // If there is no leader, start with phase 1
        if (_acceptor.LeaderID == -1)
        {
            //Console.WriteLine("START WITH PHASE ONE 1");
            PhaseOne();
        }
        else if (leaderId == (_IDp % _nServers)) // Multi-Paxos
        {
            //Console.WriteLine("START WITH PHASE ONE 2");
            PhaseTwo();
        }
    }

    public async void PhaseOne()
    {
        //Console.WriteLine("PHASE ONE 1 TIMEOUT: {0}", _timeout);
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
            //Console.WriteLine("(Proposer):Paxos prepare with IDp: {0}", _IDp);
            task.Start(); 
        }
        
        await Task.WhenAll(sendTasks);

        int count = 0; // Count itself (the acceptor)
        foreach (var resultTask in sendTasks)
        {
            Promise promise = await resultTask;
            //Console.WriteLine("(Proposer):Promise received with IDp: {0}", promise.IDp);
            
            // Received a promise (with its ID)?
            if (promise.IDp == _IDp) // Yes, update count
            {
                count++;
                // Did it receive Promise _IDp accepted IDa, value?
                // It needs to update the _value with the Highest IDa (PreviousAcceptedID) that it got
                if (promise.IDa != -1 && (promise.IDa > _IDa)) 
                {
                    List<List<string>> auxLease = new List<List<string>>();
                    foreach (Lease lease in promise.Value)
                    {
                        List<string> list = new List<string>(lease.Value);
                        auxLease.Add(list);
                    }
                    _value = auxLease; // Yes, update value
                    //Console.WriteLine("(Proposer):Value has changed: {0}", PrintLease(_value));
                }
            }
        }
        //Console.WriteLine("(Proposer):Count: {0} | nServers: {1} | Value: {2}", count, _nServers, PrintLease(_value));
        // Did it receive promises from a majority?
        if (count > _nServers / 2)
        {
            _timeout = 1; 
            //Console.WriteLine("(Proposer):Paxos phase 2 with value: {0}", PrintLease(_value));
            PhaseTwo(); // Yes, go to phase 2
        }
        else
        {
            // Retry again with higher ID 
            _IDp += _nServers;
            
            // Timeout to avoid live lock
            //Console.WriteLine("(Proposer):Wait Timeout: {0}", _timeout);
            
            Thread.Sleep(_timeout * 1000); 
            _timeout *= 2;
            //Console.WriteLine("(Proposer):Retrying prepare with new ID: {0}", _IDp);
            PhaseOne();
        }
    }

    private async void PhaseTwo()
    {
        //Console.WriteLine("PHASE TWO 2 TIMEOUT: {0}", _timeout);
        //Console.WriteLine("(Proposer):PhaseTwo Value: {0}", PrintLease(_value));
        var accept = new Accept
        {
            IDp = _IDp,
        };

        foreach (var leaseAux in _value)
        {
            Lease lease = new Lease();
            lease.Value.AddRange(leaseAux);
            accept.Value.Add(lease);
        }
        
        List<Task<Accepted>> sendTasks = new List<Task<Accepted>>();
        sendTasks.Add(new Task<Accepted>(() => _acceptor.DoPhaseTwo(accept))); 
        foreach (var stub in _stubs)
        {
            sendTasks.Add(new Task<Accepted>(() => stub.PaxosPhaseTwo(accept)));
        }
                                                          
        foreach (var task in sendTasks)
        {
            //Console.WriteLine("(Proposer):Value to be accepted: {0}", PrintLease(_value));
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
        
        // Check majority
        if (count > _nServers / 2)
        {
            Console.WriteLine("(Proposer):FINISH PAXOS WITH VALUE: {0} (my id: {1})", PrintLease(_value), _IDp); 
        } 
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

    public void CloseStubs()
    {
        foreach (var ch in _channels)
        {
            ch.ShutdownAsync().Wait();
        }
    }
}