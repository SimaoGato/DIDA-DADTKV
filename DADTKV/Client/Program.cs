using System;
using System.Collections.Generic;
using Client;

class Program
{
    private static void Main(string[] args)
    {
        string clientNick = args[0];
        string scriptPath = @"..\..\..\" + args[1];
        int clientId = int.Parse(args[2]);
        int numberOfTm = int.Parse(args[3]);
        List<string> TmServers = new List<string>();
        for(int i = 0; i < numberOfTm; i++)
        {
            TmServers.Add(args[5 + i*2]);
        }
        string mainTmServer = TmServers[clientId % numberOfTm];
        
        Console.WriteLine("clientId: " + clientId);
        Console.WriteLine("mainTmServer: " + mainTmServer);
        Console.WriteLine(Directory.GetCurrentDirectory());
        Console.WriteLine("scriptPath: " + scriptPath);
        Console.WriteLine("TmServers: ");
        foreach (var tmServer in TmServers)
        {
            Console.WriteLine(tmServer);
        }

        var clientService = new ClientService(mainTmServer);
        
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
                    var objectsToWrite = new List<KeyValuePair<string, int>>();
                    // TODO add this logic to another class
                    var readEls = parts[1].Trim('(', ')').Split(',');
                    var writeEls = parts[2].Trim('(', ')').Split(',');
                    foreach (var el in readEls)
                    {
                        objectsToRead.Add(el.Trim());
                    }
                    //Console.WriteLine(writeEls[0]);
                    if (writeEls.Length > 1)
                        for (int i = 0; i < writeEls.Length; i+=2) {
                            var keyValuePair = new KeyValuePair<string, int>(writeEls[i].Trim('<', '>'),
                                int.Parse(writeEls[i + 1].Trim('<', '>')));
                            objectsToWrite.Add(keyValuePair);
                        }
                    Console.WriteLine("objectsToRead: ");
                    foreach (var element in objectsToRead)
                    {
                        Console.WriteLine(element);
                    }
                    Console.WriteLine("\nObjectsToWrite: ");
                    foreach (var element in objectsToWrite)
                    {
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
        string co = "";
        while (true)
        {
            co = Console.ReadLine();
            if (co == "q")
            {
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
