using Grpc.Net.Client;

namespace LeaseManager.Paxos;

public class Proposer
{
    private Dictionary<string, PaxosService.PaxosServiceClient> _stubs =
        new Dictionary<string, PaxosService.PaxosServiceClient>();
    private bool _isLeader;
    private int _id;
    private int _value; // TODO: Change to buffer for the leaseRequests
    private int _nServers;
  
    public Proposer(string nick, int nServers, Dictionary<string, Uri> addresses)
    {
        _nServers = nServers;
        foreach (var addr in addresses)
        {
            if (addr.Key != nick)
            {
                var channel = GrpcChannel.ForAddress(addr.Value.ToString());
                var stub = new PaxosService.PaxosServiceClient(channel);
                _stubs[addr.Key] = stub;
            }
        }
    }

    public void PhaseOne()
    {
        var prepare = new Prepare
        {
            Id = _id
        };
        
        int count = 0;

        foreach (var stub in _stubs)
        {
            var promise = stub.PhaseOne(prepare);
            if (promise.Id == _id)
            {
                count++;
                if (promise.prevAcceptedValue != -1)
                {
                    _value = promise.prevAcceptedValue;
                }
            }
        }
        
        if (count > _nServers / 2)
        {
            PhaseTwo();
        }
    }

    public void PhaseTwo()
    {
        var accept = new Accept
        {
            Id = _id,
            Value = _value
        };

        foreach (var stub in _stubs)
        {
            var accepted = stub.PhaseTwo(accept);
            if (accepted.Id == _id)
            {
                // do something
            }
        }
    }
}