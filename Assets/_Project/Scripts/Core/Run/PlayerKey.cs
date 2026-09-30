using System;

namespace SITG.Core.Run
{
    /// <summary>
    /// A player's permanent ID (from Unity Authentication).
    /// Used to recognise a player who reconnects, so they get their money and body parts back.
    /// </summary>
    [Serializable]
    public struct PlayerKey : IEquatable<PlayerKey>
    {
        public string Value;

        public PlayerKey(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("A player key cannot be empty.", nameof(value));
            Value = value;
        }

        public bool IsValid => !string.IsNullOrEmpty(Value);

        public bool Equals(PlayerKey other) => string.Equals(Value, other.Value, StringComparison.Ordinal);
        public override bool Equals(object obj) => obj is PlayerKey other && Equals(other);
        public override int GetHashCode() => Value == null ? 0 : StringComparer.Ordinal.GetHashCode(Value);
        public override string ToString() => Value ?? "<none>";

        public static bool operator ==(PlayerKey a, PlayerKey b) => a.Equals(b);
        public static bool operator !=(PlayerKey a, PlayerKey b) => !a.Equals(b);
    }
}
