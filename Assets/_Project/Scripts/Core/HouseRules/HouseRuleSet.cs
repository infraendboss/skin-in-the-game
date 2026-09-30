using System;

namespace SITG.Core.HouseRules
{
    /// <summary>Applies the active house rule to payouts, time and quota.</summary>
    public sealed class HouseRuleSet
    {
        public HouseRuleDefinition Active { get; private set; }

        public HouseRuleSet(HouseRuleDefinition active)
        {
            SetActive(active);
        }

        public void SetActive(HouseRuleDefinition rule)
        {
            Active = rule ?? throw new ArgumentNullException(nameof(rule));
        }

        public long ApplyPayout(long payout) => (long)Math.Floor(payout * Active.PayoutMultiplier);
        public float ApplyFloorTime(float seconds) => (float)(seconds * Active.FloorTimeMultiplier);
        public long ApplyQuota(long quota) => (long)Math.Ceiling(quota * Active.QuotaMultiplier);
    }
}
