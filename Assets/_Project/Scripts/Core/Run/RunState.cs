using System;
using System.Collections.Generic;

namespace SITG.Core.Run
{
    public enum RunResult { InProgress, Won, Lost }

    /// <summary>
    /// The complete state of one run. This is what gets saved and resumed.
    /// Only plain fields and lists, so Unity's JsonUtility can save it.
    /// </summary>
    [Serializable]
    public sealed class RunState
    {
        public int Seed;

        /// <summary>Random generator state at the start of the current night. Used to resume.</summary>
        public ulong NightStartRngState;

        public int NightIndex;
        public bool NightInProgress;
        public long TeamBank;
        public long[] QuotaPerNight = Array.Empty<long>();
        public string[] HouseRuleIdPerNight = Array.Empty<string>();
        public List<PlayerState> Players = new List<PlayerState>();
        public RunResult Result = RunResult.InProgress;

        public int NightCount => QuotaPerNight.Length;
        public bool IsLastNight => NightIndex >= NightCount - 1;
        public long CurrentQuota => QuotaPerNight[NightIndex];
        public string CurrentHouseRuleId => HouseRuleIdPerNight[NightIndex];

        public PlayerState Find(PlayerKey key)
        {
            foreach (var p in Players) if (p.Key == key) return p;
            return null;
        }

        public PlayerState Get(PlayerKey key)
        {
            var p = Find(key);
            if (p == null) throw new KeyNotFoundException($"No player with key '{key}' in this run.");
            return p;
        }

        public bool AllGhosts
        {
            get
            {
                if (Players.Count == 0) return false;
                foreach (var p in Players) if (!p.IsGhost) return false;
                return true;
            }
        }
    }
}
