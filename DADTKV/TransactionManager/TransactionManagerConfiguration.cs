using System;
using System.Collections.Generic;

namespace TransactionManager
{
    public class TransactionManagerConfiguration
    {
        public string TmNick { get; }
        public string TmUrl { get; }
        public int NumberOfTm { get; }
        public int NumberOfLm { get; }
        public int TimeSlots { get; }
        public int SlotDuration { get; }
        public int SlotBehaviorCount { get; }
        public DateTime StartTime { get; }

        private readonly string[] _args;

        public TransactionManagerConfiguration(string[] args)
        {
            _args = args;

            TmNick = args[0];
            TmUrl = args[1];
            NumberOfTm = int.Parse(args[2]);
            NumberOfLm = int.Parse(args[3 + NumberOfTm * 2]);

            int argBreaker = Array.IndexOf(args, "-");
            TimeSlots = int.Parse(args[argBreaker + 1]);
            SlotDuration = int.Parse(args[argBreaker + 2]);
            SlotBehaviorCount = int.Parse(args[argBreaker + 3]);
            StartTime = DateTime.ParseExact(args[^1], "HH:mm:ss", null);
        }

        public List<string> ParseTmServers()
        {
            List<string> tmServers = new List<string>();
            for (int i = 0; i < NumberOfTm; i++)
            {
                if (_args[4 + i * 2] != TmUrl)
                {
                    tmServers.Add(_args[4 + i * 2]);
                }
            }
            return tmServers;
        }

        public List<string> ParseLmServers()
        {
            List<string> lmServers = new List<string>();
            for (int i = 0; i < NumberOfLm; i++)
            {
                lmServers.Add(_args[5 + NumberOfTm * 2 + i * 2]);
            }
            return lmServers;
        }

        public List<KeyValuePair<string, string>> ParseSlotBehavior()
        {
            var slotBehavior = new List<KeyValuePair<string, string>>();
            var start = Array.IndexOf(_args, "-") + 4;
            for (int i = start; i < start + SlotBehaviorCount; i++)
            {
                int index = _args[i].IndexOf("(");
                KeyValuePair<string, string> keyValuePair;
                if (index != -1)
                {
                    keyValuePair = new KeyValuePair<string, string>(_args[i].Substring(0, index),
                        _args[i].Substring(index));
                }
                else
                {
                    keyValuePair = new KeyValuePair<string, string>(_args[i], "");
                }
                slotBehavior.Add(keyValuePair);
            }
            return slotBehavior;
        }
    }
}
