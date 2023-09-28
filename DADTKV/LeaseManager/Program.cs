using Grpc.Core;
using LeaseManager;

class Program
{
    public static void Main(string[] args)
    {   
        Uri lmUri = new Uri(args[1]);
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