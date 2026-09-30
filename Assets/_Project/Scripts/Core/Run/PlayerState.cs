using System;
using SITG.Core.Stakes;

namespace SITG.Core.Run
{
    /// <summary>Everything the game remembers about one player during a run.</summary>
    [Serializable]
    public sealed class PlayerState
    {
        public PlayerKey Key;
        public string DisplayName;
        public long PocketMoney;

        /// <summary>Body parts the player still has.</summary>
        public BodyParts Parts = BodyParts.All;

        /// <summary>Body parts currently on the table in an unfinished round. The player still has them until they lose.</summary>
        public BodyParts PledgedParts = BodyParts.None;

        public CosmeticLoadout Cosmetics = new CosmeticLoadout();

        /// <summary>Only used while playing, never saved.</summary>
        [NonSerialized] public bool IsConnected;

        public bool IsGhost => Parts == BodyParts.None;
        public BodyParts LostParts => BodyParts.All & ~Parts;
    }

    [Serializable]
    public sealed class CosmeticLoadout
    {
        public string HatId = "";
        public string ColorId = "";
    }
}
