﻿using System;
using System.Collections.Generic;

namespace TransactionManager
{
    public class TransactionManagerConfiguration
    {
        public string TmNick { get; }
        public string TmUrl { get; }
        
        public int TmId { get; }
        public int NumberOfTm { get; }
        public int NumberOfLm { get; }
        public int TimeSlots { get; }
        public int SlotDuration { get; }
        public int SlotBehaviorCount { get; }
        public DateTime StartTime { get; }
        
        public Dictionary<string, string> TmNickMap { get; set; }
        
        public Dictionary<string, string> LmNickMap { get; set; }

        private readonly string[] _args;

        public TransactionManagerConfiguration(string[] args)
        {
            _args = args;

            try
            {
                TmNick = args[0];
                TmUrl = args[1];
                TmId = int.Parse(args[2]);
                NumberOfTm = int.Parse(args[3]);
                NumberOfLm = int.Parse(args[4 + NumberOfTm * 2]);

                int argBreaker = Array.IndexOf(args, "-");
                TimeSlots = int.Parse(args[argBreaker + 1]);
                SlotDuration = int.Parse(args[argBreaker + 2]);
                SlotBehaviorCount = int.Parse(args[argBreaker + 3]);
                StartTime = DateTime.ParseExact(args[^1], "HH:mm:ss", null);
            }
            catch (Exception ex)
            {
                throw new Exception("Error parsing configuration: " + ex.Message);
            }
        }
        
        public List<KeyValuePair<int, string>> ParseTmServers()
        {
            var tmServers = new List<KeyValuePair<int, string>>();
            TmNickMap = new Dictionary<string, string>();
            for (int i = 0; i < NumberOfTm; i++)
            {
                string nickname = _args[4 + i * 2];
                string url = _args[5 + i * 2];
                if (_args.Length > 5 + i * 2 && _args[5 + i * 2] != TmUrl)
                {
                    tmServers.Add(new KeyValuePair<int, string>(i, _args[5 + i * 2]));
                    TmNickMap.Add(nickname, url);
                }
            }
            return tmServers;
        }

        public List<KeyValuePair<int, string>> ParseLmServers()
        {
            var lmServers = new List<KeyValuePair<int, string>>();
            LmNickMap = new Dictionary<string, string>();
            for (int i = 0; i < NumberOfLm; i++)
            {
                string nickname = _args[5 + NumberOfTm * 2 + i * 2];
                string url = _args[6 + NumberOfTm * 2 + i * 2];
                if (_args.Length > 6 + NumberOfTm * 2 + i * 2)
                {
                    lmServers.Add(new KeyValuePair<int, string>(i, _args[6 + NumberOfTm * 2 + i * 2]));
                    LmNickMap.Add(nickname, url);
                }
            }
            return lmServers;
        }

        public List<KeyValuePair<string, string>> ParseSlotBehavior()
        {
            var slotBehavior = new List<KeyValuePair<string, string>>();
            var start = Array.IndexOf(_args, "-") + 4;
            for (int i = start; i < start + SlotBehaviorCount; i++)
            {
                if (i < _args.Length)
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
            }
            return slotBehavior;
        }
    }
}

