using System;
using System.Collections.Generic;
using SITG.Core.Balance;
using SITG.Core.Random;
using SITG.Core.Run;

namespace SITG.Core.Stakes
{
    /// <summary>
    /// Handles betting, losing and winning back body parts. Runs on the host only.
    /// A pledged part stays with the player until the round is lost, so effects only start after losing.
    /// </summary>
    public sealed class StakeService
    {
        private readonly RunState _run;
        private readonly GameBalance _balance;

        public event Action<PlayerKey, BodyParts> PartLost;
        public event Action<PlayerKey, BodyParts> PartRestored;
        public event Action<PlayerKey> BecameGhost;

        public StakeService(RunState run, GameBalance balance)
        {
            _run = run ?? throw new ArgumentNullException(nameof(run));
            _balance = balance ?? throw new ArgumentNullException(nameof(balance));
        }

        public bool CanPledge(PlayerKey player, BodyParts part)
        {
            var p = _run.Find(player);
            return p != null && part.IsSingle() && p.Parts.Has(part) && !p.PledgedParts.Has(part);
        }

        /// <summary>What a body part is worth tonight, in money.</summary>
        public long ValueOf(BodyParts part) => _balance.BodyPartValue(part, _run.NightIndex);

        /// <summary>Puts a body part on the table. Returns its money value.</summary>
        public long Pledge(PlayerKey player, BodyParts part)
        {
            if (!CanPledge(player, part)) throw new InvalidOperationException($"{player} cannot pledge {part}.");
            _run.Get(player).PledgedParts |= part;
            return ValueOf(part);
        }

        /// <summary>Round won: the pledged part is safe again.</summary>
        public void ReleasePledge(PlayerKey player, BodyParts part)
        {
            var p = _run.Get(player);
            RequirePledged(p, part);
            p.PledgedParts &= ~part;
        }

        /// <summary>Round lost: the pledged part is gone.</summary>
        public void ForfeitPledge(PlayerKey player, BodyParts part)
        {
            var p = _run.Get(player);
            RequirePledged(p, part);
            p.PledgedParts &= ~part;
            RemovePart(p, part);
        }

        /// <summary>Gives back one random part the player has lost. Returns None if nothing was lost.</summary>
        public BodyParts RestoreRandomLostPart(PlayerKey player, SeededRng rng)
        {
            var p = _run.Get(player);
            var lost = p.LostParts.ToList();
            if (lost.Count == 0) return BodyParts.None;
            var part = rng.Pick(lost);
            p.Parts |= part;
            PartRestored?.Invoke(player, part);
            return part;
        }

        /// <summary>Quota missed: the house takes one random unpledged part from every living player.</summary>
        public List<PartTaken> CollectShortfall(SeededRng rng)
        {
            var taken = new List<PartTaken>();
            foreach (var p in _run.Players)
            {
                if (p.IsGhost) continue;
                var available = (p.Parts & ~p.PledgedParts).ToList();
                if (available.Count == 0) available = p.Parts.ToList();
                var part = rng.Pick(available);
                p.PledgedParts &= ~part;
                RemovePart(p, part);
                taken.Add(new PartTaken(p.Key, part));
            }
            return taken;
        }

        /// <summary>Cancels all open pledges, for example when a night ends mid-round.</summary>
        public void ClearAllPledges()
        {
            foreach (var p in _run.Players) p.PledgedParts = BodyParts.None;
        }

        private void RemovePart(PlayerState p, BodyParts part)
        {
            p.Parts &= ~part;
            PartLost?.Invoke(p.Key, part);
            if (p.IsGhost) BecameGhost?.Invoke(p.Key);
        }

        private static void RequirePledged(PlayerState p, BodyParts part)
        {
            if (!p.PledgedParts.Has(part)) throw new InvalidOperationException($"{p.Key} has not pledged {part}.");
        }
    }
}
