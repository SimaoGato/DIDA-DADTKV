using Grpc.Core;
using TransactionManager;

class Program
{
    public static void Main(string[] args) {
        
        Uri tmUri = new Uri(args[1]);
        Console.WriteLine(tmUri.Host + "-" + tmUri.Port);

        
        string leaseServerHostname = "localhost";
        int leaseServerPort = 5001;
        
        var transactionManagerService = new TransactionManagerService(leaseServerHostname, leaseServerPort);
        
        Console.WriteLine("tm id: " + args[0]);
        string transactionManagerId = args[0];


        Server server = new Server
        {
            Services = { DadtkvClientService.BindService(new ClientServiceImpl(transactionManagerId, transactionManagerService)) }, 
            Ports = { new ServerPort(tmUri.Host, tmUri.Port, ServerCredentials.Insecure) }
        };

        server.Start();

        Console.WriteLine("Starting Transaction Manager on port: {0}", tmUri.Port);
        Console.WriteLine("Press any key to stop...");
        Console.ReadKey();

        server.ShutdownAsync().Wait();
    }
}
