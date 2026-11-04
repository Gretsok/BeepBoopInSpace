using System.IO;

namespace Game.Online
{
    /// <summary>A player of an online room. The room lists them in the order of the games: badges J1 to J4, robots, starting cells.</summary>
    public struct RoomPlayer
    {
        /// <summary>The machine the player plays on (Netcode's client id; the host's is 0).</summary>
        public ulong MachineId;
        /// <summary>The player's rank among the players of that machine, in its PlayerManager.</summary>
        public byte LocalIndex;
        /// <summary>The player's robot, in the character bank.</summary>
        public byte CharacterIndex;

        public void Write(BinaryWriter writer)
        {
            writer.Write(MachineId);
            writer.Write(LocalIndex);
            writer.Write(CharacterIndex);
        }

        public static RoomPlayer Read(BinaryReader reader) => new()
        {
            MachineId = reader.ReadUInt64(),
            LocalIndex = reader.ReadByte(),
            CharacterIndex = reader.ReadByte(),
        };
    }
}
