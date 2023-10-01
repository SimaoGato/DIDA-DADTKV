using Grpc.Core;
using TransactionManager;

class Program
{
    public static void Main(string[] args) {

        TransactionManagerLogic tmLogic = new TransactionManagerLogic(args);
        
        string tmNick = tmLogic.tmNick;
        string tmUrl = tmLogic.tmUrl;
        List<string> tmServers = tmLogic.ParseTmServers();
        List<string> lmServers = tmLogic.ParseLmServers();
        var slotBehavior = tmLogic.ParseSlotBehavior();
        var timeSlots = tmLogic.timeSlots;
        var slotDuration = tmLogic.slotDuration;
        
        Console.WriteLine("tmNick: " + tmNick);
        Console.WriteLine("tmUrl: " + tmUrl);
        Console.WriteLine("TmServers: ");
        foreach (var tmServer in tmServers)
        {
            Console.WriteLine(tmServer);
        }
        Console.WriteLine("LmServers: ");
        foreach (var lmServer in lmServers)
        {
            Console.WriteLine(lmServer);
        }
        Console.WriteLine("slotBehavior: ");
        foreach (var slot in slotBehavior)
        {
            Console.WriteLine(slot.Key + "-" + slot.Value);
        }
        Console.WriteLine("timeSlots: " + timeSlots);
        Console.WriteLine("slotDuration: " + slotDuration);
        Console.WriteLine("----------");
        
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
