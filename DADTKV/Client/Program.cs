using System;
using System.Collections.Generic;
using Client;
using Timer = System.Timers.Timer;


class Program
{

    private ClientConfiguration _clientConfiguration;
    static ManualResetEvent waitHandle = new ManualResetEvent(false);

    private Program(string[] args)
    {
        _clientConfiguration = new ClientConfiguration(args);
    }

    private static void Main(string[] args)
    {
        Program program = new Program(args);
        TimeSpan timeToStart = program._clientConfiguration.StartTime - DateTime.Now;
        int msToWait = (int)timeToStart.TotalMilliseconds;
        Console.WriteLine($"Starting Client in {timeToStart} s");
        Timer slotTimer = new Timer(msToWait);
        slotTimer.Elapsed += (_, _) => program.StartProgram();
        slotTimer.AutoReset = false;
        slotTimer.Start();
        waitHandle.WaitOne();
    }

    private void StartProgram()
    {
        
        _clientConfiguration.PrintArgs();
        if (_clientConfiguration.NumberOfTm == 0)
        {
            Console.WriteLine("Error: There is no available Transaction Manager Server");
            Thread.Sleep(5000);
            waitHandle.Set();
            return;
        }

        ClientService clientService = new ClientService(_clientConfiguration.MainTmServer, _clientConfiguration.Servers, 
                                                        _clientConfiguration.TmServers);

        if (!File.Exists(_clientConfiguration.ScriptPath))
        {
            Console.WriteLine("The specified script file does not exist.");
            Thread.Sleep(5000);
            waitHandle.Set();
            return;
        }

        var script = File.ReadAllLines(_clientConfiguration.ScriptPath);
        foreach (var command in script)
        {
            var parts = command.Split(' ');
            switch (parts[0])
            {
                case "T":
                    var objectsToRead = _clientConfiguration.ParseObjectsToRead(parts);
                    var objectsToWrite = _clientConfiguration.ParseObjectsToWrite(parts);
                    _clientConfiguration.PrintTxObj(objectsToRead, objectsToWrite);
                    clientService.TxSubmit(_clientConfiguration.ClientNick, objectsToRead, objectsToWrite);
                    break;
                case "S":
                    clientService.Status();
                    break;
                case "W":
                    Thread.Sleep(int.Parse(parts[1]));
                    break;
            }
        }

        while (true)
        {
            var co = Console.ReadLine();
            if (co != "q") continue;
            clientService.CloseClientStubs();
            break;
        }
        waitHandle.Set();
    }
}