using Grpc.Core;
using System;
using System.Collections.Generic;
using System.Threading;
using TransactionManager;
using Timer = System.Timers.Timer;

class Program
{
    private readonly TransactionManagerConfiguration _tmConfiguration;
    private static readonly ManualResetEvent WaitHandle = new ManualResetEvent(false);
    private string tmNick;
    private string tmUrl;
    private int tmId;
    private List<KeyValuePair<int, string>> tmServersIdMap;
    private Dictionary<string, string> tmNickMap;
    private List<string> tmNicks;
    private List<string> tmServers;
    private List<KeyValuePair<int, string>> lmServersIdMap;
    private Dictionary<string, string> lmNickMap;
    private List<string> lmNicks;
    private List<string> lmServers;
    private List<KeyValuePair<string, string>> slotBehavior;
    private int timeSlots;
    private int slotDuration;
    private DateTime startTime;
    private bool _isRunning = true;
    private static readonly object SlotBehaviorLock = new object();

    private Program(string[] args)
    {
        _tmConfiguration = new TransactionManagerConfiguration(args);
        tmNick = _tmConfiguration.TmNick;
        tmUrl = _tmConfiguration.TmUrl;
        tmId = _tmConfiguration.TmId;
        tmServersIdMap = _tmConfiguration.ParseTmServers();
        tmNickMap = _tmConfiguration.TmNickMap;
        tmNicks = new List<string>();
        foreach (var server in tmNickMap)
        {
            tmNicks.Add(server.Key);
        }
        tmServers = new List<string>();
        foreach (var server in tmServersIdMap)
        {
            tmServers.Add(server.Value);
        }
        lmServersIdMap = _tmConfiguration.ParseLmServers();
        lmNickMap = _tmConfiguration.LmNickMap;
        lmNicks = new List<string>();
        foreach (var server in lmNickMap)
        {
            lmNicks.Add(server.Key);
        }
        lmServers = new List<string>();
        foreach (var server in lmServersIdMap)
        {
            lmServers.Add(server.Value);
        }
        slotBehavior = _tmConfiguration.ParseSlotBehavior();
        timeSlots = _tmConfiguration.TimeSlots;
        slotDuration = _tmConfiguration.SlotDuration;
        startTime = _tmConfiguration.StartTime;
    }

    public static void Main(string[] args)
    {
        try
        {
            Program program = new Program(args);
            program.StartProgram();
            WaitHandle.WaitOne();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"An error occurred: {ex.Message}");
        }
    }

    private void StartProgram()
    {
        try
        {

            TransactionManagerState tmState = new TransactionManagerState();

            PrintConfigurationDetails(tmNick, tmUrl, tmId, tmServers, lmServers, slotBehavior, timeSlots, slotDuration);

            Uri tmUri = new Uri(tmUrl);
            Console.WriteLine($"{tmUri.Host}-{tmUri.Port}");

            var transactionManagerLeaseService = new TransactionManagerLeaseService(lmServers);
            var transactionManagerPropagateService = new TransactionManagerPropagateService(tmNickMap, tmNick);

            LeaseManagerServiceImpl lmServiceImpl =
                new LeaseManagerServiceImpl(tmNick, tmState, lmServers.Count);
            
            LeaseHandler leaseHandler = new LeaseHandler();
            
            ClientRequestHandler clientRequestHandler = new ClientRequestHandler(tmState, leaseHandler);

            ClientTxServiceImpl clientTxServiceImpl =
                new ClientTxServiceImpl(tmNick, transactionManagerLeaseService, tmState, clientRequestHandler);

            Server server = ConfigureServer(tmNick, transactionManagerLeaseService, tmState, tmUri.Host, 
                tmUri.Port, lmServers.Count, lmServiceImpl, clientTxServiceImpl);

            server.Start();
            
            var currentTimeslot = 1;

            Console.WriteLine($"Starting Transaction Manager on port: {tmUri.Port}");

            // Create a new thread and start the printing
            Thread clientRequestHandlerThread = new Thread(clientRequestHandler.ProcessTransactions);
            clientRequestHandlerThread.Start();
            
            TimeSpan timeToStart = startTime - DateTime.Now;
            int msToWait = (int)timeToStart.TotalMilliseconds;
            Console.WriteLine($"Starting in {timeToStart} s");
            Thread.Sleep(msToWait);
            
            while(currentTimeslot <= timeSlots)
            {
                clientTxServiceImpl.isUpdating = true;
                clientRequestHandler.isUpdating = true;
                for (int i = 0; i < slotBehavior.Count(); i++)
                {
                    if (slotBehavior[i].Key[0] == currentTimeslot.ToString()[0])
                    {
                        UpdateSlotBehavior(slotBehavior[i], transactionManagerLeaseService, transactionManagerPropagateService);
                        break;
                    }
                }
                if (!_isRunning)
                {
                    Console.WriteLine($"Slot {currentTimeslot} crashed");
                    clientRequestHandler.isCrashed = true;
                    clientRequestHandler.canClose.WaitOne();
                    break;
                }
                clientTxServiceImpl.isUpdating = false;
                clientRequestHandler.isUpdating = false;
                Console.WriteLine($"Slot {currentTimeslot} started");
                Thread.Sleep(slotDuration);
                currentTimeslot++;
            }
            
            clientRequestHandlerThread.Interrupt();
            
            transactionManagerLeaseService.CloseLeaseManagerStubs();
            
            server.ShutdownAsync().Wait();
            
            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
            
            WaitHandle.Set();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"An error occurred: {ex.Message}");
        }
    }

    private static Server ConfigureServer(string tmNick, TransactionManagerLeaseService transactionManagerLeaseService,
        TransactionManagerState tmState, string tmHost, int tmPort, int numberOfLm, 
        LeaseManagerServiceImpl lmServiceImpl, ClientTxServiceImpl clientTxServiceImpl)
    {
        
        return new Server
        {
            Services =
            {
                ClientTransactionService.BindService(clientTxServiceImpl),
                ClientStatusService.BindService(new ClientStatusServiceImpl(tmNick)),
                LeaseResponseService.BindService(lmServiceImpl),
                TmService.BindService(new TransactionManagerPropagateServiceImpl())
            },
            Ports = { new ServerPort(tmHost, tmPort, ServerCredentials.Insecure) }
        };
    }
    private static void PrintConfigurationDetails(string tmNick, string tmUrl, int tmId, List<string> tmServers, List<string> lmServers,
        List<KeyValuePair<string, string>> slotBehavior, int timeSlots, int slotDuration)
    {
        Console.WriteLine("tmNick: " + tmNick);
        Console.WriteLine("tmUrl: " + tmUrl);
        Console.WriteLine("tmId: " + tmId);
        Console.WriteLine("TmServers: ");
        Console.WriteLine(string.Join(Environment.NewLine, tmServers));
        Console.WriteLine("LmServers: ");
        Console.WriteLine(string.Join(Environment.NewLine, lmServers));
        Console.WriteLine("slotBehavior: ");
        foreach (var slot in slotBehavior)
        {
            Console.WriteLine($"{slot.Key}-{slot.Value}");
        }
        Console.WriteLine("timeSlots: " + timeSlots);
        Console.WriteLine("slotDuration: " + slotDuration);
        Console.WriteLine("----------");
    }
    
    private void UpdateSlotBehavior(KeyValuePair<string, string> slot, TransactionManagerLeaseService transactionManagerLeaseService, 
                                    TransactionManagerPropagateService transactionManagerPropagateService)
    {
        var crashes = slot.Key;
        var suspects = slot.Value;
        transactionManagerPropagateService.ClearSuspectedList();
        transactionManagerPropagateService.CreateSuspectedList(suspects);
        string[] groups = crashes.Split('#');
        var tmCrashBehavior = groups[1];
        var lmCrashBehavior = groups[2];
        for(int i = 0; i < tmServers.Count + 1; i++)
        {
            if (tmCrashBehavior[i] == 'C')
            {
                if (i == tmId)
                {
                    _isRunning = false;
                }
                else
                {
                    RemoveCrashedTransactionServer(i, transactionManagerPropagateService);
                }
            }
        }
        for(int i = 0; i < lmServers.Count; i++)
        {
            if (lmCrashBehavior[i] == 'C')
            {
                RemoveCrashedLeaseServer(i, transactionManagerLeaseService);
            }
        }
        
        if (suspects.Count() != 0)
        {
            string[] suspectsGroups = suspects.Split('+');
            foreach (var suspect in suspectsGroups)
            {
                string[] suspectGroup = suspect.Split(',');
                string sourceServer = suspectGroup[0].Substring(1);
                string targetServer = suspectGroup[1].Substring(0, suspectGroup[1].Length - 1);
                if (sourceServer == tmNick && tmNicks.Contains(targetServer))
                {
                    Console.WriteLine($"Suspecting tm {targetServer}");
                    // someService.SuspectTransactionManager(tmNickMap[targetServer]);
                }
            }
        }

    }
    
    private void RemoveCrashedTransactionServer(int id, TransactionManagerPropagateService transactionManagerPropagateService)
    {
        var crashedServer = tmServersIdMap.Find(x => x.Key == id).Value;
        tmServers.Remove(crashedServer);
        tmServersIdMap.Remove(tmServersIdMap.Find(x => x.Key == id));
        var crashedServerNick = tmNickMap.FirstOrDefault(x => x.Value == crashedServer).Key;
        tmNickMap.Remove(crashedServerNick);
        tmNicks.Remove(crashedServerNick);
        transactionManagerPropagateService.RemoveTransactionManagerStub(crashedServer);

    }

    public void RemoveCrashedLeaseServer(int id, TransactionManagerLeaseService transactionManagerLeaseService)
    {
        var crashedServer = lmServersIdMap.Find(x => x.Key == id).Value;
        lmServers.Remove(crashedServer);
        lmServersIdMap.Remove(lmServersIdMap.Find(x => x.Key == id));
        var crashedServerNick = lmNickMap.FirstOrDefault(x => x.Value == crashedServer).Key;
        lmNickMap.Remove(crashedServerNick);
        lmNicks.Remove(crashedServerNick);
        transactionManagerLeaseService.RemoveLeaseManagerStub(crashedServer);
    }
}
