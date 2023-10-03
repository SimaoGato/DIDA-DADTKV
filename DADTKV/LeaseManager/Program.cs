using Grpc.Core;
using LeaseManager;
using Timer = System.Timers.Timer;

class Program
{
    // TODO: Save leaseManagers adresses in a dictionary
    // TODO: receive from args the list of nicks and addresses (the first one should be this lease manager)

    private LeaseManagerLogic lmLogic;
    static ManualResetEvent waitHandle = new ManualResetEvent(false);
    
    private Program(string[] args) 
    {
        lmLogic = new LeaseManagerLogic(args);
    }
    
    public static void Main(string[] args) {
        Program program = new Program(args);
        TimeSpan timeToStart = program.lmLogic.startTime - DateTime.Now;
        int msToWait = (int)timeToStart.TotalMilliseconds;
        Console.WriteLine($"Starting in {timeToStart} s");
        Timer slotTimer = new Timer(msToWait);
        slotTimer.Elapsed += (_, _) => program.StartProgram();
        slotTimer.AutoReset = false;
        slotTimer.Start();
        waitHandle.WaitOne();
    }

    private void StartProgram() {
        string lmNick = lmLogic.lmNick;
        string lmUrl = lmLogic.lmUrl;
        List<string> lmServers = lmLogic.ParseLmServers();
        List<string> tmServers = lmLogic.ParseTmServers();
        var slotBehavior = lmLogic.ParseSlotBehavior();
        var timeSlots = lmLogic.timeSlots;
        var slotDuration = lmLogic.slotDuration;
        
        Console.WriteLine("tmNick: " + lmNick);
        Console.WriteLine("tmUrl: " + lmUrl);
        Console.WriteLine("LmServers: ");
        foreach (var lmServer in lmServers)
        {
            Console.WriteLine(lmServer);
        }
        Console.WriteLine("TmServers: ");
        foreach (var tmServer in tmServers)
        {
            Console.WriteLine(tmServer);
        }
        Console.WriteLine("slotBehavior: ");
        foreach (var slot in slotBehavior)
        {
            Console.WriteLine(slot.Key + "-" + slot.Value);
        }
        Console.WriteLine("timeSlots: " + timeSlots);
        Console.WriteLine("slotDuration: " + slotDuration);
        Console.WriteLine("----------");
        
        Uri lmUri = new Uri(lmUrl);
        Console.WriteLine(lmUri.Host + "-" + lmUri.Port);

        Server server = new Server
        {
            Services = { LeaseService.BindService(new TransactionManagerServiceImpl()) }, 
            Ports = { new ServerPort(lmUri.Host, lmUri.Port, ServerCredentials.Insecure) }
        };

        server.Start();

        // TODO see if it has to be synchronized
        int counter = 0;
        Timer slotTimer = new Timer(slotDuration);
        slotTimer.Elapsed += (sender, e) =>
        {
            foo(); // TODO change to paxos func
            counter++;
            if (counter >= timeSlots) slotTimer.Stop();
        };
        slotTimer.AutoReset = true;
        slotTimer.Start();
        
        Console.WriteLine("Starting Lease Manager on port: {0}", lmUri.Port);
        Console.WriteLine("Press any key to stop...");
        Console.ReadKey();

        server.ShutdownAsync().Wait();
        waitHandle.Set();
    }

    static void foo() {
        Console.WriteLine("ran function");
    }
}