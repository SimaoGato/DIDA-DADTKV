namespace TransactionManager;

public class Request
{
    public string ClientNick { get; set; }
    public string TransactionId { get; set; }
    public List<string> ObjectsLockNeeded { get; set; }
    public List<string> ObjectsToRead { get; set; }
    public List<KeyValuePair<string, long>> ObjectsToWrite { get; set; }
    public List<DadInt> TransactionResult { get; set; }
    
    public Request(string clientNick, string transactionId)
    {
        ClientNick = clientNick;
        TransactionId = transactionId;
        ObjectsLockNeeded = new List<string>();
        ObjectsToRead = new List<string>();
        ObjectsToWrite = new List<KeyValuePair<string, long>>();
        TransactionResult = new List<DadInt>();
    }
    
    public void AddTransactionResult(DadInt dadInt)
    {
        TransactionResult.Add(dadInt);
    }
    
    public void AddObjectToRead(string objectToRead)
    {
        ObjectsToRead.Add(objectToRead);
    }
    
    public void AddObjectToWrite(string objectToWrite, long value)
    {
        ObjectsToWrite.Add(new KeyValuePair<string, long>(objectToWrite, value));
    }
    
    public void AddObjectLockNeeded()
    {
        ObjectsLockNeeded.AddRange(ObjectsToRead);
        foreach (var item in ObjectsToWrite)
        {
            ObjectsLockNeeded.Add(item.Key);
        }
    }
    
    public void PrintRequest()
    {
        Console.WriteLine("Printing request");
        Console.WriteLine("ClientNick: {0}", ClientNick);
        Console.WriteLine("TransactionId: {0}", TransactionId);
        Console.WriteLine("ObjectsLockNeeded:");
        foreach (var item in ObjectsLockNeeded)
        {
            Console.WriteLine(item);
        }
        Console.WriteLine("ObjectsToRead:");
        foreach (var item in ObjectsToRead)
        {
            Console.WriteLine(item);
        }
        Console.WriteLine("ObjectsToWrite:");
        foreach (var item in ObjectsToWrite)
        {
            Console.WriteLine(item);
        }
        Console.WriteLine();
    }
}