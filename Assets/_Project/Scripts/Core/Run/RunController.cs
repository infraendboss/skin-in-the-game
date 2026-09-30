using System;
using System.Collections.Generic;
using SITG.Core.Balance;
using SITG.Core.Economy;
using SITG.Core.HouseRules;
using SITG.Core.Minigames;
using SITG.Core.Random;
using SITG.Core.SideBets;
using SITG.Core.Stakes;

namespace SITG.Core.Run
{
    /// <summary>
    /// Runs one complete run: start, nights, settlement, end. Runs on the host only.
    /// Order per night: (save) -> BeginNight -> play -> EndNight -> (save) -> BeginNight ...
    /// Save the State whenever NightInProgress is false; Resume continues from there.
    /// </summary>
    public sealed class RunController
    {
        private readonly Dictionary<string, HouseRuleDefinition> _rulesById = new Dictionary<string, HouseRuleDefinition>();

        public RunState State { get; }
        public GameBalance Balance { get; }
        public SeededRng Rng { get; private set; }
        public EconomyService Economy { get; }
        public StakeService Stakes { get; }
        public SideBetBook SideBets { get; }
        public HouseRuleSet Rules { get; }
        public RoundSettler Settler { get; }

        public event Action<NightInfo> NightBegan;
        public event Action<NightResult> NightEnded;

        private RunController(RunState state, GameBalance balance, IReadOnlyList<HouseRuleDefinition> ruleCatalog)
        {
            State = state;
            Balance = balance;
            foreach (var r in ruleCatalog) _rulesById[r.Id] = r;

            Rng = SeededRng.FromState(state.NightStartRngState);
            Economy = new EconomyService(state);
            Stakes = new StakeService(state, balance);
            SideBets = new SideBetBook(state, Economy, balance);
            Rules = new HouseRuleSet(FindRule(state.HouseRuleIdPerNight.Length > 0 ? state.CurrentHouseRuleId : ruleCatalog[0].Id));
            Settler = new RoundSettler(state, Economy, Stakes, Rules, () => Rng);
        }

        /// <summary>Creates a brand new run. Quotas and house rules for every night are decided now, from the seed.</summary>
        public static RunController StartNew(int seed, IEnumerable<(PlayerKey key, string name)> players,
            GameBalance balance, IReadOnlyList<HouseRuleDefinition> ruleCatalog)
        {
            if (ruleCatalog == null || ruleCatalog.Count == 0) throw new ArgumentException("Need at least one house rule.", nameof(ruleCatalog));
            var rng = new SeededRng(unchecked((ulong)seed));
            int nights = balance.NightCount;

            var openers = new List<HouseRuleDefinition>();
            var others = new List<HouseRuleDefinition>();
            foreach (var r in ruleCatalog) (r.IsOpener ? openers : others).Add(r);
            if (openers.Count == 0) openers.AddRange(ruleCatalog);
            if (others.Count == 0) others.AddRange(ruleCatalog);

            var ruleIds = new string[nights];
            var quotas = new long[nights];
            string previous = null;
            for (int n = 0; n < nights; n++)
            {
                var pool = n == 0 ? openers : others;
                var rule = rng.Pick(pool);
                // Avoid the same rule two nights in a row when there is a choice.
                if (pool.Count > 1)
                    while (rule.Id == previous) rule = rng.Pick(pool);
                ruleIds[n] = rule.Id;
                previous = rule.Id;
                quotas[n] = (long)Math.Ceiling(balance.BaseQuotaPerNight[n] * rule.QuotaMultiplier);
            }

            var state = new RunState
            {
                Seed = seed,
                NightIndex = 0,
                TeamBank = balance.StartingTeamBank,
                QuotaPerNight = quotas,
                HouseRuleIdPerNight = ruleIds,
                NightStartRngState = rng.State
            };

            var controller = new RunController(state, balance, ruleCatalog);
            foreach (var (key, name) in players) controller.AddPlayer(key, name);
            return controller;
        }

        /// <summary>Continues a saved run. The save must be made while no night was in progress.</summary>
        public static RunController Resume(RunState saved, GameBalance balance, IReadOnlyList<HouseRuleDefinition> ruleCatalog)
        {
            if (saved == null) throw new ArgumentNullException(nameof(saved));
            if (saved.NightInProgress) throw new InvalidOperationException("Can only resume from a save made between nights.");
            foreach (var p in saved.Players)
            {
                p.PledgedParts = BodyParts.None;
                p.IsConnected = false;
            }
            return new RunController(saved, balance, ruleCatalog);
        }

        /// <summary>
        /// Adds a new player, or returns the existing one when someone rejoins with the same key.
        /// </summary>
        public PlayerState AddPlayer(PlayerKey key, string displayName)
        {
            var existing = State.Find(key);
            if (existing != null)
            {
                existing.IsConnected = true;
                if (!string.IsNullOrEmpty(displayName)) existing.DisplayName = displayName;
                return existing;
            }

            var p = new PlayerState
            {
                Key = key,
                DisplayName = displayName,
                PocketMoney = Balance.StartingPocketMoney,
                IsConnected = true
            };
            State.Players.Add(p);
            return p;
        }

        public void MarkDisconnected(PlayerKey key)
        {
            var p = State.Find(key);
            if (p != null) p.IsConnected = false;
        }

        public NightInfo BeginNight()
        {
            if (State.Result != RunResult.InProgress) throw new InvalidOperationException("The run is over.");
            if (State.NightInProgress) throw new InvalidOperationException("A night is already in progress.");

            // Reset the random generator to the saved point, so a resumed night plays out from the same state.
            Rng = SeededRng.FromState(State.NightStartRngState);
            Rules.SetActive(FindRule(State.CurrentHouseRuleId));
            SideBets.ResetNight();
            Stakes.ClearAllPledges();

            foreach (var p in State.Players)
                if (p.IsGhost) Economy.DepositPocket(p.Key, Balance.GhostNightlyAllowance);

            State.NightInProgress = true;

            var info = new NightInfo
            {
                NightIndex = State.NightIndex,
                NightCount = State.NightCount,
                Quota = State.CurrentQuota,
                FloorTimeSeconds = Rules.ApplyFloorTime(Balance.FloorTimeSeconds),
                HouseRule = Rules.Active
            };
            NightBegan?.Invoke(info);
            return info;
        }

        /// <summary>
        /// Settles the night. Abort all open minigame rounds before calling this.
        /// </summary>
        public NightResult EndNight()
        {
            if (!State.NightInProgress) throw new InvalidOperationException("No night in progress.");

            var result = new NightResult
            {
                NightIndex = State.NightIndex,
                Quota = State.CurrentQuota,
                TopBetterAgainstTeam = SideBets.TopBetterAgainstTeam()
            };
            SideBets.ResetNight();
            Stakes.ClearAllPledges();

            if (Economy.TeamBank >= State.CurrentQuota)
            {
                result.QuotaMet = true;
                result.MoneyCollected = State.CurrentQuota;
                if (State.CurrentQuota > 0) Economy.TryWithdrawTeam(State.CurrentQuota);
            }
            else
            {
                result.QuotaMet = false;
                result.MoneyCollected = Economy.EmptyTeamBank();
                result.PartsTaken.AddRange(Stakes.CollectShortfall(Rng));
            }

            State.NightInProgress = false;

            if (State.AllGhosts) State.Result = RunResult.Lost;
            else if (State.IsLastNight) State.Result = result.QuotaMet ? RunResult.Won : RunResult.Lost;
            else
            {
                State.NightIndex++;
                State.NightStartRngState = Rng.State;
            }

            result.Result = State.Result;
            NightEnded?.Invoke(result);
            return result;
        }

        private HouseRuleDefinition FindRule(string id)
        {
            if (!_rulesById.TryGetValue(id, out var rule)) throw new KeyNotFoundException($"Unknown house rule '{id}'.");
            return rule;
        }
    }
}
