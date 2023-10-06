using Grpc.Core;
using Grpc.Net.Client;

namespace Client;

public class ClientService
{
    
    private readonly ClientTransactionService.ClientTransactionServiceClient _clientTxStub;
    private readonly Dictionary<string, ClientStatusService.ClientStatusServiceClient> _clientStatusStubs;
    private readonly List<GrpcChannel> _grpcChannels;
    
    public ClientService(string mainTmAddress, Dictionary<string, string> servers)
    {
        var txChannel = GrpcChannel.ForAddress(mainTmAddress);
        _grpcChannels = new List<GrpcChannel> { txChannel };
        _clientTxStub = new ClientTransactionService.ClientTransactionServiceClient(txChannel);
        _clientStatusStubs = new Dictionary<string, ClientStatusService.ClientStatusServiceClient>();
        foreach (var server in servers)
        {
            var channel = GrpcChannel.ForAddress(server.Value);
            _grpcChannels.Add(channel);
            _clientStatusStubs.Add(server.Key, new ClientStatusService.ClientStatusServiceClient(channel));
        }
    }

    public void CloseClientStubs()
    {
        foreach (var ch in _grpcChannels)
        {
            ch.ShutdownAsync().Wait();
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
        
        // TODO AWAIT RESPONSE and error handling
        TransactionResponse response = new TransactionResponse();
        try
        {
            response = await _clientTxStub.TxSubmitAsync(request);
        }
        catch (RpcException ex)
        {
            Console.WriteLine($"tm server for transaction unavailable");
        }
        
        
        foreach (var dadInt in response.ObjectsRead)
        {
            Console.WriteLine("Read object: {0} with value {1}", dadInt.Key, dadInt.Value);
        }
        Console.WriteLine("Response: {0}", response);
    }

    public async void Status()
    {
        var request = new StatusRequest();
        Console.WriteLine();
        foreach (var stub in _clientStatusStubs)
        {
            try
            {
                var response = await stub.Value.StatusAsync(request);
                Console.WriteLine($"Status of {stub.Key}: {response.Status}");
            }
            catch (RpcException ex)
            {
                //TODO get name of server that is unavailable
                Console.WriteLine($"Status of {stub.Key}: unavailable");
                Thread.Sleep(0);
            }
        }
    }
}