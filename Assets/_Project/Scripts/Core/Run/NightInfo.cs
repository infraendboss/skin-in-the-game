using System.Collections.Generic;
using SITG.Core.HouseRules;
using SITG.Core.Stakes;

namespace SITG.Core.Run
{
    /// <summary>What the doll announces at the start of a night.</summary>
    public sealed class NightInfo
    {
        public int NightIndex;
        public int NightCount;
        public long Quota;
        public float FloorTimeSeconds;
        public HouseRuleDefinition HouseRule;
    }

    /// <summary>What happened when the night was settled.</summary>
    public sealed class NightResult
    {
        public int NightIndex;
        public long Quota;
        public bool QuotaMet;
        /// <summary>Money the house took (the quota, or everything left if it wasn't met).</summary>
        public long MoneyCollected;
        public readonly List<PartTaken> PartsTaken = new List<PartTaken>();
        public PlayerKey? TopBetterAgainstTeam;
        public RunResult Result;
    }
}
