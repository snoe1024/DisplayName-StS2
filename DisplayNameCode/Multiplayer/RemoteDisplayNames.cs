using System;
using System.Collections.Generic;

namespace DisplayName.DisplayNameCode.Multiplayer;

/// <summary>
/// In-memory cache of other players' DisplayNames for the currently active lobby/run, keyed by their net
/// player ID. Populated by <see cref="DisplayNameNetworking"/> as <see cref="DisplayNameSyncMessage"/>s
/// arrive. Only ever holds data for the lifetime of one lobby/run; cleared on disconnect.
/// </summary>
internal static class RemoteDisplayNames
{
    private static readonly Dictionary<ulong, string> Names = new();

    /// <summary>
    /// Raised after a player's cached DisplayName changes, with their player ID. Some UI (e.g. the lobby
    /// nameplate NRemoteLobbyPlayer, and the persistent in-combat HUD NMultiplayerPlayerState) renders a
    /// player's name exactly once when the widget is created and never re-queries PlatformUtil afterward,
    /// so it must subscribe to this to refresh itself when a DisplayNameSyncMessage arrives after that
    /// initial render (which it usually will, since it takes at least one network round-trip and the
    /// widget is typically already on screen by then).
    /// </summary>
    public static event Action<ulong>? Updated;

    public static void Set(ulong playerId, string displayName)
    {
        Names[playerId] = displayName;
        Updated?.Invoke(playerId);
    }

    public static bool TryGet(ulong playerId, out string displayName)
    {
        return Names.TryGetValue(playerId, out displayName!);
    }

    public static void Clear()
    {
        Names.Clear();
    }
}
