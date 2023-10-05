using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

class Program
{
    static void Main(string[] args)
    {
        var scriptPath = GetScriptPath(args);
        if (!File.Exists(scriptPath))
        {
            Console.WriteLine("The specified script file does not exist.");
            return;
        }

        var script = File.ReadAllLines(scriptPath);
        var config = ParseScript(script);
        Console.WriteLine(Directory.GetCurrentDirectory());

        StartLeaseManagers(config.LeaseManagers, config.TransactionManagers, config.Clients, config.TimeSlots, config.SlotDuration, config.StartTime, config.SlotBehaviors);
        StartTransactionManagers(config.TransactionManagers, config.LeaseManagers, config.TimeSlots, config.SlotDuration, config.StartTime, config.SlotBehaviors);
        StartClients(config.Clients, config.TransactionManagers, config.LeaseManagers, config.StartTime);
    }

    static string GetScriptPath(string[] args)
    {
        if (args.Length == 0)
        {
            throw new ArgumentException("No script file specified.");
        }

        return Path.Combine("..", "..", "..", args[0]);
    }

    static Config ParseScript(string[] script)
    {
        var config = new Config();
        foreach (var line in script)
        {
            var parts = line.Split(' ');
            switch (parts[0])
            {
                case "P":
                    AddProcess(parts[1], parts[2], parts[3], config);
                    break;
                case "S":
                    config.TimeSlots = int.Parse(parts[1]);
                    break;
                case "D":
                    config.SlotDuration = int.Parse(parts[1]);
                    break;
                case "T":
                    DateTime currentDate = DateTime.Now;
                    DateTime startTime = currentDate.AddSeconds(5);
                    config.StartTime = startTime;
                    break;
                case "F":
                    AddSlotBehavior(parts, config);
                    break;
            }
        }

        return config;
    }

    static void AddProcess(string name, string type, string args, Config config)
    {
        switch (type)
        {
            case "T":
                config.TransactionManagers.Add(new ProcessInfo(name, args));
                break;
            case "L":
                config.LeaseManagers.Add(new ProcessInfo(name, args));
                break;
            case "C":
                config.Clients.Add(new ProcessInfo(name, args));
                break;
        }
    }

    static void AddSlotBehavior(string[] parts, Config config)
    {
        var slotBehavior = string.Join("", parts, 1, parts.Length - 1);
        config.SlotBehaviors.Add(slotBehavior);
    }

    static void StartLeaseManagers(List<ProcessInfo> leaseManagers, List<ProcessInfo> transactionManagers, List<ProcessInfo> clients, int timeSlots, int slotDuration, DateTime startTime, List<string> slotBehaviors)
    {
        var lmArgs = GetProcessArgs(leaseManagers);
        var tmArgs = GetProcessArgs(transactionManagers);

        for (int i = 0; i < leaseManagers.Count; i++)
        {
            var lm = leaseManagers[i];
            var lmId = i;
            var numLMs = leaseManagers.Count;
            var numTMs = transactionManagers.Count;

            var arguments = $"{lm.Key} {lm.Value} {lmId} {numLMs} {lmArgs} {numTMs} {tmArgs} - {timeSlots} {slotDuration} {slotBehaviors.Count} {string.Join(" ", slotBehaviors)} {startTime:HH:mm:ss}";

            StartProcess("LeaseManager", arguments);
        }
    }

    static void StartTransactionManagers(List<ProcessInfo> transactionManagers, List<ProcessInfo> leaseManagers, int timeSlots, int slotDuration, DateTime startTime, List<string> slotBehaviors)
    {
        var tmArgs = GetProcessArgs(transactionManagers);
        var lmArgs = GetProcessArgs(leaseManagers);

        for (int i = 0; i < transactionManagers.Count; i++)
        {
            var tm = transactionManagers[i];
            var numTMs = transactionManagers.Count;
            var numLMs = leaseManagers.Count;

            var arguments = $"{tm.Key} {tm.Value} {numTMs} {tmArgs} {numLMs} {lmArgs} - {timeSlots} {slotDuration} {slotBehaviors.Count} {string.Join(" ", slotBehaviors)} {startTime:HH:mm:ss}";

            StartProcess("TransactionManager", arguments);
        }
    }

    static void StartClients(List<ProcessInfo> clients, List<ProcessInfo> transactionManagers, List<ProcessInfo> leaseManagers, DateTime startTime)
    {
        var tmArgs = GetProcessArgs(transactionManagers);
        var lmArgs = GetProcessArgs(leaseManagers);

        for (int i = 0; i < clients.Count; i++)
        {
            var c = clients[i];
            var numCs = i;
            var numTMs = transactionManagers.Count;
            var numLMs = leaseManagers.Count;

            var arguments = $"{c.Key} {c.Value} {numCs} {numTMs} {tmArgs} {numLMs} {lmArgs} {startTime:HH:mm:ss}";

            StartProcess("Client", arguments);
        }
    }

    static string GetProcessArgs(List<ProcessInfo> processes)
    {
        return string.Join(" ", processes.Select(p => $"{p.Key} {p.Value}"));
    }

    static void StartProcess(string fileName, string arguments)
    {
        try
        {
            var process = new Process()
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
            
            var args = "start " + @"..\..\..\..\" + fileName + @"\bin\Debug\net7.0\" + fileName + ".exe " + arguments;

            process.Start();
            process.StandardInput.WriteLine(args);
            process.StandardInput.WriteLine("exit");

            Console.WriteLine($"{args}");
        }
        catch (Exception e)
        {
            Console.WriteLine($"Failed to start {fileName} process: {e}");
        }
    }
}

class Config
{
    public List<ProcessInfo> LeaseManagers { get; } = new List<ProcessInfo>();
    public List<ProcessInfo> TransactionManagers { get; } = new List<ProcessInfo>();
    public List<ProcessInfo> Clients { get; } = new List<ProcessInfo>();
    public int TimeSlots { get; set; }
    public int SlotDuration { get; set; }
    public DateTime StartTime { get; set; }
    public List<string> SlotBehaviors { get; } = new List<string>();
}

class ProcessInfo
{
    public string Key { get; }
    public string Value { get; }

    public ProcessInfo(string key, string value)
    {
        Key = key;
        Value = value;
    }
}