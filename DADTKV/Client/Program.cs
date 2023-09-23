using System;
using System.Collections.Generic;
using Client;

class Program
{
    static private void Main()
    {
        string serverHostname = "localhost";
        int serverPort = 5000;

        var clientService = new ClientService(serverHostname, serverPort);

        Console.Write("Enter client ID: ");
        string clientId = Console.ReadLine();

        bool exit = false;

        while (!exit)
        {
            Console.WriteLine("Choose an option:");
            Console.WriteLine("1. Submit a transaction");
            Console.WriteLine("2. Check status");
            Console.WriteLine("3. Exit");

            string choice = Console.ReadLine();

            switch (choice)
            {
                case "1":
                    Console.WriteLine("Enter objects to read (comma-separated): ");
                    List<string> objectsToRead = new List<string>(Console.ReadLine().Split(','));

                    Console.WriteLine("Enter objects to write (key:value,key:value): ");
                    List<Dictionary<string, int>> objectsToWrite = new List<Dictionary<string, int>>();
                    string[] writeInputs = Console.ReadLine().Split(',');

                    foreach (string input in writeInputs)
                    {
                        string[] keyValue = input.Split(':');
                        if (keyValue.Length == 2)
                        {
                            string key = keyValue[0];
                            int value;
                            if (int.TryParse(keyValue[1], out value))
                            {
                                objectsToWrite.Add(new Dictionary<string, int> { { key, value } });
                            }
                        }
                    }

                    clientService.TxSubmit(clientId, objectsToRead, objectsToWrite);
                    break;

                case "2":
                    clientService.Status();
                    break;

                case "3":
                    exit = true;
                    break;

                default:
                    Console.WriteLine("Invalid choice. Please enter a valid option.");
                    break;
            }
        }

        Console.WriteLine("Exiting the client application.");
    }
}
