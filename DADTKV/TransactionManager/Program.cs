﻿using Grpc.Core;
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

            SharedContext sharedContext = new SharedContext();

            LeaseManagerServiceImpl lmServiceImpl =
                new LeaseManagerServiceImpl(tmNick, tmState, lmServers.Count, sharedContext);

            Server server = ConfigureServer(tmNick, transactionManagerService, tmState, tmUri.Host, 
                tmUri.Port, lmServers.Count, sharedContext, lmServiceImpl);

            server.Start();

            Console.WriteLine($"Starting Transaction Manager on port: {tmUri.Port}");
            
            // while dont reach the number of time slots
            // wait for a signal to execute transactions
            // when the signal is received, execute transactions
            // when transactions are executed, go back to waiting for signal
            var i = 0;
            var countLM = 0;
            while (i < timeSlots)
            {
                
                Console.WriteLine($"[TransactionManager] slot {i} - {tmNick} waiting for signal to start transactions");
                // while dont receive signal from majority of lease managers
                while (countLM < lmServers.Count)
                {
                    Console.WriteLine($"[TransactionManager] {tmNick} waiting for all lease managers to send leases. " +
                                      $"Has received {countLM} signals. Waiting for majority {(lmServers.Count / 2) + 1}");
                    sharedContext.LeaseSignal.WaitOne();
                    countLM++;
                }
                Console.WriteLine($"[TransactionManager] {tmNick} received enough signals ({countLM}) to start transactions");
                // arrumar leases numa fila
                var numOfTransactions = sharedContext.ExecuteTransactions(tmState.GetLeasesPerLeaseManager().First());
                Console.WriteLine($"[TransactionManager] {tmNick} finished {numOfTransactions} transactions");
                i++;
                countLM = 0;
                tmState.ClearLeasesPerLeaseManager();
                // print every object in data storage
                //tmState.PrintObjects();
            }
            
            Console.WriteLine("Press any key to stop...");
            Console.ReadKey();

            transactionManagerService.CloseLeaseManagerStubs();

            server.ShutdownAsync().Wait();
            WaitHandle.Set();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"An error occurred: {ex.Message}");
        }
    }

    private static Server ConfigureServer(string tmNick, TransactionManagerService transactionManagerService,
        TransactionManagerState tmState, string tmHost, int tmPort, int numberOfLm, 
        SharedContext sharedContext, LeaseManagerServiceImpl lmServiceImpl)
    {
        
        return new Server
        {
            Services =
            {
                ClientTransactionService.BindService(new ClientTxServiceImpl(tmNick, transactionManagerService, tmState, sharedContext)),
                ClientStatusService.BindService(new ClientStatusServiceImpl(tmNick)),
                LeaseResponseService.BindService(lmServiceImpl)
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
