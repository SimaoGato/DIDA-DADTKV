using Grpc.Net.Client;

namespace LeaseManager.Paxos;

public class Proposer
{
    private readonly Dictionary<string, GrpcChannel> _channels;
    private readonly Dictionary<string, PaxosService.PaxosServiceClient> _stubs;
    private int _IDp; 
    private int _IDa = -1;
    private List<List<string>> _value = new List<List<string>>();
    private readonly int _nServers;
    private readonly Acceptor _acceptor;
    private int _leaderId;
    private int _timeout = 1;

    public List<List<string>> Value
    {
        get { return _value; }
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
        Task t = PhaseOne();
        t.Wait();
        // // If there is no leader, start with phase 1
        // if (_leaderId == -1)
        // {
        //     //Console.WriteLine("START WITH PHASE ONE 1");
        //     PhaseOne();
        // }
        // else if (_IDp == _leaderId) // Multi-Paxos
        // {
        //     //Console.WriteLine("START WITH PHASE ONE 2");
        //     PhaseTwo();
        // }
    }

    public async Task PhaseOne()
    {
        Console.WriteLine("(Proposer): Phase One");
        // Want to propose a value, send prepare ID
        var prepare = new Prepare { IDp = _IDp };

        List<Task<Promise>> sendTasks = SendPrepare(prepare, out int countSuspects);

        int count = 0;
        while (count <= _nServers / 2 && sendTasks.Count > 0 && countSuspects < _nServers / 2)
        {
            Task<Promise> completedTask = await Task.WhenAny(sendTasks);
            sendTasks.Remove(completedTask);
            Promise promise = await completedTask;
            
            // Received a promise (with its ID)?
            if (promise.IDp == _IDp) // Yes, update count
            {
                count++;
                Console.WriteLine("I've got a promise from, count: {0}, IDp: {1}", count, _IDp);
                // Did it receive Promise _IDp accepted IDa, value?
                if (promise.IDa != -1 && (promise.IDa > _IDa)) 
                {
                    _value = UpdateValue(promise); // Yes, update value
                    _IDa = promise.IDa; 
                    //Console.WriteLine("(Proposer):Value has changed: {0}", PrintLease(_value));
                }
            }
            else if (promise.IDp == -2) // Receive a ignore response due to suspect
            {
                Console.WriteLine("Received an ignore response due to suspect");
                countSuspects++;
            }
        }
        
        // Did it receive promises from a majority?
        if (count > _nServers / 2)
        {
            _timeout = 1;
            Console.WriteLine("(Proposer): I got majority, go to Phase 2 with value: {0}", PrintLease(_value));
            Task t = PhaseTwo(); // Yes, go to phase 2
            t.Wait();
        }
        else if (countSuspects < _nServers / 2) // If does not have a majority and is not suspected by a majority
        {
            // Retry again with higher ID 
            _IDp += _nServers;
            // Timeout to avoid live lock
            Console.WriteLine("(Proposer): Wait Timeout: {0}", _timeout);
            Thread.Sleep(_timeout * 1000); 
            _timeout *= 2;
            Console.WriteLine("(Proposer): Retrying Prepare with new ID: {0}", _IDp);
            Task t = PhaseOne();
            t.Wait();
        }
        else
        {
            Console.WriteLine("(Proposer): Can't connect to a majority in Phase One");
        }
    }

    private async Task PhaseTwo()
    {
        Accept accept = SetValue();
        List<Task<Accepted>> sendTasks = SendAccept(accept, out int countSuspects);
        
        int count = 0;
        while (count <= _nServers / 2 && sendTasks.Count > 0 && countSuspects < _nServers / 2)
        {
            Task<Accepted> completedTask = await Task.WhenAny(sendTasks);
            sendTasks.Remove(completedTask);
            Accepted accepted = await completedTask;
            
            // Confirmation of acceptance ?
            if (accepted.IDp == _IDp) 
            {
                count++; // Yes
            }
            else if (accepted.IDp == -2) // Receive a ignore response due to suspect
            {
                countSuspects++;
            }
        }
        
        // Check majority
        if (count > _nServers / 2)
        {
            Console.WriteLine("(Proposer): !! Finish Paxos with value: {0} (my id: {1})", PrintLease(_value), _IDp); 
        }
        else if (countSuspects >= _nServers / 2)
        {
            Console.WriteLine("(Proposer): Can't connect to a majority in Phase Two");
        }
        else
        {
            Console.WriteLine("(Proposer): Didn't achieve majority in Phase 2, count: {0}", count);
        }
    }

    private List<Task<Promise>> SendPrepare(Prepare prepare, out int countSuspects)
    {
        List<Task<Promise>> sendTasks = new List<Task<Promise>>();
        Console.WriteLine("(Proposer): Sending prepare to: My Acceptor, my id: {0}", _IDp);
        sendTasks.Add(Task.Run(() => _acceptor.DoPhaseOne(prepare)));
        
        countSuspects = 0;
        foreach (var stub in _stubs)
        {
            if (!Suspects.IsSuspected(stub.Key))
            {
                Console.WriteLine("(Proposer): Sending prepare to: {0}, my id: {1}", stub.Key, _IDp);
                sendTasks.Add(Task.Run(() => stub.Value.PaxosPhaseOne(prepare)));
            }
            else
            {
                countSuspects++;
            }
        }

        return sendTasks;
    }

    private static List<List<string>> UpdateValue(Promise promise)
    {
        List<List<string>> auxLease = new List<List<string>>();
        foreach (Lease lease in promise.Value)
        {
            List<string> list = new List<string>(lease.Value);
            auxLease.Add(list);
        }
        
        return auxLease;
    }

    private List<Task<Accepted>> SendAccept(Accept accept, out int countSuspects)
    {
        List<Task<Accepted>> sendTasks = new List<Task<Accepted>>();
        Console.WriteLine("(Proposer): Sending accept to: My Acceptor, my id: {0}", _IDp);
        sendTasks.Add(Task.Run(() => _acceptor.DoPhaseTwo(accept)));
        
        countSuspects = 0;
        foreach (var stub in _stubs)
        {
            if (!Suspects.IsSuspected(stub.Key))
            {
                Console.WriteLine("(Proposer): Sending accept to: {0}, my id: {1}", stub.Key, _IDp);
                sendTasks.Add(Task.Run(() => stub.Value.PaxosPhaseTwo(accept)));
            }
            else
            {
                countSuspects++;
            }
        }

        return sendTasks;
    }

    private Accept SetValue()
    {
        var accept = new Accept { IDp = _IDp };
        
        foreach (var leaseAux in _value)
        {
            Lease lease = new Lease();
            lease.Value.AddRange(leaseAux);
            accept.Value.Add(lease);
        }

        return accept;
    }
    
    public void PrepareForNextEpoch()
    {
        _leaderId = _acceptor.LeaderID;
        _IDa = -1;
        _value.Clear();
    }

    private static string PrintLease(List<List<string>> value)
    {
        string result = "";
        foreach (var lease in value)
        {
            // string leaseAux = "";
            // foreach (var str in lease)
            // {
            //     leaseAux = leaseAux + " " + str;
            // }
            // result = result + leaseAux + " | ";
            result = result + lease[0] + " | ";
        }

        return result;
    }
    
    public void RemoveNode(string nickname, int id)
    {
        if (_leaderId % _nServers == id)
        {
            _leaderId = -1; // leader has crashed
        }
        _channels[nickname].ShutdownAsync().Wait();
        _channels.Remove(nickname);
        _stubs.Remove(nickname);
    }

    public void CloseStubs()
    {
        foreach (var ch in _channels)
        {
            ch.Value.ShutdownAsync().Wait();
        }
    }
}