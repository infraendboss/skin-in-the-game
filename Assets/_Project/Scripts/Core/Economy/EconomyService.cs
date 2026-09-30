using System;
using SITG.Core.Run;

namespace SITG.Core.Economy
{
    /// <summary>
    /// The only place where money changes. Team bank is shared, pocket money is per player.
    /// Runs on the host only.
    /// </summary>
    public sealed class EconomyService
    {
        private readonly RunState _run;

        public event Action<long> TeamBankChanged;
        public event Action<PlayerKey, long> PocketMoneyChanged;

        public EconomyService(RunState run)
        {
            _run = run ?? throw new ArgumentNullException(nameof(run));
        }

        public long TeamBank => _run.TeamBank;
        public long PocketMoney(PlayerKey player) => _run.Get(player).PocketMoney;

        public bool TryWithdrawTeam(long amount)
        {
            RequirePositive(amount);
            if (_run.TeamBank < amount) return false;
            _run.TeamBank -= amount;
            TeamBankChanged?.Invoke(_run.TeamBank);
            return true;
        }

        public void DepositTeam(long amount)
        {
            if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount));
            if (amount == 0) return;
            _run.TeamBank += amount;
            TeamBankChanged?.Invoke(_run.TeamBank);
        }

        /// <summary>Takes everything that is left. Used when the quota is not met.</summary>
        public long EmptyTeamBank()
        {
            long all = _run.TeamBank;
            _run.TeamBank = 0;
            if (all != 0) TeamBankChanged?.Invoke(0);
            return all;
        }

        public bool TryWithdrawPocket(PlayerKey player, long amount)
        {
            RequirePositive(amount);
            var p = _run.Get(player);
            if (p.PocketMoney < amount) return false;
            p.PocketMoney -= amount;
            PocketMoneyChanged?.Invoke(player, p.PocketMoney);
            return true;
        }

        public void DepositPocket(PlayerKey player, long amount)
        {
            if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount));
            if (amount == 0) return;
            var p = _run.Get(player);
            p.PocketMoney += amount;
            PocketMoneyChanged?.Invoke(player, p.PocketMoney);
        }

        private static void RequirePositive(long amount)
        {
            if (amount <= 0) throw new ArgumentOutOfRangeException(nameof(amount), "Amount must be more than 0.");
        }
    }
}
