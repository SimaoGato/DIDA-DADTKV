using Grpc.Core;

namespace TransactionManager
{
    public class ClientTxServiceImpl : ClientTransactionService.ClientTransactionServiceBase
    {
        private readonly object _lock = new object();
        private readonly TransactionManagerService _transactionManagerService;
        private readonly TransactionManagerState _transactionManagerState;
        private readonly string _transactionManagerId;
        private ClientRequestHandler _clientRequestHandler;
        
        public bool isUpdating { get; set; }

        public ClientTxServiceImpl(string transactionManagerId, TransactionManagerService transactionManagerService, 
            TransactionManagerState transactionManagerState, ClientRequestHandler clientRequestHandler)
        {
            _transactionManagerId = transactionManagerId;
            _transactionManagerService = transactionManagerService;
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
                string uniqueTransactionId = clientId + "-" + System.Guid.NewGuid().ToString();
                
                Console.WriteLine("[ClientServiceImpl] Received transaction request from client {0}", clientId);
                
                List<string> objectsRequested = new List<string>();
                objectsRequested.Add(uniqueTransactionId);
                objectsRequested.AddRange(request.ObjectsToRead.Select(item => item));
                objectsRequested.AddRange(request.ObjectsToWrite.Select(item => item.Key));

                // Request a lease for the objects
                //Console.WriteLine("[ClientServiceImpl] Received ack: {0}", _transactionManagerService.RequestLease(_transactionManagerId, objectsRequested));
                _transactionManagerService.RequestLease(_transactionManagerId, objectsRequested);
                
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
                
                // TO TEST CLIENT RESUBMISSION OF TRANSACTION
                // TransactionResponse ABORT = new TransactionResponse();
                //
                // ABORT.ObjectsRead.Add(new DadInt
                // {
                //     Key = "abort",
                //     Value = 0
                // });
                //
                // return ABORT;
                
                requestToPush.AddObjectLockNeeded();
                
                // Push request to queue
                _clientRequestHandler.PushRequest(requestToPush);
                
                // Wait for request to be executed
                _clientRequestHandler.WaitForTransaction(uniqueTransactionId).WaitOne();
                
                TransactionResponse response = new TransactionResponse();
                
                response.ObjectsRead.AddRange(requestToPush.TransactionResult);

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
