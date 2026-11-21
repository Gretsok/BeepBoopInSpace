using System;
using System.IO;
using System.Text;

namespace Game.Online
{
    /// <summary>What a message between the machines of a room says. Its first byte, before its content.</summary>
    public enum EOnlineMessage : byte
    {
        // Room, host to guests.
        Roster = 1,
        LaunchTraining = 2,
        LaunchTournament = 3,
        TournamentNextGame = 4,
        RoomClosed = 5,
        // What a guest's machine sends with its connection: version, robots of its players.
        JoinRequest = 6,

        // Room, guest to host.
        GameLoaded = 10,

        // Game, host to guests: what happened, in the order it happened.
        FlowState = 20,
        Move = 21,
        Bump = 22,
        Turn = 23,
        Face = 24,
        Teleport = 25,
        Detach = 26,
        Slide = 27,
        Kill = 28,
        Resurrect = 29,
        RespawnTarget = 30,
        Shot = 31,
        Meteorite = 32,
        Score = 33,
        Objective = 34,
        PointCollected = 35,
        Round = 36,
        Timer = 37,
        TutorialStatus = 38,
        RoundWon = 39,

        // Game, guest to host: what its players pressed.
        PlayerAction = 60,
        TutorialStatusRequest = 61,
    }

    /// <summary>Writes and reads the content of the room's messages: a type byte, then values in a fixed order.</summary>
    public static class OnlineMessage
    {
        public static byte[] Write(EOnlineMessage type, Action<BinaryWriter> writeContent = null)
        {
            using var stream = new MemoryStream(16);
            using (var writer = new BinaryWriter(stream, Encoding.UTF8, true))
            {
                writer.Write((byte)type);
                writeContent?.Invoke(writer);
            }
            return stream.ToArray();
        }

        public static EOnlineMessage TypeOf(byte[] message) => (EOnlineMessage)message[0];

        /// <summary>A reader placed after the type byte.</summary>
        public static BinaryReader Read(byte[] message)
        {
            var reader = new BinaryReader(new MemoryStream(message, false), Encoding.UTF8);
            reader.ReadByte();
            return reader;
        }

        /// <summary>Whether guests buffer the message until their game is loaded, rather than handle it right away.</summary>
        public static bool IsGameMessage(EOnlineMessage type) => (byte)type >= (byte)EOnlineMessage.FlowState;
    }
}
