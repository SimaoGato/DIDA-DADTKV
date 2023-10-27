using Grpc.Core;
using LeaseManager.Domain;
using LeaseManager.Paxos;
using LeaseManager.Services;
namespace LeaseManager;

class Program
{
    private readonly LeaseManagerState _lmState;
    private readonly string _lmNick;
    private readonly string _lmUrl;
    private readonly int _lmId;
    private readonly Dictionary<int, string> _lmIdsMap;
    private readonly Dictionary<int, string> _tmIdsMap;
    private readonly DateTime _startTime;
    private readonly int _timeSlots;
    private readonly int _slotDuration;
    private readonly Dictionary<int, List<string>> _slotBehaviors;
    private readonly Server _server;
    private readonly Proposer _proposer;
    private readonly Acceptor _acceptor;
    private readonly LeaseManagerService _lmService;
    
    private Program(string[] args)
    {
        _lmState = new LeaseManagerState();
        var lmConfig = new LeaseManagerConfiguration(args);
        _lmNick = lmConfig.lmNick;
        _lmUrl = lmConfig.lmUrl;
        _lmId = lmConfig.lmId;
        int _numberOfLm = lmConfig.numberOfLm;
        _lmIdsMap = lmConfig.lmIdsMap;
        Dictionary<string, string> lmServers = lmConfig.lmServers;
        Suspects.SetMaps(_lmIdsMap);
        _tmIdsMap = lmConfig.tmIdsMap;
        Dictionary<string, string> tmServers = lmConfig.tmServers;
        _slotBehaviors = lmConfig.slotBehaviors;
        _timeSlots = lmConfig.timeSlots;
        _slotDuration = lmConfig.slotDuration;
        _startTime = lmConfig.startTime;
        _lmService = new LeaseManagerService(tmServers);
        _acceptor = new Acceptor(_lmId, _numberOfLm);
        _proposer = new Proposer(_lmId, _numberOfLm, lmServers, _acceptor, _lmState, _lmService);
        
        var lmUri = new Uri(_lmUrl);
        _server = new Server
        {
            Services =
            {
                LeaseService.BindService(new TransactionManagerLeaseServiceImpl(_lmState)),
                ClientStatusService.BindService(new ClientStatusServiceImpl(_lmNick)),
                PaxosService.BindService(_acceptor)
            }, 
            Ports = { new ServerPort(lmUri.Host, lmUri.Port, ServerCredentials.Insecure) }
        };
    }
    
    public static void Main(string[] args) 
    {
        Program program = new Program(args);
        program._server.Start();
        Console.WriteLine("Starting Lease Manager on: {0}", program._lmUrl);
        
        TimeSpan timeToStart = program._startTime - DateTime.Now;
        int msToWait = (int)timeToStart.TotalMilliseconds;
        Console.WriteLine($"Starting in {timeToStart} s");
        Thread.Sleep(msToWait);
        
        int timeSlot = 1;
        while (timeSlot <= program._timeSlots)
        {
            Console.WriteLine("\n(LM): [STARTING TIMESLOT {0}...]", timeSlot);
            Thread.Sleep(program._slotDuration);
            program._lmState.PrintBuffer();
            if (program.CheckCrashes(timeSlot + 1)) // There are no crashes in the first round 
            {
                break;
            }
            if (program._proposer.PaxosIsRunning())
            {
                Console.WriteLine("(LM): There is a Paxos round still running...");
                timeSlot++;
                continue; // Can't call paxos if is still running
            }
            program.Paxos();
            timeSlot++;
        }
        
        Console.WriteLine();
        Console.WriteLine("(LM): [ENDING PROGRAM...]");
        Console.WriteLine("(LM): Press any key to close...");
        Console.ReadKey();
        program.ShutDown();
    }

    private void Paxos()
    {
        _proposer.PrepareForNextEpoch();
        _acceptor.PrepareForNextEpoch();
        _proposer.StartPaxos();
    }

    private void ShutDown()
    {
        _lmService.CloseStubs();
        _proposer.CloseStubs();
        _server.ShutdownAsync().Wait();
    }

    private bool CheckCrashes(int round)
    {
        Suspects.ResetSuspects();
        if (!_slotBehaviors.ContainsKey(round)) // No changes for this round
        {
            return false;
        }
        
        List<string> behaviors = _slotBehaviors[round];
        string tmStatus = behaviors[0];
        for (int i = 0; i < tmStatus.Length; i++)
        {
            if (tmStatus[i] == 'C' && _tmIdsMap.ContainsKey(i))
            {
                Console.WriteLine("(LM): Need to close connection to TM: " + _tmIdsMap[i]);
                string nick = _tmIdsMap[i];
                _lmService.RemoveStub(nick);
                _tmIdsMap.Remove(i);
            }
        }
        
        string lmStatus = behaviors[1];
        if (lmStatus[_lmId] == 'C')
        {
            Console.WriteLine("(LM): I am crashing...");
            return true;
        }
        for (int i = 0; i < lmStatus.Length; i++)
        {
            if (lmStatus[i] == 'C' && _lmIdsMap.ContainsKey(i))
            {
                Console.WriteLine("(LM): Need to close connection to LM: " + _lmIdsMap[i]);
                string nick = _lmIdsMap[i];
                _proposer.RemoveNode(nick, i);
                _lmIdsMap.Remove(i);
            }    
        }

        if (behaviors.Count == 3) // There are suspects
        {
            string[] suspectStatus = behaviors[2].Split("+");
            foreach (var pair in suspectStatus)
            {
                string trimedPair = pair.TrimStart('(').TrimEnd(')');
                string[] ids = trimedPair.Split(",");

                if (ids[0] == _lmNick)
                {
                    Console.WriteLine("(LM): I suspect this server: " + ids[1]);
                    Suspects.SetSuspected(ids[1]);
                }
            }
        }
        
        return false;
    }
}