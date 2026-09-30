using SITG.Core.Random;

namespace SITG.Core.Minigames.HigherLower
{
    /// <summary>Card source: returns values 1 (ace) to 13 (king).</summary>
    public interface IDeck
    {
        int Draw();
    }

    /// <summary>An endless deck: every card is equally likely every time.</summary>
    public sealed class InfiniteDeck : IDeck
    {
        public const int Lowest = 1;
        public const int Highest = 13;
        private readonly SeededRng _rng;

        public InfiniteDeck(SeededRng rng) { _rng = rng; }

        public int Draw() => _rng.NextInt(Lowest, Highest + 1);
    }
}
