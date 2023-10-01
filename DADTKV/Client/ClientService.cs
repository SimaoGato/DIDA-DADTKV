using Grpc.Net.Client;

namespace Client;

public class ClientService
{
    
    private DadtkvClientService.DadtkvClientServiceClient _clientStub;
    
    public ClientService(string mainTmAddress)
    {
        var channel = GrpcChannel.ForAddress(mainTmAddress);
        _clientStub = new DadtkvClientService.DadtkvClientServiceClient(channel);
    }

    public void TxSubmit(string clientId, List<string> objectsToRead, List<KeyValuePair<string, int>> objectsToWrite)
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
        
        var response = _clientStub.TxSubmit(request);

        foreach (var dadInt in response.ObjectsRead)
        {
            Console.WriteLine("Read object: {0} with value {1}", dadInt.Key, dadInt.Value);
        }
        Console.WriteLine("Response: {0}", response);
    }

    public void Status()
    {
        var request = new StatusRequest();
        
        var reply = _clientStub.Status(request);
        
        Console.WriteLine("Status: {0}", reply.Status);
    }
}