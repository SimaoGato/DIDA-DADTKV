using Grpc.Core;
using LeaseManager;

class Program
{
    public static void Main(string[] args)
    {
        const string hostname = "localhost";
        const int port = 5001;

        Server server = new Server
        {
            Services = { LeaseService.BindService(new TransactionManagerServiceImpl()) }, 
            Ports = { new ServerPort(hostname, port, ServerCredentials.Insecure) }
        };

        server.Start();

        Console.WriteLine("Starting Lease Manager on port: {0}", port);
        Console.WriteLine("Press any key to stop...");
        Console.ReadKey();

        server.ShutdownAsync().Wait();
    }
}