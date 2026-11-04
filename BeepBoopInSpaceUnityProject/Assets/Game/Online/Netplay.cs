namespace Game.Online
{
    /// <summary>
    /// Who decides what happens in a game. Offline and on the host of an online room, this machine does: it plays the
    /// game as before and tells the guests what happened. A guest only shows what the host tells it, and sends its
    /// players' buttons to the host: gameplay code that decides something (a death, a random cell, a score, the next
    /// state) runs only where <see cref="HasAuthority"/> is true.
    /// </summary>
    public static class Netplay
    {
        /// <summary>In an online room, as its host or as a guest.</summary>
        public static bool IsOnline => OnlineManager.Instance && OnlineManager.Instance.IsInRoom;

        public static bool IsHost => IsOnline && OnlineManager.Instance.IsHost;

        /// <summary>
        /// A guest of an online room: the host plays the game, this machine shows it. Still true after losing the host
        /// in a game, until the main menu: the game being left mustn't start deciding things on its own.
        /// </summary>
        public static bool IsGuest => OnlineManager.Instance && OnlineManager.Instance.IsGuest;

        /// <summary>This machine decides what happens in the game: offline, or as the host.</summary>
        public static bool HasAuthority => !IsGuest;
    }
}
