using Grpc.Core;

namespace LeaseManager;

public class ClientStatusServiceImpl : ClientStatusService.ClientStatusServiceBase
{
    private string serverNick;
    public ClientStatusServiceImpl(string nick)
    {
        serverNick = nick;
    }
    public override Task<StatusResponse> Status(StatusRequest request, ServerCallContext context)
    {
        return Task.FromResult(CheckStatus(request, serverNick));
    }
    
    private static StatusResponse CheckStatus(StatusRequest request, string nick)
    {
        StatusResponse response = new StatusResponse
        {
            Nick = nick,
            Status = true
        };
        return response;
    }
}