namespace LeaseManager {
    public class LeaseManagerLogic {
        private string[] _args;
        public string lmNick;
        public string lmUrl;
        public int lmId;
        public int numberOfLm;
        public List<string> lmServers;
        public int numberOfTm;
        public List<string> tmServers;
        public int timeSlots;
        public int slotDuration;
        private int slotBehaviorCount;
        public List<KeyValuePair<string, string>> slotBehavior;
        public DateTime startTime;

        public LeaseManagerLogic(string[] args) {
            _args = args;
            lmNick = args[0];
            lmUrl = args[1];
            lmId = int.Parse(args[2]);
            numberOfLm = int.Parse(args[3]);
            lmServers = ParseLmServers();
            numberOfTm = int.Parse(args[4 + numberOfLm * 2]);
            tmServers = ParseTmServers();
            var argBreaker = Array.IndexOf(args, "-");
            timeSlots = int.Parse(args[argBreaker + 1]);
            slotDuration = int.Parse(args[argBreaker + 2]);
            slotBehaviorCount = int.Parse(args[argBreaker + 3]);
            slotBehavior = ParseSlotBehavior();
            startTime = DateTime.ParseExact(args[^1], "HH:mm:ss", null);
            
            //PrintArgs();
        }
        
        public List<string> ParseTmServers() {
            List<string> tmServers = new List<string>();
            for(int i = 0; i < numberOfTm; i++)
            {
                tmServers.Add(_args[6 + numberOfLm * 2 + i * 2]);
            }
            return tmServers;
        }

        public List<string> ParseLmServers() {
            List<string> lmServers = new List<string>();
            for(int i = 0; i < numberOfLm; i++)
            {
                if (_args[5 + i * 2] != lmUrl)
                {
                    lmServers.Add(_args[5 + i * 2]);
                }
            }
            return lmServers;
        }
        
        public List<KeyValuePair<string, string>> ParseSlotBehavior() {
            var slotBehavior = new List<KeyValuePair<string, string>>();
            var start = Array.IndexOf(_args, "-") + 4;
            for (int i = start; i < start + slotBehaviorCount; i++) {
                int index = _args[i].IndexOf("(");
                KeyValuePair<string, string> keyValuePair;
                if (index != -1) {
                    keyValuePair = new KeyValuePair<string, string>(_args[i].Substring(0, index),
                        _args[i].Substring(index));
                }
                else {
                    keyValuePair = new KeyValuePair<string, string>(_args[i], "");
                }
                slotBehavior.Add(keyValuePair);
            }
            return slotBehavior;
        }

        public void PrintArgs()
        {
            Console.WriteLine("lmNick: " + lmNick);
            Console.WriteLine("lmUrl: " + lmUrl);
            Console.WriteLine("lmId: " + lmId);
            Console.WriteLine("LmServers: ");
            foreach (var lmServer in lmServers)
            {
                Console.WriteLine(lmServer);
            }
            Console.WriteLine("TmServers: ");
            foreach (var tmServer in tmServers)
            {
                Console.WriteLine(tmServer);
            }
            Console.WriteLine("slotBehavior: ");
            foreach (var slot in slotBehavior)
            {
                Console.WriteLine(slot.Key + "-" + slot.Value);
            }
            Console.WriteLine("timeSlots: " + timeSlots);
            Console.WriteLine("slotDuration: " + slotDuration);
            Console.WriteLine("----------");
        }
    }
}