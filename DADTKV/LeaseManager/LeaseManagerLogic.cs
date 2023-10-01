namespace LeaseManager {
    public class LeaseManagerLogic {
        private string[] _args;
        public string lmNick;
        public string lmUrl;
        public int numberOfLm;
        public int numberOfTm;

        public LeaseManagerLogic(string[] args) {
            _args = args;
            lmNick = args[0];
            lmUrl = args[1];
            numberOfLm = int.Parse(args[2]);
            numberOfTm = int.Parse(args[3 + numberOfLm * 2]);
        }
        
        public List<string> ParseTmServers() {
            List<string> tmServers = new List<string>();
            for(int i = 0; i < numberOfTm; i++)
            {
                tmServers.Add(_args[5 + numberOfLm * 2 + i * 2]);
            }
            return tmServers;
        }

        public List<string> ParseLmServers() {
            List<string> lmServers = new List<string>();
            for(int i = 0; i < numberOfLm; i++)
            {
                if (_args[4 + i * 2] != lmUrl)
                {
                    lmServers.Add(_args[4 + i * 2]);
                }
            }
            return lmServers;
        }
    }
}
