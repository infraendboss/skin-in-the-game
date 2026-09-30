using System;
using System.Collections.Generic;
using SITG.Core.Balance;
using SITG.Core.Economy;
using SITG.Core.Minigames;
using SITG.Core.Run;

namespace SITG.Core.SideBets
{
    /// <summary>
    /// Side bets on other players' rounds. Paid from and to pocket money, never the team bank.
    /// Ghosts can bet too. Tracks who bet most against their friends this night.
    /// </summary>
    public sealed class SideBetBook
    {
        private readonly RunState _run;
        private readonly EconomyService _economy;
        private readonly GameBalance _balance;
        private readonly Dictionary<string, SideBetMarket> _open = new Dictionary<string, SideBetMarket>();
        private readonly Dictionary<PlayerKey, long> _betAgainstTonight = new Dictionary<PlayerKey, long>();

        public event Action<SideBetMarket> MarketOpened;
        public event Action<SideBetMarket, SideBet> BetPlaced;
        public event Action<SideBetSettlement> MarketSettled;

        public SideBetBook(RunState run, EconomyService economy, GameBalance balance)
        {
            _run = run;
            _economy = economy;
            _balance = balance;
        }

        public IEnumerable<SideBetMarket> OpenMarkets => _open.Values;

        public SideBetMarket GetMarket(string roundKey) => _open.TryGetValue(roundKey, out var m) ? m : null;

        public SideBetMarket Open(RoundOpened round, double now)
        {
            var market = new SideBetMarket
            {
                RoundKey = round.RoundKey,
                TableId = round.TableId,
                Subject = round.Player,
                ClosesAt = now + _balance.SideBetWindowSeconds
            };
            _open[round.RoundKey] = market;
            MarketOpened?.Invoke(market);
            return market;
        }

        public PlaceBetResult Place(string roundKey, PlayerKey bettor, SideBetPick pick, long amount, double now)
        {
            if (!_open.TryGetValue(roundKey, out var market)) return PlaceBetResult.NoSuchRound;
            if (!market.IsOpen(now)) return PlaceBetResult.BettingClosed;
            if (_run.Find(bettor) == null) return PlaceBetResult.UnknownPlayer;
            if (bettor == market.Subject) return PlaceBetResult.CannotBetOnYourself;
            foreach (var b in market.Bets) if (b.Bettor == bettor) return PlaceBetResult.AlreadyBet;
            if (amount < _balance.MinSideBet) return PlaceBetResult.BelowMinimum;
            if (!_economy.TryWithdrawPocket(bettor, amount)) return PlaceBetResult.NotEnoughMoney;

            var bet = new SideBet { Bettor = bettor, Pick = pick, Amount = amount };
            market.Bets.Add(bet);

            if (pick == SideBetPick.PlayerLoses)
            {
                _betAgainstTonight.TryGetValue(bettor, out long sum);
                _betAgainstTonight[bettor] = sum + amount;
            }

            BetPlaced?.Invoke(market, bet);
            return PlaceBetResult.Ok;
        }

        /// <summary>Pays winning bets. Unknown rounds (nobody bet) are ignored.</summary>
        public SideBetSettlement Settle(RoundOutcome outcome)
        {
            if (!_open.TryGetValue(outcome.RoundKey, out var market)) return null;
            _open.Remove(outcome.RoundKey);

            var settlement = new SideBetSettlement
            {
                RoundKey = outcome.RoundKey,
                Subject = market.Subject,
                SubjectWon = outcome.Won
            };
            var winningPick = outcome.Won ? SideBetPick.PlayerWins : SideBetPick.PlayerLoses;

            foreach (var bet in market.Bets)
            {
                if (bet.Pick != winningPick) continue;
                long payout = (long)Math.Floor(bet.Amount * _balance.SideBetPayoutMultiplier);
                _economy.DepositPocket(bet.Bettor, payout);
                settlement.Payouts.Add(new SideBetPayout { Bettor = bet.Bettor, Amount = payout });
            }

            MarketSettled?.Invoke(settlement);
            return settlement;
        }

        /// <summary>The player who bet the most against their friends tonight, or null if nobody did.</summary>
        public PlayerKey? TopBetterAgainstTeam()
        {
            PlayerKey? top = null;
            long best = 0;
            foreach (var kv in _betAgainstTonight)
            {
                if (kv.Value > best) { best = kv.Value; top = kv.Key; }
            }
            return top;
        }

        /// <summary>Hook a minigame up so markets open and settle automatically. Pass the game clock.</summary>
        public void Attach(IMinigame game, Func<double> clock)
        {
            game.RoundOpened += r => Open(r, clock());
            game.RoundResolved += o => Settle(o);
        }

        /// <summary>Clears everything at the start of a night. Open bets are refunded.</summary>
        public void ResetNight()
        {
            foreach (var market in _open.Values)
                foreach (var bet in market.Bets)
                    _economy.DepositPocket(bet.Bettor, bet.Amount);
            _open.Clear();
            _betAgainstTonight.Clear();
        }
    }
}
