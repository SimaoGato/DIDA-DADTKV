using Grpc.Core;

namespace TransactionManager;

public class ClientServiceImpl : DadtkvClientService.DadtkvClientServiceBase
{
    private readonly object _dadIntMapLock = new object(); //TODO: Maybe change
    private Dictionary<string, long> _dadIntMap = new Dictionary<string, long>();
    
    public override Task<TransactionResponse> TxSubmit(TransactionRequest request, ServerCallContext context)
    {
        return Task.FromResult(DoTransaction(request));
    }
    
    public override Task<StatusResponse> Status(StatusRequest request, ServerCallContext context)
    {
        return Task.FromResult(CheckStatus(request));
    }

    public TransactionResponse DoTransaction(TransactionRequest request)
    {
        string clientId = request.ClientId;
        Console.WriteLine("Received transaction request from client {0}", clientId);

        List<DadInt> responseDadIntList = new List<DadInt>();
        
        List<string> objectsToRead = request.ObjectsToRead.ToList();

        lock (_dadIntMapLock)
        {
            foreach (string dadIntKey in objectsToRead)
            {
                Console.WriteLine("Reading object: {0}", dadIntKey);

                if (_dadIntMap.ContainsKey(dadIntKey))
                {

                    DadInt dadInt = new DadInt
                    {
                        Key = dadIntKey,
                        Value = _dadIntMap[dadIntKey]
                    };

                    responseDadIntList.Add(dadInt);
                }
            }

            List<DadInt> objectsToWrite = request.ObjectsToWrite.ToList();

            foreach (var dadInt in objectsToWrite)
            {
                string key = dadInt.Key;
                long value = dadInt.Value;

                _dadIntMap[key] = value;
                Console.WriteLine("Writing object: {0} with value {1}", key, value);
            }
        }

        TransactionResponse response = new TransactionResponse();
        response.ObjectsRead.AddRange(responseDadIntList);

        return response;
    }
    
    public StatusResponse CheckStatus(StatusRequest request)
    {
        StatusResponse response = new StatusResponse();
        response.Status = true;
        return response;
    }
}