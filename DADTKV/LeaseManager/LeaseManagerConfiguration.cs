namespace LeaseManager {
    public class LeaseManagerConfiguration {
        private readonly string[] _args;
        public readonly string lmNick;
        public readonly string lmUrl;
        public readonly int lmId;
        public readonly int numberOfLm;
        public readonly Dictionary<int, string> lmIdsMap;
        public readonly Dictionary<string, string> lmServers;
        private readonly int numberOfTm;
        public readonly Dictionary<int, string> tmIdsMap;
        public readonly Dictionary<string, string> tmServers;
        public readonly int timeSlots;
        public readonly int slotDuration;
        private readonly int _slotBehaviorsCount;
        public Dictionary<int, List<string>> slotBehaviors; 
        public DateTime startTime;

        public LeaseManagerConfiguration(string[] args) {
            _args = args;
            lmNick = args[0];
            lmUrl = args[1];
            lmId = int.Parse(args[2]);
            numberOfLm = int.Parse(args[3]);
            ParseLmServers(out Dictionary<int, string> _lmIdsMap, out Dictionary<string, string> _lmServers);
            lmIdsMap = _lmIdsMap;
            lmServers = _lmServers;
            numberOfTm = int.Parse(args[4 + numberOfLm * 2]);
            ParseTmServers(out Dictionary<int, string> _tmIdsMap, out Dictionary<string, string> _tmServers);
            tmIdsMap = _tmIdsMap;
            tmServers = _tmServers;
            var argBreaker = Array.IndexOf(args, "-");
            timeSlots = int.Parse(args[argBreaker + 1]);
            slotDuration = int.Parse(args[argBreaker + 2]);
            _slotBehaviorsCount = int.Parse(args[argBreaker + 3]);
            slotBehaviors = ParseSlotBehaviors();
            startTime = DateTime.ParseExact(args[^1], "HH:mm:ss", null);
        }
        
        private void ParseTmServers(out Dictionary<int, string> _tmIdsMap, out Dictionary<string, string> _tmServers) {
            _tmIdsMap = new Dictionary<int, string>();
            _tmServers = new Dictionary<string, string>(); 
            for(int i = 0; i < numberOfTm; i++)
            {
                string nickname = _args[5 + numberOfLm * 2 + i * 2];
                string url = _args[6 + numberOfLm * 2 + i * 2];
                _tmIdsMap.Add(i, nickname);
                _tmServers.Add(nickname, url);
            }
        }

        private void ParseLmServers(out Dictionary<int, string> _lmIdsMap, out Dictionary<string, string> _lmServers) {
            _lmIdsMap = new Dictionary<int, string>();
            _lmServers = new Dictionary<string, string>();
            for(int i = 0; i < numberOfLm; i++)
            {
                string nickname = _args[4 + i * 2];
                string url = _args[5 + i * 2];
                if (nickname != lmNick)
                {
                    _lmIdsMap.Add(i, nickname);
                    _lmServers.Add(nickname, url);
                }
            }
        }
        
        private Dictionary<int, List<string>> ParseSlotBehaviors() {
            slotBehaviors = new Dictionary<int, List<string>>();
            var start = Array.IndexOf(_args, "-") + 4;
            for (var i = start; i < start + _slotBehaviorsCount; i++)
            {
                var behaviors = new List<string>();
                var parts = _args[i].Split('#');
                var slot = int.Parse(parts[0]);
                behaviors.Add(parts[1]);
                behaviors.Add(parts[2]);
                if (parts.Length > 3)
                {
                    behaviors.Add(parts[3]);
                }
                slotBehaviors.Add(slot, behaviors);
            }
            
            return slotBehaviors;
        }
        
    }
}