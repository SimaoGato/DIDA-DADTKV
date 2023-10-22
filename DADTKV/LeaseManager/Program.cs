using Grpc.Core;
using LeaseManager;
using LeaseManager.Paxos;
using Timer = System.Timers.Timer;

class Program
{
    private LeaseManagerState _lmState;
    private string _lmNick;
    private string _lmUrl;
    private int _lmId;
    private int _numberOfLm;
    private Dictionary<int, string> _lmIdsMap;
    private int _numberOfTm;
    private Dictionary<int, string> _tmIdsMap;
    private DateTime _startTime;
    private int _timeSlots;
    private int _slotDuration;
    private Dictionary<int, List<string>> _slotBehaviors;
    private Server _server;
    private Proposer _proposer;
    private Acceptor _acceptor;
    private LeaseManagerService _lmService;
    
    private Program(string[] args)
    {
        _lmState = new LeaseManagerState();
        var lmConfig = new LeaseManagerConfiguration(args);
        _lmNick = lmConfig.lmNick;
        _lmUrl = lmConfig.lmUrl;
        _lmId = lmConfig.lmId;
        _numberOfLm = lmConfig.numberOfLm;
        _lmIdsMap = lmConfig.lmIdsMap;
        Dictionary<string, string> lmServers = lmConfig.lmServers;
        Suspects.SetMaps(_lmIdsMap);
        _numberOfTm = lmConfig.numberOfTm;
        _tmIdsMap = lmConfig.tmIdsMap;
        Dictionary<string, string> tmServers = lmConfig.tmServers;
        _slotBehaviors = lmConfig.slotBehaviors;
        _timeSlots = lmConfig.timeSlots;
        _slotDuration = lmConfig.slotDuration;
        _startTime = lmConfig.startTime;
        _lmService = new LeaseManagerService(tmServers);
        _acceptor = new Acceptor(_lmId, _numberOfLm, _lmService);
        _proposer = new Proposer(_lmId, _numberOfLm, lmServers, _acceptor);
        
        var lmUri = new Uri(_lmUrl);
        _server = new Server
        {
            Services =
            {
                LeaseService.BindService(new TransactionManagerServiceImpl(_lmState)),
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
        
        int round = 1;
        ManualResetEvent timerFinished = new ManualResetEvent(true);
        Timer slotTimer = new Timer(program._slotDuration);
        slotTimer.Elapsed += (_, _) =>
        {
            if (timerFinished.WaitOne(0))
            {
                Console.WriteLine();
                Console.WriteLine("[ROUND: " + round + "]");
                timerFinished.Reset();
                program._proposer.PrepareForNextEpoch();
                program._acceptor.PrepareForNextEpoch();
                bool isCrashed = program.CheckCrashes(round);
                if (!isCrashed)
                {
                    // TODO: Maybe call paxos only if we have a majority (?)
                    program.Paxos();
                }
                round++;
                if ((round > program._timeSlots) || isCrashed)
                {
                    slotTimer.Stop();
                    // TODO: Do automatic shutdown (?)
                    Console.WriteLine();
                    Console.WriteLine("[ENDING PROGRAM...]");
                    Console.WriteLine("Press any key to close...");
                }
                timerFinished.Set();
            }
        };
        slotTimer.AutoReset = true;
        slotTimer.Start();
        
        Console.WriteLine("(LM): Press any key to stop...");
        Console.ReadKey();
        program.ShutDown();
    }

    private void Paxos()
    {
        _proposer.Value.AddRange(_lmState.RequestedLeases);
        Console.WriteLine("(LM): Starting Paxos...");
        _proposer.PhaseOne();
        Console.WriteLine("(LM): Paxos has Finished");
        _lmState.CleanRequestedLeases();
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
        Console.WriteLine("(LM): Checking Crashes...");
        if (!_slotBehaviors.ContainsKey(round))
        {
            Console.WriteLine("(LM): No behavior change for this round");
            return false;
        }
        
        List<string> behaviors = _slotBehaviors[round];
        string tmStatus = behaviors[0];
        for (int i = 0; i < tmStatus.Length; i++)
        {
            if (tmStatus[i] == 'C' && _tmIdsMap.ContainsKey(i))
            {
                Console.WriteLine("(LM): Need to close connection to TM: " + i);
                string nick = _tmIdsMap[i];
                _lmService.RemoveStub(nick);
                _tmIdsMap.Remove(i);
            }
        }
        
        string lmStatus = behaviors[1];
        if (lmStatus[_lmId] == 'C')
        {
            Console.WriteLine("(LM): I am crashing... my ID: " + _lmId);
            return true;
        }
        for (int i = 0; i < lmStatus.Length; i++)
        {
            if (lmStatus[i] == 'C' && _lmIdsMap.ContainsKey(i))
            {
                Console.WriteLine("(LM): Need to close connection to LM: " + i);
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