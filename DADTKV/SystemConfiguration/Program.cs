using System.Diagnostics;

class Program
{
    public static void Main(string[] args) {
        // edit configurations of the program SystemConfiguration and make sure the working dir is [...]DIDA-DADTKV/DADTKV/SystemConfiguration/
        // and add the file name in the 'program arguments'
        // IMPORTANT: the other projects have to be compiled (built)
        var scriptPath = args[0];
        
        if (!File.Exists(scriptPath))
        {
            Console.WriteLine("The specified script file does not exist.");
            return;
        }
        var script = File.ReadAllLines(scriptPath);

        var timeSlots = 0;
        var slotDuration = 0;
        DateTime startTime = DateTime.MinValue;
        Dictionary<string, ProcessStartInfo> processes = new Dictionary<string, ProcessStartInfo>();
        
        foreach (var line in script) {
            var parts = line.Split(' ');
            switch (parts[0]) {
                case "P":
                    if (parts[2] == "T") {
                        processes.Add(parts[1], new ProcessStartInfo
                        {
                            FileName = @"..\TransactionManager\bin\Debug\net7.0\TransactionManager.exe",
                            Arguments = parts[1] + " " + parts[3]
                        });
                    } else if (parts[2] == "L") {
                        processes.Add(parts[1], new ProcessStartInfo
                        {
                            FileName = @"..\LeaseManager\bin\Debug\net7.0\LeaseManager.exe",
                            Arguments = parts[1] + " " + parts[3]
                        });
                    } else if (parts[2] == "C") {
                        processes.Add(parts[1], new ProcessStartInfo
                        {
                            FileName = @"..\Client\bin\Debug\net7.0\Client.exe",
                            Arguments = parts[1] + " " + parts[3]
                        });
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
        foreach (var x in processes) {
            try {
                Process p = new Process
                {
                    StartInfo = x.Value
                };
                p.Start();
                
                Console.WriteLine("Process started successfully.");
            }
            catch (Exception e)
            {
                Console.WriteLine(e + "\n");
            }
        }
    }
}
