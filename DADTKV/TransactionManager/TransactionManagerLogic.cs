using System.Collections.Specialized;

namespace TransactionManager {
    public class TransactionManagerLogic {
        private readonly string[] _args;
        public string tmNick;
        public string tmUrl;
        private int numberOfTm;
        private int numberOfLm;
        public int timeSlots;
        public int slotDuration;
        private int slotBehaviorCount;
        public DateTime startTime;
        
        public TransactionManagerLogic(string[] args) {
            // TODO REMOVE TIME SLOTS PARSING FROM TM
            _args = args;
            tmNick = args[0];
            tmUrl = args[1];
            numberOfTm = int.Parse(args[2]);
            numberOfLm = int.Parse(args[3 + numberOfTm * 2]);
            var argBreaker = Array.IndexOf(args, "-");
            timeSlots = int.Parse(args[argBreaker + 1]);
            slotDuration = int.Parse(args[argBreaker + 2]);
            slotBehaviorCount = int.Parse(args[argBreaker + 3]);
            startTime = DateTime.ParseExact(args[^1], "HH:mm:ss", null);
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
                lmServers.Add(_args[5 + numberOfTm * 2 + i * 2]);
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
    }
}
