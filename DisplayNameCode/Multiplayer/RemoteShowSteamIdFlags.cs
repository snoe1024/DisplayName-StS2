using System.Collections.Generic;

namespace DisplayName.DisplayNameCode.Multiplayer;

/// <summary>
/// Per-player cache of whether THAT player consents to having their Steam username shown alongside their
/// DisplayName (their own DisplayNameConfig.ShowSteamIdAlongside value, broadcast via
/// <see cref="DisplayNameSyncMessage"/>). Deliberately separate from the local viewer's own config: this
/// is the subject's privacy preference, not the viewer's. Missing/false is the safe default (hidden).
/// </summary>
internal static class RemoteShowSteamIdFlags
{
    private static readonly Dictionary<ulong, bool> Flags = new();

    public static void Set(ulong playerId, bool showSteamIdAlongside)
    {
        Flags[playerId] = showSteamIdAlongside;
    }

    public static bool Get(ulong playerId)
    {
        return Flags.TryGetValue(playerId, out bool value) && value;
    }

    public static void Clear()
    {
        Flags.Clear();
    }
}
