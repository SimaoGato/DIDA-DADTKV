using Grpc.Core;
using TransactionManager;

class Program
{
    public static void Main(string[] args)
    {
        const string hostname = "localhost";
        const int port = 5000;
        
        string leaseServerHostname = "localhost";
        int leaseServerPort = 5001;
        
        var transactionManagerService = new TransactionManagerService(leaseServerHostname, leaseServerPort);
        
        Console.Write("Enter transaction manager ID: ");
        string transactionManagerId = Console.ReadLine();


        Server server = new Server
        {
            Services = { DadtkvClientService.BindService(new ClientServiceImpl(transactionManagerId, transactionManagerService)) }, 
            Ports = { new ServerPort(hostname, port, ServerCredentials.Insecure) }
        };

        server.Start();

        Console.WriteLine("Starting Transaction Manager on port: {0}", port);
        Console.WriteLine("Press any key to stop...");
        Console.ReadKey();

        server.ShutdownAsync().Wait();
    }
}