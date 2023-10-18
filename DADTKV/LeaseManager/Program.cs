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
    private List<string> _lmServers;
    private List<string> _tmServers;
    private DateTime _startTime;
    private int _timeSlots;
    private int _slotDuration;
    private int _slotBehaviorCount;
    private List<KeyValuePair<string, string>> _slotBehavior;
    static ManualResetEvent _waitHandle = new ManualResetEvent(false);
    
    private Server _server;
    private Proposer _proposer;
    private Acceptor _acceptor;
    private LeaseManagerService _lmService;
    
    private Program(string[] args)
    {
        _lmState = new LeaseManagerState();
        var lmLogic = new LeaseManagerConfiguration(args);
        _lmNick = lmLogic.lmNick;
        _lmUrl = lmLogic.lmUrl;
        _lmId = lmLogic.lmId;
        _numberOfLm = lmLogic.numberOfLm;
        _lmServers = lmLogic.ParseLmServers();
        _tmServers = lmLogic.ParseTmServers();
        _slotBehavior = lmLogic.ParseSlotBehavior();
        _timeSlots = lmLogic.timeSlots;
        _slotDuration = lmLogic.slotDuration;
        _startTime = lmLogic.startTime;
        _lmService = new LeaseManagerService(_tmServers);
        _acceptor = new Acceptor(_lmService);
        _proposer = new Proposer(_lmId, _numberOfLm, _lmServers, _acceptor);
        
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
        
        int round = 0;
        ManualResetEvent timerFinished = new ManualResetEvent(true);
        Timer slotTimer = new Timer(program._slotDuration);
        slotTimer.Elapsed += (_, _) =>
        {
            if (timerFinished.WaitOne(0))
            {
                Console.WriteLine("---Slot " + round + " started---");
                timerFinished.Reset(); 
                program._proposer.PrepareForNextEpoch();
                program._acceptor.PrepareForNextEpoch();
                program.Paxos();
                round++;
                if (round >= program._timeSlots) slotTimer.Stop();
                timerFinished.Set();
            }
        };
        slotTimer.AutoReset = true;
        slotTimer.Start();
        
        Console.WriteLine("Press any key to stop...");
        Console.ReadKey();
        program.ShutDown();
    }

    private void Paxos()
    {
        _proposer.Value.AddRange(_lmState.RequestedLeases);
        Console.WriteLine("------ STARTING MULTI-PAXOS ------");
        _proposer.StartPaxos();
        _lmState.CleanRequestedLeases();
    }

    private void ShutDown()
    {
        _lmService.CloseStubs();
        _proposer.CloseStubs();
        _server.ShutdownAsync().Wait();
    }
}