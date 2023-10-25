using Grpc.Core;
using Grpc.Net.Client;

namespace Client;

public class ClientService
{
    
    private ClientTransactionService.ClientTransactionServiceClient _clientTxStub;
    private readonly string _mainTmAddress;
    private readonly List<string> _tmServers;
    private readonly Dictionary<ClientStatusService.ClientStatusServiceClient, string> _clientServerStubs;
    private readonly List<GrpcChannel> _grpcChannels;
    
    public ClientService(string mainTmAddress, Dictionary<string, string> servers, List<string> tmServers)
    {
        var txChannel = GrpcChannel.ForAddress(mainTmAddress);
        _mainTmAddress = mainTmAddress;
        _grpcChannels = new List<GrpcChannel> { txChannel };
        _clientTxStub = new ClientTransactionService.ClientTransactionServiceClient(txChannel);
        _clientServerStubs = new Dictionary<ClientStatusService.ClientStatusServiceClient, string>();
        foreach (var server in servers)
        {
            var channel = GrpcChannel.ForAddress(server.Value);
            _grpcChannels.Add(channel);
            _clientServerStubs.Add(new ClientStatusService.ClientStatusServiceClient(channel), server.Key);
        }
        _tmServers = tmServers;
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

        var currentTmAddress = _mainTmAddress;
        TransactionResponse response = new TransactionResponse();
        try
        {
            response = await _clientTxStub.TxSubmitAsync(request);
            while (response.ObjectsRead.Count > 0 && response.ObjectsRead[0].Key == "abort") // send request again
            {
                Thread.Sleep(1000); //TODO HOW MUCH TIME TO WAIT
                if (response.ObjectsRead[0].Value == 1)
                {
                    Console.WriteLine("[ClientService] Resubmitting transaction to the tm server: " + currentTmAddress);
                    response = await _clientTxStub.TxSubmitAsync(request);
                }
                else
                {
                    currentTmAddress = _tmServers[(_tmServers.IndexOf(currentTmAddress) + 1) % _tmServers.Count];
                    Console.WriteLine("[ClientService] Submitting transaction to another tm server: " + currentTmAddress);
                    var txChannel = GrpcChannel.ForAddress(currentTmAddress);
                    _grpcChannels.Add(txChannel);
                    _clientTxStub = new ClientTransactionService.ClientTransactionServiceClient(txChannel);
                    response = await _clientTxStub.TxSubmitAsync(request);
                }
            }
        }
        catch (RpcException ex)
        {
            Console.WriteLine($"Tm server {currentTmAddress} for transaction unavailable, sending the transaction to another TM");
            currentTmAddress = _tmServers[(_tmServers.IndexOf(currentTmAddress) + 1) % _tmServers.Count];
            var txChannel = GrpcChannel.ForAddress(currentTmAddress);
            _grpcChannels.Add(txChannel);
            _clientTxStub = new ClientTransactionService.ClientTransactionServiceClient(txChannel);
            response = await _clientTxStub.TxSubmitAsync(request);
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
        foreach (var stub in _clientServerStubs)
        {
            try
            {
                var response = await stub.Key.StatusAsync(request);
                Console.WriteLine($"Status of {stub.Key}: {response.Status}");
            }
            catch (RpcException ex)
            {
                Console.WriteLine($"Status of {stub.Key}: unavailable");
                Thread.Sleep(0);
            }
        }
    }
}