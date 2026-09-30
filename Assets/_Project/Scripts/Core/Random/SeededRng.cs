using System;
using System.Collections.Generic;

namespace SITG.Core.Random
{
    /// <summary>
    /// Deterministic random number generator (PCG32).
    /// The same seed always gives the same numbers, on every computer.
    /// Never use UnityEngine.Random for game logic, always use this.
    /// </summary>
    public sealed class SeededRng
    {
        private const ulong Multiplier = 6364136223846793005UL;
        private const ulong Increment = 1442695040888963407UL;

        private ulong _state;

        public SeededRng(ulong seed)
        {
            _state = 0UL;
            NextUInt();
            _state = unchecked(_state + seed);
            NextUInt();
        }

        private SeededRng() { }

        /// <summary>Continue exactly where a saved generator left off.</summary>
        public static SeededRng FromState(ulong state)
        {
            return new SeededRng { _state = state };
        }

        /// <summary>The internal state. Save this to resume the same sequence later.</summary>
        public ulong State => _state;

        public uint NextUInt()
        {
            ulong old = _state;
            _state = unchecked(old * Multiplier + Increment);
            uint xorShifted = (uint)(((old >> 18) ^ old) >> 27);
            int rotation = (int)(old >> 59);
            return (xorShifted >> rotation) | (xorShifted << ((-rotation) & 31));
        }

        /// <summary>Random int from 0 up to (not including) maxExclusive, without bias.</summary>
        public int NextInt(int maxExclusive)
        {
            if (maxExclusive <= 0) throw new ArgumentOutOfRangeException(nameof(maxExclusive));
            uint bound = (uint)maxExclusive;
            uint threshold = unchecked(0u - bound) % bound;
            while (true)
            {
                uint r = NextUInt();
                if (r >= threshold) return (int)(r % bound);
            }
        }

        /// <summary>Random int from minInclusive up to (not including) maxExclusive.</summary>
        public int NextInt(int minInclusive, int maxExclusive)
        {
            if (maxExclusive <= minInclusive) throw new ArgumentOutOfRangeException(nameof(maxExclusive));
            return minInclusive + NextInt(maxExclusive - minInclusive);
        }

        /// <summary>Random number from 0.0 up to (not including) 1.0.</summary>
        public double NextDouble() => NextUInt() / 4294967296.0;

        /// <summary>True with the given probability (0.0 to 1.0).</summary>
        public bool Chance(double probability) => NextDouble() < probability;

        public T Pick<T>(IReadOnlyList<T> items)
        {
            if (items == null || items.Count == 0) throw new ArgumentException("Cannot pick from an empty list.", nameof(items));
            return items[NextInt(items.Count)];
        }

        public void Shuffle<T>(IList<T> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = NextInt(i + 1);
                T tmp = list[i];
                list[i] = list[j];
                list[j] = tmp;
            }
        }
    }
}
