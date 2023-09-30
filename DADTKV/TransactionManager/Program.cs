using Grpc.Core;
using TransactionManager;

class Program
{
    public static void Main(string[] args) {
        
        string tmNick = args[0];
        string tmUrl = args[1];
        int numberOfTm = int.Parse(args[2]);
        List<string> TmServers = new List<string>();
        for(int i = 0; i < numberOfTm; i++)
        {
            if (args[4 + i * 2] != tmUrl)
            {
                TmServers.Add(args[4 + i * 2]);
            }
        }
        int numberOfLm = int.Parse(args[3 + numberOfTm * 2]);
        List<string> lmServers = new List<string>();
        for(int i = 0; i < numberOfLm; i++)
        {
            lmServers.Add(args[5 + numberOfLm * 2 + i * 2]);
        }
        
        Console.WriteLine("tmNick: " + tmNick);
        Console.WriteLine("tmUrl: " + tmUrl);
        Console.WriteLine("TmServers: ");
        foreach (var tmServer in TmServers)
        {
            Console.WriteLine(tmServer);
        }
        Console.WriteLine("LmServers: ");
        foreach (var lmServer in lmServers)
        {
            Console.WriteLine(lmServer);
        }
        
        Uri tmUri = new Uri(tmUrl);
        Console.WriteLine(tmUri.Host + "-" + tmUri.Port);
        
        // TODO add this logic to another class
        var transactionManagerService = new TransactionManagerService(lmServers);

        Server server = new Server
        {
            Services = { DadtkvClientService.BindService(new ClientServiceImpl(tmNick, transactionManagerService)) }, 
            Ports = { new ServerPort(tmUri.Host, tmUri.Port, ServerCredentials.Insecure) }
        };

        server.Start();

        Console.WriteLine("Starting Transaction Manager on port: {0}", tmUri.Port);
        Console.WriteLine("Press any key to stop...");
        Console.ReadKey();

        server.ShutdownAsync().Wait();
    }
}
