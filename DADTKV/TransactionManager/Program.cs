using Grpc.Core;
using TransactionManager;

class Program
{
    public static void Main(string[] args)
    {
        const string hostname = "localhost";
        const int port = 5000;

        Server server = new Server
        {
            Services = { DadtkvClientService.BindService(new ClientServiceImpl()) }, 
            Ports = { new ServerPort(hostname, port, ServerCredentials.Insecure) }
        };

        server.Start();

        Console.WriteLine("Starting Transaction Manager on port: {0}", port);
        Console.WriteLine("Press any key to stop...");
        Console.ReadKey();

        server.ShutdownAsync().Wait();
    }
}