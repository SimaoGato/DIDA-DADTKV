using Grpc.Core;

namespace TransactionManager
{
    public class ClientTxServiceImpl : ClientTransactionService.ClientTransactionServiceBase
    {
        private readonly object _lock = new object();
        private readonly TransactionManagerService _transactionManagerService;
        private readonly TransactionManagerState _transactionManagerState;
        private readonly string _transactionManagerId;

        public ClientTxServiceImpl(string transactionManagerId, TransactionManagerService transactionManagerService, 
            TransactionManagerState transactionManagerState)
        {
            _transactionManagerId = transactionManagerId;
            _transactionManagerService = transactionManagerService;
            _transactionManagerState = transactionManagerState;
        }

        public override Task<TransactionResponse> TxSubmit(TransactionRequest request, ServerCallContext context)
        {
            return Task.FromResult(DoTransaction(request));
        }

        private TransactionResponse DoTransaction(TransactionRequest request)
        {
            try
            {
                string clientId = request.ClientId;
                List<string> objectsRequested = new List<string>();
                // random unique transaction id
                var uniqueTransactionId = clientId + "-" + System.Guid.NewGuid().ToString();

                // Combine unique transaction id, objects to read and objects to write into a single list
                // objectsRequested.Add(uniqueTransactionId);
                objectsRequested.AddRange(request.ObjectsToRead.Select(item => item));
                objectsRequested.AddRange(request.ObjectsToWrite.Select(item => item.Key));

                //Console.WriteLine("[ClientServiceImpl] Received transaction request from client {0}", clientId);

                // Request a lease for the objects
                Console.WriteLine("[ClientServiceImpl] Received ack: {0}", _transactionManagerService.RequestLease(_transactionManagerId, objectsRequested));

                List<DadInt> responseDadIntList = new List<DadInt>();

                lock (_lock)
                {
                    // Read objects
                    foreach (string dadIntKey in request.ObjectsToRead)
                    {
                        // Console.WriteLine("Reading object: {0}", dadIntKey);

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

                        // Write objects
                        foreach (var dadInt in request.ObjectsToWrite)
                        {
                            string key = dadInt.Key;
                            long value = dadInt.Value;

                            objectsRequested.Add(key);
                            _transactionManagerState.WriteOperation(key, value);
                            // Console.WriteLine("Writing object: {0} with value {1}", key, value);
                        }
                }

                //Console.WriteLine("[ClientServiceImpl] Client {0} transaction completed", clientId);

                TransactionResponse response = new TransactionResponse();
                response.ObjectsRead.AddRange(responseDadIntList);

                //Console.WriteLine("[ClientServiceImpl] Transaction response sent");

                return response;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error while executing transaction: {ex.Message}");
                return new TransactionResponse
                {
                    ObjectsRead = { },
                    
                };
            }
        }
    }
}

namespace TransactionManager
{
    public class ClientStatusServiceImpl : ClientStatusService.ClientStatusServiceBase
    {
        private readonly string _serverNick;

        public ClientStatusServiceImpl(string nick)
        {
            _serverNick = nick;
        }

        public override Task<StatusResponse> Status(StatusRequest request, ServerCallContext context)
        {
            return Task.FromResult(new StatusResponse
            {
                Status = true
            });
        }
    }
}
