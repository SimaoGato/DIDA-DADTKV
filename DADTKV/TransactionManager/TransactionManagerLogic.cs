namespace TransactionManager {
    public class TransactionManagerLogic {
        private readonly string[] _args;
        public string tmNick;
        public string tmUrl;
        private int numberOfTm;
        private int numberOfLm;
        public TransactionManagerLogic(string[] args) {
            _args = args;
            tmNick = args[0];
            tmUrl = args[1];
            numberOfTm = int.Parse(args[2]);
            numberOfLm = int.Parse(args[3 + numberOfTm * 2]);
        }

        public List<string> ParseTmServers() {
            List<string> tmServers = new List<string>();
            for(int i = 0; i < numberOfTm; i++)
            {
                if (_args[4 + i * 2] != tmUrl)
                {
                    tmServers.Add(_args[4 + i * 2]);
                }
            }
            return tmServers;
        }

        public List<string> ParseLmServers() {
            List<string> lmServers = new List<string>();
            for(int i = 0; i < numberOfLm; i++)
            {
                lmServers.Add(_args[5 + numberOfLm * 2 + i * 2]);
            }
            return lmServers;
        }
    }
}
