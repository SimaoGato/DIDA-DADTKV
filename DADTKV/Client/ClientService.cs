using Grpc.Core;
using Grpc.Net.Client;

namespace Client;

public class ClientService
{
    
    private readonly ClientTransactionService.ClientTransactionServiceClient _clientTxStub;
    private readonly List<ClientStatusService.ClientStatusServiceClient> _clientStatusStubs;
    
    public ClientService(string mainTmAddress, List<string> tmServers, List<string> lmServers)
    {
        var txChannel = GrpcChannel.ForAddress(mainTmAddress);
        _clientTxStub = new ClientTransactionService.ClientTransactionServiceClient(txChannel);
        _clientStatusStubs = new List<ClientStatusService.ClientStatusServiceClient>();
        foreach (var server in tmServers.Concat(lmServers).ToList())
        {
            var channel = GrpcChannel.ForAddress(server);
            _clientStatusStubs.Add(new ClientStatusService.ClientStatusServiceClient(channel));
        }
    }

    public async void TxSubmit(string clientId, List<string> objectsToRead, List<KeyValuePair<string, int>> objectsToWrite)
    {
        var request = new TransactionRequest
        {
            ClientId = clientId,
            ObjectsToRead = { objectsToRead }
        };
        
        var dadIntList = new List<DadInt>();
        
        foreach (var dadIntObject in objectsToWrite)
        {
            var dadInt = new DadInt
            {
                Key = dadIntObject.Key,
                Value = dadIntObject.Value
            };
            
            dadIntList.Add(dadInt);
            
        }

        request.ObjectsToWrite.AddRange(dadIntList);
        
        // TODO AWAIT RESPONSE
        var response = await _clientTxStub.TxSubmitAsync(request);

        foreach (var dadInt in response.ObjectsRead)
        {
            Console.WriteLine("Read object: {0} with value {1}", dadInt.Key, dadInt.Value);
        }
        Console.WriteLine("Response: {0}", response);
    }

    public async void Status()
    {
        var request = new StatusRequest();
        foreach (var stub in _clientStatusStubs)
        {
            try
            {
                var response = await stub.StatusAsync(request, deadline: DateTime.UtcNow.AddSeconds(5));
                Console.WriteLine($"Status of {response.Nick}: {response.Status}");
            }
            catch (RpcException ex)
            {
                //TODO get name of server that is unavailable
                Console.WriteLine("server unavailable");
                Thread.Sleep(0);
            }
        }
    }
}