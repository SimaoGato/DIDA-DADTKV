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
    private LeaseManagerService _leaseManagerService;
    
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
        _leaseManagerService = new LeaseManagerService(_tmServers);
        _acceptor = new Acceptor(_leaseManagerService);
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
        
        Timer slotTimer = new Timer(msToWait);
        slotTimer.Elapsed += (_, _) => program.StartProgram();
        slotTimer.AutoReset = false;
        slotTimer.Start();
        _waitHandle.WaitOne();
    }

    private void StartProgram() 
    {
        int counter = 0;
        Timer slotTimer = new Timer(_slotDuration);
        slotTimer.Elapsed += (sender, e) =>
        {
            _proposer.PrepareForNextEpoch();
            _acceptor.PrepareForNextEpoch();
            Paxos();
            counter++;
            if (counter >= _timeSlots) slotTimer.Stop();
        };
        slotTimer.AutoReset = true;
        slotTimer.Start();

        Console.WriteLine("Press any key to stop...");
        Console.ReadKey();
        _server.ShutdownAsync().Wait();
        _waitHandle.Set();
    }

    private void Paxos()
    {
        _proposer.Value.AddRange(_lmState.RequestedLeases);
        Console.WriteLine("------ STARTING MULTI-PAXOS ------");
        _proposer.StartPaxos();
        _lmState.CleanRequestedLeases();
    }
}