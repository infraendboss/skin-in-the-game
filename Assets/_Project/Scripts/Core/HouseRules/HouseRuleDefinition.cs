using System;

namespace SITG.Core.HouseRules
{
    /// <summary>
    /// A rule the house announces at the start of a night.
    /// New rules are made by filling in these numbers, no code needed.
    /// </summary>
    [Serializable]
    public sealed class HouseRuleDefinition
    {
        public string Id = "";
        /// <summary>What the doll says when announcing the rule. English, shown in the game.</summary>
        public string Announcement = "";
        public double PayoutMultiplier = 1.0;
        public double FloorTimeMultiplier = 1.0;
        public double QuotaMultiplier = 1.0;
        /// <summary>Seconds between power cuts. 0 means no power cuts.</summary>
        public float BlackoutIntervalSeconds = 0f;
        /// <summary>Only rules marked as opener can be used on the first night.</summary>
        public bool IsOpener;
    }
}
