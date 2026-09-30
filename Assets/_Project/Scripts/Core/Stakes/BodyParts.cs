using System;
using System.Collections.Generic;

namespace SITG.Core.Stakes
{
    /// <summary>The body parts a player can bet. Stored as flags so one number holds all of them.</summary>
    [Flags]
    public enum BodyParts
    {
        None = 0,
        Voice = 1,
        Eyes = 2,
        Hands = 4,
        Legs = 8,
        All = Voice | Eyes | Hands | Legs
    }

    public static class BodyPartsExtensions
    {
        /// <summary>Every single body part, in a fixed order.</summary>
        public static readonly BodyParts[] Individual = { BodyParts.Voice, BodyParts.Eyes, BodyParts.Hands, BodyParts.Legs };

        public static bool Has(this BodyParts set, BodyParts part) => part != BodyParts.None && (set & part) == part;

        /// <summary>True if this value is exactly one body part (not None, not a combination).</summary>
        public static bool IsSingle(this BodyParts part)
        {
            int v = (int)part;
            return v != 0 && (v & (v - 1)) == 0 && (part & ~BodyParts.All) == 0;
        }

        public static int Count(this BodyParts set)
        {
            int count = 0;
            foreach (var p in Individual) if (set.Has(p)) count++;
            return count;
        }

        public static List<BodyParts> ToList(this BodyParts set)
        {
            var list = new List<BodyParts>();
            foreach (var p in Individual) if (set.Has(p)) list.Add(p);
            return list;
        }
    }
}
