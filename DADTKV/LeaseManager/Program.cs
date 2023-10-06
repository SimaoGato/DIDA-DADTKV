using Grpc.Core;
using LeaseManager;
using LeaseManager.Paxos;
using Timer = System.Timers.Timer;

class Program
{
    private LeaseManagerState _lmState;
    
    // TODO: Review these names
    private string _lmNick;
    private string _lmUrl;
    private int _lmId;
    private int _numberOfLM;
    private List<string> _lmServers;
    private int _numberOfTm;
    private List<string> tmServers;
    private int timeSlots;
    private int slotDuration;
    private int slotBehaviorCount;
    private List<KeyValuePair<string, string>> slotBehavior;
    private DateTime startTime;
    
    private Server server;
    private Proposer _proposer;
    private Acceptor _acceptor;
    private LeaseManagerService _leaseManagerService;
    
    static ManualResetEvent waitHandle = new ManualResetEvent(false);


    private List<List<string>> value;
    
    private Program(string[] args)
    {
        _lmState = new LeaseManagerState();
        var lmLogic = new LeaseManagerLogic(args);
        _lmNick = lmLogic.lmNick;
        _lmUrl = lmLogic.lmUrl;
        _lmId = lmLogic.lmId;
        _numberOfLM = lmLogic.numberOfLm;
        _lmServers = lmLogic.ParseLmServers();
        _numberOfTm = lmLogic.numberOfTm;
        tmServers = lmLogic.ParseTmServers();
        slotBehavior = lmLogic.ParseSlotBehavior();
        timeSlots = lmLogic.timeSlots;
        slotDuration = lmLogic.slotDuration;
        startTime = lmLogic.startTime;
    }
    
    public static void Main(string[] args) {
        Program program = new Program(args);
        
        Uri lmUri = new Uri(program._lmUrl);
        Console.WriteLine(lmUri.Host + "-" + lmUri.Port);
        
        program._leaseManagerService = new LeaseManagerService(program.tmServers);
        program._acceptor = new Acceptor(program._leaseManagerService);
        program._proposer = new Proposer(program._lmId, program._numberOfLM, program._lmServers, program._acceptor);
        
        program.server = new Server
        {
            Services =
            {
                LeaseService.BindService(new TransactionManagerServiceImpl(program._lmState)),
                ClientStatusService.BindService(new ClientStatusServiceImpl(program._lmNick)),
                PaxosService.BindService(program._acceptor)
            }, 
            Ports = { new ServerPort(lmUri.Host, lmUri.Port, ServerCredentials.Insecure) }
        };
        
        program.server.Start();
        Console.WriteLine("Starting Lease Manager on port: {0}", lmUri.Port);
        
        TimeSpan timeToStart = program.startTime - DateTime.Now;
        int msToWait = (int)timeToStart.TotalMilliseconds;
        Console.WriteLine($"Starting in {timeToStart} s");
        Timer slotTimer = new Timer(msToWait);
        slotTimer.Elapsed += (_, _) => program.StartProgram();
        slotTimer.AutoReset = false;
        slotTimer.Start();
        waitHandle.WaitOne();
    }

    private void StartProgram() {
        
        // TODO see if it has to be synchronized
        int counter = 0;
        Timer slotTimer = new Timer(slotDuration);
        slotTimer.Elapsed += (sender, e) =>
        {
            _proposer.PrepareForNextEpoch();
            _acceptor.PrepareForNextEpoch();
            Paxos();
            counter++;
            if (counter >= timeSlots) slotTimer.Stop();
        };
        slotTimer.AutoReset = true;
        slotTimer.Start();

        Console.WriteLine("Press any key to stop...");
        Console.ReadKey();
        server.ShutdownAsync().Wait();
        waitHandle.Set();
    }

    private void Paxos()
    {
        //_proposer.Value = _lmState.RequestedLeases;
        _proposer.Value.AddRange(_lmState.RequestedLeases);
        Console.WriteLine("------ STARTING MULTI-PAXOS ------");
        _proposer.StartPaxos();
        _lmState.CleanRequestedLeases();
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
}