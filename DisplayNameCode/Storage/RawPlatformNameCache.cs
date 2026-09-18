using System.Collections.Generic;

namespace DisplayName.DisplayNameCode.Storage;

/// <summary>
/// Caches the platform-resolved name (e.g. Steam username) for a player exactly as vanilla
/// <see cref="MegaCrit.Sts2.Core.Platform.PlatformUtil.GetPlayerNameRaw"/> would return it, BEFORE
/// PlatformUtilGetPlayerNameRawPatch overwrites the result with a DisplayName. Used by the
/// "show Steam username alongside" feature to recover the underlying username even though this mod's own
/// patch has changed what that same method now returns.
/// </summary>
internal static class RawPlatformNameCache
{
    private static readonly Dictionary<ulong, string> Names = new();

    public static void Set(ulong playerId, string rawName)
    {
        Names[playerId] = rawName;
    }

    public static bool TryGet(ulong playerId, out string rawName)
    {
        return Names.TryGetValue(playerId, out rawName!);
    }
}
