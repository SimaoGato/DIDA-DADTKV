﻿using System;
using System.Collections.Generic;
using Client;
using Timer = System.Timers.Timer;


class Program {
    
    private ClientLogic clientLogic;
    static ManualResetEvent waitHandle = new ManualResetEvent(false);
    
    private Program(string[] args) 
    {
        clientLogic = new ClientLogic(args);
    }

    private static void Main(string[] args) 
    {
        Program program = new Program(args);
        TimeSpan timeToStart = program.clientLogic.startTime - DateTime.Now;
        int msToWait = (int)timeToStart.TotalMilliseconds;
        Console.WriteLine($"Starting in {timeToStart} s");
        Timer slotTimer = new Timer(msToWait);
        slotTimer.Elapsed += (_, _) => program.StartProgram();
        slotTimer.AutoReset = false;
        slotTimer.Start();
        waitHandle.WaitOne();
    }

    private void StartProgram()
    // TODO error handling
    {
        try {
            string clientNick = clientLogic.clientNick;
            string scriptPath = clientLogic.scriptPath;
            int clientId = clientLogic.clientId;
            int numberOfTm = clientLogic.numberOfTm;
            List<string> tmServers = clientLogic.ParseTmServers();
            string mainTmServer = tmServers[clientId % numberOfTm];

            Console.WriteLine("clientId: " + clientId);
            Console.WriteLine("mainTmServer: " + mainTmServer);
            Console.WriteLine(Directory.GetCurrentDirectory());
            Console.WriteLine("scriptPath: " + scriptPath);
            Console.WriteLine("TmServers: ");
            foreach (var tmServer in tmServers) {
                Console.WriteLine(tmServer);
            }

            ClientService clientService = new ClientService(mainTmServer);

            if (!File.Exists(scriptPath)) {
                Console.WriteLine("The specified script file does not exist.");
                Thread.Sleep(5000);
                waitHandle.Set();
                return;
            }

            var script = File.ReadAllLines(scriptPath);
            foreach (var command in script) {
                var parts = command.Split(' ');
                switch (parts[0]) {
                    case "T":
                        var objectsToRead = clientLogic.ParseObjectsToRead(parts);
                        var objectsToWrite = clientLogic.ParseObjectsToWrite(parts);

                        Console.WriteLine("objectsToRead: ");
                        foreach (var element in objectsToRead) {
                            Console.WriteLine(element);
                        }
                        Console.WriteLine("\nObjectsToWrite: ");
                        foreach (var element in objectsToWrite) {
                            Console.WriteLine(element.Key + "-" + element.Value);
                        }
                        Console.WriteLine();
                        // TODO this is still not working
                        clientService.TxSubmit(clientNick, objectsToRead, objectsToWrite);
                        break;
                    case "S":
                        //clientService.Status();
                        break;
                    case "W":
                        Thread.Sleep(int.Parse(parts[1]));
                        break;
                }
            }

            while (true) {
                var co = Console.ReadLine();
                if (co == "q") {
                    break;
                }
            }
            waitHandle.Set();
        }
        catch (Exception e) {
            Console.WriteLine(e);
            Thread.Sleep(5000);
            waitHandle.Set();
        }
    }
}
