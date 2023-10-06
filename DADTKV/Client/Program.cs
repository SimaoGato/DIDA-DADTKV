using System;
using System.Collections.Generic;
using Client;
using Timer = System.Timers.Timer;


class Program
{

    private ClientLogic clientLogic;
    static ManualResetEvent waitHandle = new ManualResetEvent(false);

    private Program(string[] args)
    {
        clientLogic = new ClientLogic(args);
    }

    private static void Main(string[] args)
    {
        Program program = new Program(args);
        TimeSpan timeToStart = program.clientLogic.StartTime - DateTime.Now;
        int msToWait = (int)timeToStart.TotalMilliseconds;
        Console.WriteLine($"Starting Client in {timeToStart} s");
        Timer slotTimer = new Timer(msToWait);
        slotTimer.Elapsed += (_, _) => program.StartProgram();
        slotTimer.AutoReset = false;
        slotTimer.Start();
        waitHandle.WaitOne();
    }

    private void StartProgram()
        // TODO error handling
    {
        try
        {
            clientLogic.PrintArgs();
            if (clientLogic.NumberOfTm == 0)
            {
                Console.WriteLine("Error: There is no available Transaction Manager Server");
                Thread.Sleep(5000);
                waitHandle.Set();
                return;
            }

            ClientService clientService = new ClientService(clientLogic.MainTmServer, clientLogic.Servers);

            if (!File.Exists(clientLogic.ScriptPath))
            {
                Console.WriteLine("The specified script file does not exist.");
                Thread.Sleep(5000);
                waitHandle.Set();
                return;
            }

            var script = File.ReadAllLines(clientLogic.ScriptPath);
            foreach (var command in script)
            {
                var parts = command.Split(' ');
                switch (parts[0])
                {
                    case "T":
                        var objectsToRead = clientLogic.ParseObjectsToRead(parts);
                        var objectsToWrite = clientLogic.ParseObjectsToWrite(parts);
                        clientLogic.PrintTxObj(objectsToRead, objectsToWrite);
                        clientService.TxSubmit(clientLogic.ClientNick, objectsToRead, objectsToWrite);
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
                if (co == "q")
                {
                    clientService.CloseClientStubs();
                    break;
                }
            }
            waitHandle.Set();
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            Thread.Sleep(10000);
            waitHandle.Set();
        }
    }
}