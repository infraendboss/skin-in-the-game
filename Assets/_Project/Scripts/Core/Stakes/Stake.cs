using System;

namespace SITG.Core.Stakes
{
    /// <summary>What a player puts on the table: either money from the team bank, or one body part.</summary>
    [Serializable]
    public struct Stake
    {
        public long Money;
        public BodyParts Part;

        public bool IsBodyPart => Part != BodyParts.None;

        public static Stake OfMoney(long amount)
        {
            if (amount <= 0) throw new ArgumentOutOfRangeException(nameof(amount), "A money stake must be more than 0.");
            return new Stake { Money = amount, Part = BodyParts.None };
        }

        public static Stake OfBodyPart(BodyParts part)
        {
            if (!part.IsSingle()) throw new ArgumentException("A body part stake must be exactly one body part.", nameof(part));
            return new Stake { Money = 0, Part = part };
        }

        public override string ToString() => IsBodyPart ? Part.ToString() : Money.ToString();
    }
}
