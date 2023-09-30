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
        Dictionary<string, string> TMProcesses = new Dictionary<string, string>();
        Dictionary<string, string> LMProcesses = new Dictionary<string, string>();
        Dictionary<string, string> CProcesses = new Dictionary<string, string>();

        foreach (var line in script)
        {
            var parts = line.Split(' ');
            switch (parts[0])
            {
                case "P":
                    if (parts[2] == "T")
                    {
                        //processes.Add(parts[1], @"..\..\..\..\TransactionManager\bin\Debug\net7.0\TransactionManager.exe " + parts[1] + " " + parts[3]);
                        TMProcesses.Add(parts[1], parts[3]); 
                    }
                    else if (parts[2] == "L")
                    {
                        //processes.Add(parts[1], @"..\..\..\..\LeaseManager\bin\Debug\net7.0\LeaseManager.exe " + parts[1] + " " + parts[3]);
                        LMProcesses.Add(parts[1], parts[3]);
                    }
                    else if (parts[2] == "C")
                    {
                        //processes.Add(parts[1], @"..\..\..\..\Client\bin\Debug\net7.0\Client.exe " + parts[1] + " " + parts[3]);
                        CProcesses.Add(parts[1], parts[3]);
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

        string tmArgs = "";
        int numTMs = 0;
        
        foreach (var tm in TMProcesses)
        {
            numTMs++;
            tmArgs += tm.Key + " " + tm.Value + " ";
        }
        
        Console.WriteLine("tmArgs: " + tmArgs);
        
        string lmArgs = "";
        int numLMs = 0;
        
        foreach (var lm in LMProcesses)
        {
            numLMs++;
            lmArgs += lm.Key + " " + lm.Value + " ";
        }
        
        foreach (var lm in LMProcesses)
        {
            try
            {
                Process p = new Process()
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
                Console.WriteLine("c.value: " + lm.Value); // key=nick, value=address
                
                Console.WriteLine("start "
                                  + @"..\..\..\..\LeaseManager\bin\Debug\net7.0\LeaseManager.exe " 
                                  + lm.Key + " " + lm.Value + " " + numLMs + " " + lmArgs + " " + numTMs + " " + tmArgs);
                
                p.Start();
                // Send the command to open a new terminal window and run the process
                p.StandardInput.WriteLine("start "
                                          + @"..\..\..\..\LeaseManager\bin\Debug\net7.0\LeaseManager.exe " 
                                          + lm.Key + " " + lm.Value + " " + numLMs + " " + lmArgs + " " + numTMs + " " + tmArgs);
                p.StandardInput.WriteLine("exit");

                Console.WriteLine("Process started successfully.");
            }
            catch (Exception e)
            {
                Console.WriteLine(e + "\n");
            }
        }
        
        foreach (var tm in TMProcesses)
        {
            try
            {
                Process p = new Process()
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
                Console.WriteLine("c.value: " + tm.Value); // key=nick, value=address
                
                Console.WriteLine("start "
                                  + @"..\..\..\..\TransactionManager\bin\Debug\net7.0\TransactionManager.exe " 
                                  + tm.Key + " " + tm.Value + " " + numTMs + " " + tmArgs + " " + numLMs + " " + lmArgs);
                
                p.Start();
                // Send the command to open a new terminal window and run the process
                p.StandardInput.WriteLine("start "
                                          + @"..\..\..\..\TransactionManager\bin\Debug\net7.0\TransactionManager.exe " 
                                          + tm.Key + " " + tm.Value + " " + numTMs + " " + tmArgs + " " + numLMs + " " + lmArgs);
                p.StandardInput.WriteLine("exit");

                Console.WriteLine("Process started successfully.");
            }
            catch (Exception e)
            {
                Console.WriteLine(e + "\n");
            }
        }

        int numCs = 0;
        foreach (var c in CProcesses)
        {
            try
            {
                Process p = new Process()
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
                Console.WriteLine("c.value: " + c.Value); // key=nick, value=address
                
                Console.WriteLine("start "
                                  + @"..\..\..\..\Client\bin\Debug\net7.0\Client.exe " 
                                  + c.Key + " " + c.Value + " " + numCs + " " + numTMs + " " + tmArgs);
                
                p.Start();
                // Send the command to open a new terminal window and run the process
                p.StandardInput.WriteLine("start "
                                          + @"..\..\..\..\Client\bin\Debug\net7.0\Client.exe " 
                                          + c.Key + " " + c.Value + " " + numCs + " " + numTMs + " " + tmArgs);
                p.StandardInput.WriteLine("exit");

                Console.WriteLine("Process started successfully.");
                numCs++;
            }
            catch (Exception e)
            {
                Console.WriteLine(e + "\n");
            }
        }
    }
}