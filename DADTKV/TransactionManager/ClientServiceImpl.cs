using Grpc.Core;

namespace TransactionManager;

public class ClientServiceImpl : DadtkvClientService.DadtkvClientServiceBase
{
    private readonly object _dadIntMapLock = new object(); //TODO: Maybe change
    
    private TransactionManagerService _transactionManagerService;
    private string _transactionManagerId;
    private TransactionManagerState _transactionManagerState;
    
    public ClientServiceImpl(string transactionManagerId, TransactionManagerService transactionManagerService, TransactionManagerState transactionManagerState)
    {
        _transactionManagerId = transactionManagerId;
        _transactionManagerService = transactionManagerService;
        _transactionManagerState = transactionManagerState;
    }
    
    public override Task<TransactionResponse> TxSubmit(TransactionRequest request, ServerCallContext context)
    {
        return Task.FromResult(DoTransaction(request));
    }
    
    public override Task<StatusResponse> Status(StatusRequest request, ServerCallContext context)
    {
        return Task.FromResult(CheckStatus(request));
    }

    private TransactionResponse DoTransaction(TransactionRequest request)
    {
        string clientId = request.ClientId;
        List<string> objectsRequested = request.ObjectsToRead.ToList();
        
        Console.WriteLine("Received transaction request from client {0}", clientId);

        List<DadInt> responseDadIntList = new List<DadInt>();
        
        List<string> objectsToRead = request.ObjectsToRead.ToList();

        lock (_dadIntMapLock)
        {
            foreach (string dadIntKey in objectsToRead)
            {
                Console.WriteLine("Reading object: {0}", dadIntKey);

                if (_transactionManagerState.ContainsKey(dadIntKey))
                {

                    DadInt dadInt = new DadInt
                    {
                        Key = dadIntKey,
                        Value = _transactionManagerState.ReadOperation(dadIntKey)
                    };

                    responseDadIntList.Add(dadInt);
                }
            }

            List<DadInt> objectsToWrite = request.ObjectsToWrite.ToList();

            foreach (var dadInt in objectsToWrite)
            {
                string key = dadInt.Key;
                long value = dadInt.Value;

                objectsRequested.Add(key);
                _transactionManagerState.WriteOperation(key, value);
                Console.WriteLine("Writing object: {0} with value {1}", key, value);
            }
        }
        
        Console.WriteLine("{0}", _transactionManagerService.RequestLease(_transactionManagerId, objectsRequested));

        TransactionResponse response = new TransactionResponse();
        response.ObjectsRead.AddRange(responseDadIntList);

        return response;
    }
    
    private static StatusResponse CheckStatus(StatusRequest request)
    {
        StatusResponse response = new StatusResponse();
        response.Status = true;
        return response;
    }
}