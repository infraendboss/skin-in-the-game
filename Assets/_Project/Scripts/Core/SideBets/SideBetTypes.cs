using System;
using System.Collections.Generic;
using SITG.Core.Run;

namespace SITG.Core.SideBets
{
    public enum SideBetPick { PlayerWins, PlayerLoses }

    public enum PlaceBetResult
    {
        Ok,
        NoSuchRound,
        BettingClosed,
        CannotBetOnYourself,
        AlreadyBet,
        BelowMinimum,
        NotEnoughMoney,
        UnknownPlayer
    }

    public sealed class SideBet
    {
        public PlayerKey Bettor;
        public SideBetPick Pick;
        public long Amount;
    }

    /// <summary>All bets on one round of one player.</summary>
    public sealed class SideBetMarket
    {
        public string RoundKey;
        public string TableId;
        public PlayerKey Subject;
        /// <summary>Game time (seconds) when betting closes. The Game layer passes its clock in.</summary>
        public double ClosesAt;
        public readonly List<SideBet> Bets = new List<SideBet>();

        public bool IsOpen(double now) => now < ClosesAt;
    }

    public sealed class SideBetPayout
    {
        public PlayerKey Bettor;
        public long Amount;
    }

    public sealed class SideBetSettlement
    {
        public string RoundKey;
        public PlayerKey Subject;
        public bool SubjectWon;
        public readonly List<SideBetPayout> Payouts = new List<SideBetPayout>();
    }
}
