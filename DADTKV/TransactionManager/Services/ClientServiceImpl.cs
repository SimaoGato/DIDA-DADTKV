using Grpc.Core;

namespace TransactionManager
{
    public class ClientTxServiceImpl : ClientTransactionService.ClientTransactionServiceBase
    {
        private readonly object _lock = new object();
        private readonly TransactionManagerLeaseService _transactionManagerLeaseService;
        private readonly TransactionManagerState _transactionManagerState;
        private readonly string _transactionManagerId;
        private ClientRequestHandler _clientRequestHandler;
        
        public bool isUpdating { get; set; }

        public ClientTxServiceImpl(string transactionManagerId, TransactionManagerLeaseService transactionManagerLeaseService, 
            TransactionManagerState transactionManagerState, ClientRequestHandler clientRequestHandler)
        {
            _transactionManagerId = transactionManagerId;
            _transactionManagerLeaseService = transactionManagerLeaseService;
            _transactionManagerState = transactionManagerState;
            isUpdating = false;
            _clientRequestHandler = clientRequestHandler;
        }

        public override Task<TransactionResponse> TxSubmit(TransactionRequest request, ServerCallContext context)
        {
            if (!isUpdating)
            {
                return Task.FromResult(DoTransaction(request));
            }
            else
            {
                DadInt dadInt = new DadInt
                {
                    Key = "abort",
                    Value = 1 // code for updating
                };

                TransactionResponse response = new TransactionResponse();
                response.ObjectsRead.Add(dadInt);
                
                return Task.FromResult(response);
            }
        }

        private TransactionResponse DoTransaction(TransactionRequest request)
        {
            try
            {
                string clientId = request.ClientId;
                string uniqueTransactionId = clientId + "-" + Guid.NewGuid().ToString();
                
                Console.WriteLine("[ClientServiceImpl] Received transaction request from client {0}", clientId);
                
                Request requestToPush = new Request(clientId, uniqueTransactionId);
                
                // Read objects
                foreach (string dadIntKey in request.ObjectsToRead)
                {
                    requestToPush.AddObjectToRead(dadIntKey);
                }

                // Write objects
                foreach (var dadInt in request.ObjectsToWrite)
                {
                    string key = dadInt.Key;
                    long value = dadInt.Value;

                    requestToPush.AddObjectToWrite(key, value);
                }
                
                requestToPush.AddObjectLockNeeded();
                
                // Push request to queue
                _clientRequestHandler.PushRequest(requestToPush);
                
                // Wait for request to be executed
                _clientRequestHandler.WaitForTransaction(uniqueTransactionId).WaitOne();
                
                TransactionResponse response = new TransactionResponse();
                
                response.ObjectsRead.AddRange(requestToPush.TransactionResult);
                
                Console.WriteLine("[ClientServiceImpl] Transaction of client {0} executed", clientId);

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
