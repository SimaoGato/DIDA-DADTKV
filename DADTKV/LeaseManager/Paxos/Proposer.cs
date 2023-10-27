using Grpc.Net.Client;
using LeaseManager.Domain;

namespace LeaseManager.Paxos;

public class Proposer
{
    private readonly Dictionary<string, GrpcChannel> _channels;
    private readonly Dictionary<string, PaxosService.PaxosServiceClient> _stubs;
    private readonly LeaseManagerState _lmState;
    private readonly LeaseManagerService _lmService;
    private int _round = 1;
    private int _roundReceived = -1;
    private int _IDp; 
    private int _IDa = -1;
    private List<List<string>> _value = new List<List<string>>();
    private readonly int _nServers;
    private readonly Acceptor _acceptor;
    private int _timeout = 1;
    private bool _paxosIsRunning;
  
    public Proposer(int iDp, int nServers,  Dictionary<string, string> servers, Acceptor acceptor, LeaseManagerState lmState, LeaseManagerService lmService)
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
        _lmState = lmState;
        _lmService = lmService;
        _paxosIsRunning = false;
    }

    public void StartPaxos()
    {
        PhaseOne();
    }

    private async void PhaseOne()
    {
        _paxosIsRunning = true;
        
        // Want to propose a value, send prepare ID
        var prepare = new Prepare { IDp = _IDp, Round = _round };
        List<Task<Promise>> sendTasks = SendPrepare(prepare, out int countBadConnections);

        int count = 0;
        while (count <= _nServers / 2 && sendTasks.Count > 0 && countBadConnections < _nServers / 2)
        {
            Task<Promise> completedTask = await Task.WhenAny(sendTasks);
            sendTasks.Remove(completedTask);
            Promise promise = await completedTask;
            
            // Received a promise (with its ID)?
            if (promise.IDp == _IDp) // Yes, update count
            {
                count++;
                // Did it receive Promise _IDp accepted IDa, value?
                if (promise.IDa != -1 && (promise.IDa > _IDa)) 
                {
                    _value = UpdateValue(promise); // Yes, update value
                    _IDa = promise.IDa; 
                }
                _roundReceived = promise.Round;
            }
            else if (promise.IDp == -2) // Receive a ignore response due to suspect
            {
                countBadConnections++;
            }
        }
        
        // Did it receive promises from a majority?
        if (count > _nServers / 2)
        {
            _timeout = 1;
            if (_round == _roundReceived) // If the majority is on the same round as me
            {
                PhaseTwo(); // Yes, go to phase 2
            }
            else // If not, update buffer and move to the next round
            {
                Console.WriteLine("(Proposer ID: {0}): Value decided for previous Round {1}: {2}", _IDp, _round, PrintLease(_value));
                _lmState.RemoveLeases(_value);
                _round++;
                PrepareForNextEpoch();
                PhaseOne();
            }
        }
        else if (countBadConnections < _nServers / 2) // If does not have a majority and is not suspected by a majority
        {
            // Retry again with higher ID 
            _IDp += _nServers;
            // Timeout to avoid live lock
            Thread.Sleep(_timeout * 1000); 
            _timeout *= 2;
            PhaseOne();
        }
        else
        {
            Console.WriteLine("(Proposer ID: {0}): Can't connect to a majority in this time slot, Round: {1}", _IDp, _round);
            _paxosIsRunning = false; // Let LM know that can call another paxos if needed
        }
    }

    private async void PhaseTwo()
    {
        Accept accept = SetValue();
        List<Task<Accepted>> sendTasks = SendAccept(accept, out int countBadConnections);
        
        int count = 0;
        while (count <= _nServers / 2 && sendTasks.Count > 0 && countBadConnections < _nServers / 2)
        {
            Task<Accepted> completedTask = await Task.WhenAny(sendTasks);
            sendTasks.Remove(completedTask);
            Accepted accepted = await completedTask;
            
            // Confirmation of acceptance ?
            if (accepted.IDp == _IDp) 
            {
                count++; // Yes
                _roundReceived = accepted.Round;
            }
            else if (accepted.IDp == -2) // Receive a ignore response due to suspect
            {
                countBadConnections++;
            }
        }
        
        // Check majority
        if (count > _nServers / 2)
        {
            if (_round < _roundReceived) // Acceptors actually were in a higher round
            {
                Console.WriteLine("(Proposer ID: {0}): Value decided for previous Round {1}: {2}", _IDp, _round, PrintLease(_value));
                _lmState.RemoveLeases(_value);
                _round++;
                PrepareForNextEpoch();
                PhaseOne();
            }
            else // Acceptors were in the same round as me
            {
                Console.WriteLine("(Proposer ID: {0}): Value decided for Round {1}: {2}", _IDp, _round, PrintLease(_value));
                _lmService.SendLeases(_round, _value);
                _lmState.RemoveLeases(_value);
                _round++; 
                _paxosIsRunning = false; // Let LM know that can call another paxos if needed
            }
        }
        else if (countBadConnections >= _nServers / 2)
        {
            Console.WriteLine("(Proposer ID: {0}): Can't connect to a majority in this time slot, Round: {1}", _IDp, _round);
            _paxosIsRunning = false; // Let LM know that can call another paxos if needed
        }
        else
        {
            PhaseOne();
        }
    }

    private List<Task<Promise>> SendPrepare(Prepare prepare, out int countBadConnections)
    {
        List<Task<Promise>> sendTasks = new List<Task<Promise>>();
        sendTasks.Add(Task.Run(() => _acceptor.DoPhaseOne(prepare)));
        
        countBadConnections = _nServers - (_stubs.Count + 1); // Count with crashes
        foreach (var stub in _stubs)
        {
            if (!Suspects.IsSuspected(stub.Key))
            {
                sendTasks.Add(Task.Run(() => stub.Value.PaxosPhaseOne(prepare)));
            }
            else
            {
                countBadConnections++;
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

    private List<Task<Accepted>> SendAccept(Accept accept, out int countBadConnections)
    {
        List<Task<Accepted>> sendTasks = new List<Task<Accepted>>();
        sendTasks.Add(Task.Run(() => _acceptor.DoPhaseTwo(accept)));
        
        countBadConnections = _nServers - (_stubs.Count + 1); // Count with crashes
        foreach (var stub in _stubs)
        {
            if (!Suspects.IsSuspected(stub.Key))
            {
                sendTasks.Add(Task.Run(() => stub.Value.PaxosPhaseTwo(accept)));
            }
            else
            {
                countBadConnections++;
            }
        }

        return sendTasks;
    }

    private Accept SetValue()
    {
        var accept = new Accept { IDp = _IDp, Round = _round};
        
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
        _IDa = -1;
        _value.Clear();
        _value = _lmState.RequestedLeases.ToList();
    }

    private static string PrintLease(List<List<string>> value)
    {
        string result = "";
        foreach (var lease in value)
        {
            result = result + lease[1].Substring(0,9) + " | ";
        }
        
        if (result == "")
        {
            result += "{ Empty }";
        }
        else
        {
            result = result.Remove(result.Length - 3);
        }

        return result;
    }
    
    public bool PaxosIsRunning()
    {
        return _paxosIsRunning;
    }
    
    public void RemoveNode(string nickname, int id)
    {
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