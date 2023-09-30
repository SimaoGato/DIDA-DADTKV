using Grpc.Core;
using LeaseManager;

class Program
{
    // TODO: Save leaseManagers adresses in a dictionary
    // TODO: receive from args the list of nicks and addresses (the first one should be this lease manager)
    public static void Main(string[] args)
    {   
        
        string lmNick = args[0];
        string lmUrl = args[1];
        int numberOfLm = int.Parse(args[2]);
        List<string> LmServers = new List<string>();
        for(int i = 0; i < numberOfLm; i++)
        {
            if (args[4 + i * 2] != lmUrl)
            {
                LmServers.Add(args[4 + i * 2]);
            }
        }
        int numberOfTm = int.Parse(args[3 + numberOfLm * 2]);
        List<string> TmServers = new List<string>();
        for(int i = 0; i < numberOfTm; i++)
        {
            TmServers.Add(args[5 + numberOfLm * 2 + i * 2]);
        }
        
        Console.WriteLine("tmNick: " + lmNick);
        Console.WriteLine("tmUrl: " + lmUrl);
        Console.WriteLine("LmServers: ");
        foreach (var lmServer in LmServers)
        {
            Console.WriteLine(lmServer);
        }
        Console.WriteLine("TmServers: ");
        foreach (var tmServer in TmServers)
        {
            Console.WriteLine(tmServer);
        }
        
        Uri lmUri = new Uri(lmUrl);
        Console.WriteLine(lmUri.Host + "-" + lmUri.Port);

        Server server = new Server
        {
            Services = { LeaseService.BindService(new TransactionManagerServiceImpl()) }, 
            Ports = { new ServerPort(lmUri.Host, lmUri.Port, ServerCredentials.Insecure) }
        };

        server.Start();

        Console.WriteLine("Starting Lease Manager on port: {0}", lmUri.Port);
        Console.WriteLine("Press any key to stop...");
        Console.ReadKey();

        server.ShutdownAsync().Wait();
    }
}