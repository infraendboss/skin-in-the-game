using System.Collections.Generic;

namespace SITG.Core.HouseRules
{
    /// <summary>The four house rules of the MVP. Later these become assets you edit in Unity.</summary>
    public static class DefaultHouseRules
    {
        public const string CalmNightId = "calm_night";
        public const string DoubleOrDebtId = "double_or_debt";
        public const string PowerCutsId = "power_cuts";
        public const string HappyHourId = "happy_hour";

        public static IReadOnlyList<HouseRuleDefinition> All { get; } = new List<HouseRuleDefinition>
        {
            new HouseRuleDefinition
            {
                Id = CalmNightId,
                Announcement = "Welcome, darlings. First night is on the house. Well... almost.",
                IsOpener = true
            },
            new HouseRuleDefinition
            {
                Id = DoubleOrDebtId,
                Announcement = "I'm feeling generous tonight. Every win pays double. So does my appetite.",
                PayoutMultiplier = 2.0,
                QuotaMultiplier = 1.6
            },
            new HouseRuleDefinition
            {
                Id = PowerCutsId,
                Announcement = "The wiring in here is very old. Don't be scared of the dark. Be scared of me.",
                BlackoutIntervalSeconds = 60f
            },
            new HouseRuleDefinition
            {
                Id = HappyHourId,
                Announcement = "Happy hour! Less time, smaller debt. Run, little gamblers, run.",
                FloorTimeMultiplier = 0.7,
                QuotaMultiplier = 0.75
            }
        };
    }
}
