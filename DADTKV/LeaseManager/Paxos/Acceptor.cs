using Grpc.Core;

namespace LeaseManager.Paxos;

public class Acceptor : PaxosService.PaxosServiceBase
{
    private readonly int _lmId;
    private readonly int _nServers;
    private int _round;
    private int _IDp = -1;
    private int _IDa = -1;
    private List<List<string>> _value;
    private readonly List<KeyValuePair<int, List<List<string>>>> _acceptedValues;
    
    public Acceptor(int lmId, int nServers)
    {
        _lmId = lmId;
        _nServers = nServers;
        _value = new List<List<string>>();
        _acceptedValues = new List<KeyValuePair<int, List<List<string>>>>();
        _round = -1;
    }
    
    public override Task<Promise> PaxosPhaseOne(Prepare prepare, ServerCallContext context)
    {
        return Task.FromResult(DoPhaseOne(prepare));
    }
   
    public override Task<Accepted> PaxosPhaseTwo(Accept accept, ServerCallContext context)
    {
        return Task.FromResult(DoPhaseTwo(accept));
    }

    public Promise DoPhaseOne(Prepare prepare)
    {
        // Ignore calls of suspects
        if ((prepare.IDp % _nServers) != _lmId && Suspects.IsSuspected(Suspects.GetNickname(prepare.IDp % _nServers)))
        {
            return new Promise() { IDp = -2 };
        }
        
        Promise promise = new Promise();
        
        if (prepare.Round > _round)
        {
            _IDp = -1;
            _IDa = -1;
            _round = prepare.Round;
        }

        if (prepare.Round < _round) // Proposer is in an older round
        {
            promise.Round = _round;
            promise.IDp = prepare.IDp;
            promise.IDa = _acceptedValues[prepare.Round - 1].Key;
            SetPromiseValue(promise, _acceptedValues[prepare.Round - 1].Value);
            return promise;
        }
        
        // Did it promise to ignore requests with this ID?
        if (prepare.IDp < _IDp)
        {
            promise.IDp = -1; // Ignore request
        }
        else // Will promise to ignore request with lower Id
        {
            _round = prepare.Round;
            _IDp = prepare.IDp;
            promise.IDp = _IDp;
        }

        promise.Round = _round;
        promise.IDa = _IDa;
        SetPromiseValue(promise, _value);
        return promise;
    }
    
    public Accepted DoPhaseTwo(Accept accept)
    {
        if ((accept.IDp % _nServers) != _lmId && Suspects.IsSuspected(Suspects.GetNickname(accept.IDp % _nServers)))
        {
            return new Accepted() { IDp = -2 };
        }
        
        Accepted accepted = new Accepted();

        if (accept.Round < _round)
        {
            accepted.Round = _round;
            accepted.IDp = accept.IDp;
            SetAcceptedValue(accepted, _acceptedValues[accept.Round - 1].Value);
            return accepted;
        }
        // Did it promise to ignore requests with this ID?
        if (accept.IDp < _IDp)
        {
            accepted.IDp = -1; // Ignore request
        }
        else // Reply with accept Id and value 
        {
            _IDp = accept.IDp;
            _IDa = _IDp;
            _value = UpdateValue(accept);
            if (accept.Round > _acceptedValues.Count) // New value for a new round
            {
               _acceptedValues.Add(new KeyValuePair<int, List<List<string>>>(_IDa, _value.ToList())); 
            }
            else // New value for the current round
            {
                _acceptedValues[accept.Round - 1] = new KeyValuePair<int, List<List<string>>>(_IDa, _value.ToList());
            }
            accepted.IDp = _IDp;
            SetAcceptedValue(accepted, _value);
        }
        
        return accepted;
    }
    
    private void SetPromiseValue(Promise promise, List<List<string>> value)
    {
        foreach (var leaseAux in value)
        {
            Lease lease = new Lease();
            lease.Value.AddRange(leaseAux);
            promise.Value.Add(lease);
        }
    }

    private static List<List<string>> UpdateValue(Accept accept)
    {
        List<List<string>> auxLease = new List<List<string>>();
        foreach (Lease lease in accept.Value)
        {
            List<string> list = new List<string>(lease.Value);
            auxLease.Add(list);
        }

        return auxLease;
    }

    private void SetAcceptedValue(Accepted accepted, List<List<string>> value)
    {
        foreach (var leaseAux in value)
        {
            Lease lease = new Lease();
            lease.Value.AddRange(leaseAux);
            accepted.Value.Add(lease);
        }
    }
    
    public void PrepareForNextEpoch()
    {
        _IDp = -1;
        _IDa = -1;
        _value.Clear();
    }
    
}