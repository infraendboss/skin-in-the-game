using System.Collections.Generic;
using SITG.Core.Balance;
using SITG.Core.HouseRules;
using SITG.Core.Minigames.HigherLower;
using SITG.Core.Run;

namespace SITG.Tests
{
    /// <summary>A deck that deals the cards you give it, in order. Makes tests predictable.</summary>
    public sealed class FixedDeck : IDeck
    {
        private readonly Queue<int> _cards;
        public FixedDeck(params int[] cards) { _cards = new Queue<int>(cards); }
        public int Draw() => _cards.Dequeue();
    }

    public static class TestData
    {
        public static readonly PlayerKey Anna = new PlayerKey("anna");
        public static readonly PlayerKey Bram = new PlayerKey("bram");
        public static readonly PlayerKey Cas = new PlayerKey("cas");

        public static RunController NewRun(int seed = 42, params PlayerKey[] players)
        {
            if (players.Length == 0) players = new[] { Anna, Bram };
            var list = new List<(PlayerKey, string)>();
            foreach (var p in players) list.Add((p, p.Value));
            return RunController.StartNew(seed, list, new GameBalance(), DefaultHouseRules.All);
        }
    }
}
