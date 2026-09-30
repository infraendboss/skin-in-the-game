using System;
using SITG.Core.Economy;
using SITG.Core.HouseRules;
using SITG.Core.Random;
using SITG.Core.Run;
using SITG.Core.Stakes;

namespace SITG.Core.Minigames
{
    public sealed class Settlement
    {
        public RoundOutcome Outcome;
        /// <summary>Money added to the team bank after house rules.</summary>
        public long PaidToTeam;
        public BodyParts PartLost = BodyParts.None;
        /// <summary>Bonus: winning with a body part gives back one part you lost earlier.</summary>
        public BodyParts PartRestored = BodyParts.None;
    }

    /// <summary>
    /// Connects minigames to money and body parts, the same way for every game.
    /// Before a round: take the stake. After a round: pay out or take the body part.
    /// </summary>
    public sealed class RoundSettler
    {
        private readonly RunState _run;
        private readonly EconomyService _economy;
        private readonly StakeService _stakes;
        private readonly HouseRuleSet _rules;
        private readonly Func<SeededRng> _rng;

        public event Action<Settlement> Settled;

        public RoundSettler(RunState run, EconomyService economy, StakeService stakes, HouseRuleSet rules, Func<SeededRng> rng)
        {
            _run = run;
            _economy = economy;
            _stakes = stakes;
            _rules = rules;
            _rng = rng;
        }

        /// <summary>
        /// Takes the stake: money from the team bank, or pledges the body part.
        /// Returns false if not allowed (not enough money, part already gone, ghost).
        /// </summary>
        public bool TryTakeStake(PlayerKey player, Stake stake, out long stakeValue)
        {
            stakeValue = 0;
            var p = _run.Find(player);
            if (p == null || p.IsGhost || !_run.NightInProgress) return false;

            if (stake.IsBodyPart)
            {
                if (!_stakes.CanPledge(player, stake.Part)) return false;
                stakeValue = _stakes.Pledge(player, stake.Part);
                return true;
            }

            if (stake.Money <= 0 || !_economy.TryWithdrawTeam(stake.Money)) return false;
            stakeValue = stake.Money;
            return true;
        }

        /// <summary>Gives the stake back if a round could not start after the stake was taken.</summary>
        public void RefundStake(PlayerKey player, Stake stake)
        {
            if (stake.IsBodyPart) _stakes.ReleasePledge(player, stake.Part);
            else _economy.DepositTeam(stake.Money);
        }

        public Settlement Settle(RoundOutcome outcome)
        {
            var result = new Settlement { Outcome = outcome };

            if (outcome.Won)
            {
                result.PaidToTeam = _rules.ApplyPayout(outcome.Payout);
                _economy.DepositTeam(result.PaidToTeam);
                if (outcome.Stake.IsBodyPart)
                {
                    _stakes.ReleasePledge(outcome.Player, outcome.Stake.Part);
                    result.PartRestored = _stakes.RestoreRandomLostPart(outcome.Player, _rng());
                }
            }
            else if (outcome.Stake.IsBodyPart)
            {
                _stakes.ForfeitPledge(outcome.Player, outcome.Stake.Part);
                result.PartLost = outcome.Stake.Part;
            }

            Settled?.Invoke(result);
            return result;
        }

        /// <summary>Hook a minigame up so every finished round is settled automatically.</summary>
        public void Attach(IMinigame game) => game.RoundResolved += o => Settle(o);
    }
}
