using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

class Program
{
    public static void Main(string[] args)
    {
        // Edit configurations of the program SystemConfiguration and make sure the working dir is [...]DIDA-DADTKV/DADTKV/SystemConfiguration/
        // and add the file name in the 'program arguments'
        // IMPORTANT: The other projects have to be compiled (built)
        var scriptPath = @"..\..\..\"+ args[0];
        Console.WriteLine(scriptPath);

        if (!File.Exists(scriptPath))
        {
            Console.WriteLine("The specified script file does not exist.");
            return;
        }
        var script = File.ReadAllLines(scriptPath);

        var timeSlots = 0;
        var slotDuration = 0;
        DateTime startTime = DateTime.MinValue;
        Dictionary<string, string> processes = new Dictionary<string, string>();

        foreach (var line in script)
        {
            var parts = line.Split(' ');
            switch (parts[0])
            {
                case "P":
                    if (parts[2] == "T")
                    {
                        processes.Add(parts[1], @"..\..\..\..\TransactionManager\bin\Debug\net7.0\TransactionManager.exe " + parts[1] + " " + parts[3]);
                    }
                    else if (parts[2] == "L")
                    {
                        processes.Add(parts[1], @"..\..\..\..\LeaseManager\bin\Debug\net7.0\LeaseManager.exe " + parts[1] + " " + parts[3]);
                    }
                    else if (parts[2] == "C")
                    {
                        processes.Add(parts[1], @"..\..\..\..\Client\bin\Debug\net7.0\Client.exe " + parts[1] + " " + parts[3]);
                    }
                    Console.WriteLine("Creating ProcessInfo");
                    break;
                case "S":
                    timeSlots = int.Parse(parts[1]);
                    Console.WriteLine($"How many time slots: {timeSlots}");
                    break;
                case "D":
                    slotDuration = int.Parse(parts[1]);
                    Console.WriteLine($"Duration of each time slot (ms): {slotDuration}");
                    break;
                case "T":
                    startTime = DateTime.ParseExact(parts[1], "HH:mm:ss", null);
                    Console.WriteLine($"Physical wall time: {startTime}");
                    break;
                case "F":
                    Console.WriteLine("Slot behavior");
                    break;
            }
        }
        foreach (var x in processes)
        {
            try
            {
                Process p = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "cmd.exe",
                        RedirectStandardInput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true,
                    }
                };
                Console.WriteLine("x.value: " + x.Value);
                
                p.Start();
                // Send the command to open a new terminal window and run the process
                p.StandardInput.WriteLine("start " + x.Value);
                p.StandardInput.WriteLine("exit");

                Console.WriteLine("Process started successfully.");
            }
            catch (Exception e)
            {
                Console.WriteLine(e + "\n");
            }
        }
    }
}