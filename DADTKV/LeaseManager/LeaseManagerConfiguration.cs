namespace LeaseManager {
    public class LeaseManagerConfiguration {
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
        private int _slotBehaviorsCount; // TODO: If this is == timeSlots, then remove
        public Dictionary<int, List<string>> slotBehaviors; 
        public DateTime startTime;

        public LeaseManagerConfiguration(string[] args) {
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
            _slotBehaviorsCount = int.Parse(args[argBreaker + 3]);
            slotBehaviors = ParseSlotBehaviors();
            startTime = DateTime.ParseExact(args[^1], "HH:mm:ss", null);
            
            PrintArgs();
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
        
        
        public Dictionary<int, List<string>> ParseSlotBehaviors() {
            var slotBehaviors = new Dictionary<int, List<string>>();
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

        private void PrintArgs()
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
            Console.WriteLine("slotBehaviors: ");
            foreach (var slot in slotBehaviors)
            {
                Console.Write("Slot: " + slot.Key + " Behavior: ");
                foreach (var behavior in slot.Value)
                {
                   Console.Write(behavior + " "); 
                }
                Console.WriteLine();
            }
            Console.WriteLine("timeSlots: " + timeSlots);
            Console.WriteLine("slotDuration: " + slotDuration);
            Console.WriteLine("----------");
        }
    }
}