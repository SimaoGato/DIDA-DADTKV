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

    private Program(string[] args)
    {
        _tmConfiguration = new TransactionManagerConfiguration(args);
    }

    public static void Main(string[] args)
    {
        Program program = new Program(args);
        program.StartProgram();
        WaitHandle.WaitOne();
    }

    private void StartProgram()
    {
        string tmNick = _tmConfiguration.TmNick;
        string tmUrl = _tmConfiguration.TmUrl;
        List<string> tmServers = _tmConfiguration.ParseTmServers();
        List<string> lmServers = _tmConfiguration.ParseLmServers();
        var slotBehavior = _tmConfiguration.ParseSlotBehavior();
        var timeSlots = _tmConfiguration.TimeSlots;
        var slotDuration = _tmConfiguration.SlotDuration;

        TransactionManagerState tmState = new TransactionManagerState();
        
        PrintConfigurationDetails(tmNick, tmUrl, tmServers, lmServers, slotBehavior, timeSlots, slotDuration);

        Uri tmUri = new Uri(tmUrl);
        Console.WriteLine($"{tmUri.Host}-{tmUri.Port}");
        
        var transactionManagerService = new TransactionManagerService(lmServers);

        Server server = ConfigureServer(tmNick, transactionManagerService, tmState, tmUri.Host, tmUri.Port, lmServers.Count);

        server.Start();

        Console.WriteLine($"Starting Transaction Manager on port: {tmUri.Port}");
        Console.WriteLine("Press any key to stop...");
        Console.ReadKey();

        server.ShutdownAsync().Wait();
        WaitHandle.Set();
    }
    
    private static Server ConfigureServer(string tmNick, TransactionManagerService transactionManagerService, 
        TransactionManagerState tmState, string tmHost, int tmPort, int numberOfLm)
    {
        return new Server
        {
            Services =
            {
                ClientTransactionService.BindService(new ClientTxServiceImpl(tmNick, transactionManagerService, tmState)),
                ClientStatusService.BindService(new ClientStatusServiceImpl(tmNick)),
                LeaseResponseService.BindService(new LeaseManagerServiceImpl(tmState, numberOfLm))
            },
            Ports = { new ServerPort(tmHost, tmPort, ServerCredentials.Insecure) }
        };
    }
    
    private static void PrintConfigurationDetails(string tmNick, string tmUrl, List<string> tmServers, List<string> lmServers, 
        List<KeyValuePair<string, string>> slotBehavior, int timeSlots, int slotDuration)
    {
        Console.WriteLine("tmNick: " + tmNick);
        Console.WriteLine("tmUrl: " + tmUrl);
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
}
