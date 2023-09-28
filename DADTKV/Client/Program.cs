using System;
using System.Collections.Generic;
using Client;

class Program
{
    private static void Main(string[] args)
    {
        string serverHostname = "localhost";
        int serverPort = 5000;

        var clientService = new ClientService(serverHostname, serverPort);

        string clientId = args[0];
        Console.WriteLine("clientId: " + clientId);
        
        var scriptPath = @"..\..\..\" + args[1];
        Console.WriteLine(Directory.GetCurrentDirectory());
        Console.WriteLine(scriptPath);
        if (!File.Exists(scriptPath))
        {
            Console.WriteLine("The specified script file does not exist.");
            Thread.Sleep(5000);
            return;
        }
        var script = File.ReadAllLines(scriptPath);
        foreach (var command in script) {
            var parts = command.Split(' '); 
            switch (parts[0]) {
                case "T":
                    var objectsToRead = new List<string>();
                    var objectsToWrite = new Dictionary<string, int>();
                    // TODO add this logic to another class
                    var readEls = parts[1].Trim('(', ')').Split(',');
                    var writeEls = parts[2].Trim('(', ')').Split(',');
                    Console.WriteLine(writeEls[1]);
                    foreach (var el in readEls)
                    {
                        objectsToRead.Add(el.Trim());
                    }
                    for (int i = 0; i < writeEls.Length; i+=2) {
                        objectsToWrite.Add(writeEls[i].Trim('<', '>'), int.Parse(writeEls[i+1].Trim('<', '>')));
                    }
                    foreach (var element in objectsToRead)
                    {
                        Console.WriteLine(element);
                    }
                    foreach (var element in objectsToWrite)
                    {
                        Console.WriteLine(element.Key + " " + element.Value);
                    }
                    // TODO this is still not working
                    clientService.TxSubmit(clientId, objectsToRead, objectsToWrite);
                    break;
                case "S":
                    clientService.Status();
                    break;
                case "W":
                    Thread.Sleep(int.Parse(parts[1]));
                    break;
            }
        }
        
        // bool exit = false;
        //
        // while (!exit)
        // {
        //     Console.WriteLine("Choose an option:");
        //     Console.WriteLine("1. Submit a transaction");
        //     Console.WriteLine("2. Check status");
        //     Console.WriteLine("3. Exit");
        //
        //     string choice = Console.ReadLine();
        //
        //     switch (choice)
        //     {
        //         case "1":
        //             Console.WriteLine("Enter objects to read (comma-separated): ");
        //             List<string> objectsToRead = new List<string>(Console.ReadLine().Split(','));
        //
        //             Console.WriteLine("Enter objects to write (key:value,key:value): ");
        //             List<Dictionary<string, int>> objectsToWrite = new List<Dictionary<string, int>>();
        //             string[] writeInputs = Console.ReadLine().Split(',');
        //
        //             foreach (string input in writeInputs)
        //             {
        //                 string[] keyValue = input.Split(':');
        //                 if (keyValue.Length == 2)
        //                 {
        //                     string key = keyValue[0];
        //                     int value;
        //                     if (int.TryParse(keyValue[1], out value))
        //                     {
        //                         objectsToWrite.Add(new Dictionary<string, int> { { key, value } });
        //                     }
        //                 }
        //             }
        //
        //             clientService.TxSubmit(clientId, objectsToRead, objectsToWrite);
        //             break;
        //
        //         case "2":
        //             clientService.Status();
        //             break;
        //
        //         case "3":
        //             exit = true;
        //             break;
        //
        //         default:
        //             Console.WriteLine("Invalid choice. Please enter a valid option.");
        //             break;
        //     }
        // }
        //
        // Console.WriteLine("Exiting the client application.");
    }
}
