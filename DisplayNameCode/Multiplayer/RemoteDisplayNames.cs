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

    public static void Set(ulong playerId, string displayName)
    {
        Names[playerId] = displayName;
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
