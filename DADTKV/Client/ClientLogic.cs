namespace Client
{
    public class ClientLogic
    {
        private readonly string[] _clientArgs;
        public string ClientNick { get; }
        public string ScriptPath { get; }
        public int ClientId { get; }
        public int NumberOfTm { get; }
        public List<string> TmServers { get; }
        public int NumberOfLm { get; }
        public List<string> LmServers { get; }
        public string MainTmServer { get; }
        public DateTime StartTime { get; }

        public ClientLogic(string[] args)
        {
            _clientArgs = args;
            ClientNick = args[0];
            ScriptPath = @"..\..\..\" + args[1];
            ClientId = int.Parse(args[2]);
            NumberOfTm = int.Parse(args[3]);
            TmServers = ParseTmServers();
            NumberOfLm = int.Parse(args[4 + NumberOfTm * 2]);
            LmServers = ParseLmServers();
            MainTmServer = TmServers[ClientId % NumberOfTm];
            StartTime = DateTime.ParseExact(args[^1], "HH:mm:ss", null);
        }

        private List<string> ParseTmServers()
        {
            List<string> servers = new List<string>();
            for (int i = 0; i < NumberOfTm; i++)
            {
                servers.Add(_clientArgs[5 + i * 2]);
            }
            return servers;
        }
        
        private List<string> ParseLmServers()
        {
            List<string> servers = new List<string>();
            for (int i = 0; i < NumberOfLm; i++)
            {
                servers.Add(_clientArgs[6 + NumberOfTm * 2 + i * 2]);
            }
            return servers;
        }

        public List<string> ParseObjectsToRead(string[] parts)
        {
            var objectsToRead = new List<string>();
            var readEls = parts[1].Trim('(', ')').Split(',');
            foreach (var el in readEls)
            {
                objectsToRead.Add(el.Trim());
            }
            return objectsToRead;
        }

        public List<KeyValuePair<string, int>> ParseObjectsToWrite(string[] parts)
        {
            var objectsToWrite = new List<KeyValuePair<string, int>>();
            var writeEls = parts[2].Trim('(', ')').Split(',');
            if (writeEls.Length > 1)
            {
                for (int i = 0; i < writeEls.Length; i += 2)
                {
                    var keyValuePair = new KeyValuePair<string, int>(writeEls[i].Trim('<', '>'),
                        int.Parse(writeEls[i + 1].Trim('<', '>')));
                    objectsToWrite.Add(keyValuePair);
                }
            }
            return objectsToWrite;
        }

        public void PrintArgs()
        {
            Console.WriteLine("=================================");
            Console.WriteLine("clientId: " + ClientId);
            Console.WriteLine("mainTmServer: " + MainTmServer);
            Console.WriteLine(Directory.GetCurrentDirectory());
            Console.WriteLine("scriptPath: " + ScriptPath);
            Console.WriteLine("TmServers: ");
            foreach (var tmServer in TmServers)
            {
                Console.WriteLine("    -> " + tmServer);
            }
            Console.WriteLine("LmServers: ");
            foreach (var lmServer in LmServers)
            {
                Console.WriteLine("    -> " + lmServer);
            }
            Console.WriteLine("=================================");

        }
    }
}