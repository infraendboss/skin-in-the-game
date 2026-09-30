using System;
using SITG.Core.Run;
using SITG.Core.Stakes;

namespace SITG.Core.Minigames
{
    public enum MinigamePhase { Waiting, Playing }

    /// <summary>Sent when a player starts a round. Side bets open on this.</summary>
    public sealed class RoundOpened
    {
        /// <summary>Unique per table and round, for example "table_03#7".</summary>
        public string RoundKey;
        public string TableId;
        public PlayerKey Player;
        public Stake Stake;
        public long StakeValue;
    }

    /// <summary>Sent when a round ends. Money, body parts and side bets are settled on this.</summary>
    public sealed class RoundOutcome
    {
        public string RoundKey;
        public string TableId;
        public PlayerKey Player;
        public Stake Stake;
        public long StakeValue;
        public bool Won;
        /// <summary>What the game pays before house rules. Includes the stake value. 0 when lost.</summary>
        public long Payout;
    }

    /// <summary>Every minigame shares these, so side bets and settlement work with all of them.</summary>
    public interface IMinigame
    {
        string TableId { get; }
        string GameId { get; }
        MinigamePhase Phase { get; }
        PlayerKey CurrentPlayer { get; }
        event Action<RoundOpened> RoundOpened;
        event Action<RoundOutcome> RoundResolved;
    }
}
