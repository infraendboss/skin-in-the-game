using System;
using SITG.Core.Stakes;

namespace SITG.Core.Balance
{
    /// <summary>
    /// All tuning numbers in one place. Change these after playtests, not the code.
    /// In Unity a ScriptableObject in the Game layer can hold one of these so you can edit it in the Inspector.
    /// </summary>
    [Serializable]
    public sealed class GameBalance
    {
        // --- Run ---
        public long StartingTeamBank = 300;
        public long StartingPocketMoney = 50;
        public long[] BaseQuotaPerNight = { 500, 1000, 1800, 3000, 5000 };
        public float FloorTimeSeconds = 300f;

        // --- Body parts (value at night 1) ---
        public long VoiceValue = 150;
        public long EyesValue = 200;
        public long HandsValue = 250;
        public long LegsValue = 200;
        /// <summary>Body parts are worth more every night. 0.5 means +50% per night.</summary>
        public double BodyPartValueGrowthPerNight = 0.5;

        // --- Higher or Lower ---
        /// <summary>Fraction of fair odds that gets paid out. 0.95 means the house keeps 5%.</summary>
        public double HigherLowerHouseEdge = 0.95;

        // --- Side bets ---
        public float SideBetWindowSeconds = 6f;
        public double SideBetPayoutMultiplier = 2.0;
        public long MinSideBet = 5;
        /// <summary>Pocket money a ghost receives at the start of each night, so they can keep betting.</summary>
        public long GhostNightlyAllowance = 25;

        public int NightCount => BaseQuotaPerNight.Length;

        public long BodyPartValue(BodyParts part, int nightIndex)
        {
            long baseValue;
            switch (part)
            {
                case BodyParts.Voice: baseValue = VoiceValue; break;
                case BodyParts.Eyes: baseValue = EyesValue; break;
                case BodyParts.Hands: baseValue = HandsValue; break;
                case BodyParts.Legs: baseValue = LegsValue; break;
                default: throw new ArgumentException("Value is only defined for a single body part.", nameof(part));
            }
            double factor = 1.0 + BodyPartValueGrowthPerNight * Math.Max(0, nightIndex);
            return (long)Math.Floor(baseValue * factor);
        }
    }
}
