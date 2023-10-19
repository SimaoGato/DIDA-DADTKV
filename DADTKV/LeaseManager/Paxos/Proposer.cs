using Grpc.Net.Client;

namespace LeaseManager.Paxos;

public class Proposer
{
    private Dictionary<string, GrpcChannel> _channels;
    private Dictionary<string, PaxosService.PaxosServiceClient> _stubs;
    private int _IDp; 
    private int _IDa = -1;
    private List<List<string>> _value = new List<List<string>>();
    private int _nServers;
    private Acceptor _acceptor;
    private int _timeout = 1;
    private bool _secondPhase = true;

    public List<List<string>> Value
    {
        get { return _value; }
        set { _value = value; }
    }
    
    public void PrepareForNextEpoch()
    {
        _IDa = -1;
        _secondPhase = true; // Reset second phase
        _value.Clear();
    }
  
    public Proposer(int iDp, int nServers,  Dictionary<string, string> servers, Acceptor acceptor)
    {
        _IDp = iDp;
        _nServers = nServers;
        _channels = new Dictionary<string, GrpcChannel>();
        _stubs = new Dictionary<string, PaxosService.PaxosServiceClient>();
        foreach (var server in servers)
        {
            var channel = GrpcChannel.ForAddress(server.Value);
            var stub = new PaxosService.PaxosServiceClient(channel);
            _channels.Add(server.Key, channel);
            _stubs.Add(server.Key, stub);
        }

        _acceptor = acceptor;
    }

    public void StartPaxos()
    {
        var leaderId = _acceptor.LeaderID;
        Console.WriteLine("Leader's ID: {0} from LM: {1}", leaderId, (leaderId % _nServers));
        
        // If there is no leader, start with phase 1
        if (leaderId == -1)
        {
            //Console.WriteLine("START WITH PHASE ONE 1");
            PhaseOne();
        }
        else if (_IDp == leaderId) // Multi-Paxos
        {
            //Console.WriteLine("START WITH PHASE ONE 2");
            PhaseTwo();
        }
    }

    public async void PhaseOne()
    {
        Console.WriteLine("Phase One");
        // Want to propose a value, send prepare ID
        var prepare = new Prepare
        {
            IDp = _IDp
        };
        
        List<Task<Promise>> sendTasks = new List<Task<Promise>>();
        sendTasks.Add(Task.Run(() => _acceptor.DoPhaseOne(prepare)));
        foreach (var stub in _stubs)
        {
            sendTasks.Add(Task.Run(() => stub.Value.PaxosPhaseOne(prepare)));
        }

        int count = 0;
        while (count <= _nServers / 2 && sendTasks.Count > 0)
        {
            //Console.WriteLine("COUNT: {0}", count);
            Task<Promise> completedTask = await Task.WhenAny(sendTasks);
            sendTasks.Remove(completedTask);
            
            Promise promise = await completedTask;
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
                    _IDa = promise.IDa; 
                    _secondPhase = false; // No need to go to phase 2
                    //Console.WriteLine("(Proposer):Value has changed: {0}", PrintLease(_value));
                }
            }
        }
        //Console.WriteLine("(Proposer):Count: {0} | nServers: {1} | Value: {2}", count, _nServers, PrintLease(_value));
        
        // Did it receive promises from a majority?
        if ((count > _nServers / 2) && _secondPhase)
        {
            _timeout = 1;
            _secondPhase = false; // TODO: [CHECK] this is here in case that a slow task tries to do phase 2 after the proposer is already doing it
            Console.WriteLine("(Proposer):GO TO PAXOS PHASE 2 with value: {0}", PrintLease(_value));
            PhaseTwo(); // Yes, go to phase 2
        }
        else if (_secondPhase)
        {
            // Retry again with higher ID 
            _IDp += _nServers;
            
            // Timeout to avoid live lock
            Console.WriteLine("(Proposer):Wait Timeout: {0}", _timeout);
            
            Thread.Sleep(_timeout * 1000); 
            _timeout *= 2;
            //Console.WriteLine("(Proposer):Retrying prepare with new ID: {0}", _IDp);
            PhaseOne();
        }
        else
        {
            Console.WriteLine("(P: NEW LEADER): With ID: {0}, the VALUE is: {1}, from IDA: {2}", _IDp, PrintLease(_value), _IDa);
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
        sendTasks.Add(Task.Run(() => _acceptor.DoPhaseTwo(accept)));
        foreach (var stub in _stubs)
        {
            sendTasks.Add(Task.Run(() => stub.Value.PaxosPhaseTwo(accept)));
        }

        int count = 0;
        while (count <= _nServers / 2 && sendTasks.Count > 0)
        {
            Task<Accepted> completedTask = await Task.WhenAny(sendTasks);
            sendTasks.Remove(completedTask);
            
            Accepted accepted = await completedTask;
            
            // Confirmation of acceptance ?
            if (accepted.IDp == _IDp) // Yes
            {
                count++;
            }
        }
        
        // Check majority
        if (count > _nServers / 2)
        {
            Console.WriteLine("(Proposer):FINISH PAXOS WITH VALUE: {0} (my id: {1})", PrintLease(_value), _IDp); 
        } 
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

    public void CloseStubs()
    {
        foreach (var ch in _channels)
        {
            ch.Value.ShutdownAsync().Wait();
        }
    }
}