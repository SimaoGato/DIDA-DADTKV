namespace Client {
    public class ClientLogic {
        private readonly string[] _clientArgs;
        public string clientNick;
        public string scriptPath;
        public int clientId;
        public int numberOfTm;
        public DateTime startTime;

        public ClientLogic(string[] args) {
            _clientArgs = args;
            clientNick = args[0];
            scriptPath = @"..\..\..\" + args[1];
            clientId = int.Parse(args[2]);
            numberOfTm = int.Parse(args[3]);
            startTime = DateTime.ParseExact(args[^1], "HH:mm:ss", null);
        }

        public List<string> ParseTmServers() {
            List<string> tmServers = new List<string>();
            for (int i = 0; i < this.numberOfTm; i++) {
                tmServers.Add(_clientArgs[5 + i * 2]);
            }
            return tmServers;
        }

        public List<string> ParseObjectsToRead(string[] parts) {
            var objectsToRead = new List<string>();
            var readEls = parts[1].Trim('(', ')').Split(',');
            foreach (var el in readEls) {
                objectsToRead.Add(el.Trim());
            }
            return objectsToRead;
        }

        public List<KeyValuePair<string, int>> ParseObjectsToWrite(string[] parts) {
            var objectsToWrite = new List<KeyValuePair<string, int>>();
            var writeEls = parts[2].Trim('(', ')').Split(',');
            if (writeEls.Length > 1) {
                for (int i = 0; i < writeEls.Length; i += 2) {
                    var keyValuePair = new KeyValuePair<string, int>(writeEls[i].Trim('<', '>'),
                        int.Parse(writeEls[i + 1].Trim('<', '>')));
                    objectsToWrite.Add(keyValuePair);
                }
            }
            return objectsToWrite;
        }
    }
}
