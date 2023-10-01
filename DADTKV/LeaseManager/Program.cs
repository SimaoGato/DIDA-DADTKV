using Grpc.Core;
using LeaseManager;

class Program
{
    // TODO: Save leaseManagers adresses in a dictionary
    // TODO: receive from args the list of nicks and addresses (the first one should be this lease manager)
    public static void Main(string[] args) {
        LeaseManagerLogic lmLogic = new LeaseManagerLogic(args);
        
        string lmNick = lmLogic.lmNick;
        string lmUrl = lmLogic.lmUrl;
        List<string> lmServers = lmLogic.ParseLmServers();
        List<string> tmServers = lmLogic.ParseTmServers();
        
        Console.WriteLine("tmNick: " + lmNick);
        Console.WriteLine("tmUrl: " + lmUrl);
        Console.WriteLine("LmServers: ");
        foreach (var lmServer in lmServers)
        {
            Console.WriteLine(lmServer);
        }
        Console.WriteLine("TmServers: ");
        foreach (var tmServer in tmServers)
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