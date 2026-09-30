using System;
using SITG.Core.Run;

namespace SITG.Core.Stakes
{
    /// <summary>Record of the house taking a body part from a player.</summary>
    [Serializable]
    public sealed class PartTaken
    {
        public PlayerKey Player;
        public BodyParts Part;

        public PartTaken(PlayerKey player, BodyParts part)
        {
            Player = player;
            Part = part;
        }
    }
}
