using DisplayName.DisplayNameCode.Config;
using DisplayName.DisplayNameCode.Multiplayer;
using DisplayName.DisplayNameCode.Storage;
using HarmonyLib;
using MegaCrit.Sts2.Core.Platform;

namespace DisplayName.DisplayNameCode.Patch;

/// <summary>
/// Substitutes a resolved player name with their DisplayName wherever the game resolves a player name
/// (nameplates, hover tips, etc. all funnel through this single method):
/// - For the local player, always uses this account's own <see cref="DisplayNameConfig.DisplayName"/>.
/// - For remote players, uses whatever they broadcast via <see cref="DisplayNameNetworking"/>, if anything.
/// If nothing is set (empty DisplayName, or no sync message received yet for that player), falls through
/// to the vanilla result untouched, i.e. their Steam username.
/// </summary>
[HarmonyPatch(typeof(PlatformUtil), nameof(PlatformUtil.GetPlayerNameRaw))]
public static class PlatformUtilGetPlayerNameRawPatch
{
    [HarmonyPostfix]
    public static void Postfix(PlatformType platformType, ulong playerId, ref string __result)
    {
        // __result is still whatever vanilla resolved (the Steam username) at this point -- cache it so
        // UI that wants "the underlying platform username" can recover it even after we override
        // __result below (see NMultiplayerPlayerExpandedStateReadyPatch / DisplayNameConfig.ShowSteamIdAlongside).
        RawPlatformNameCache.Set(playerId, __result);

        if (playerId == PlatformUtil.GetLocalPlayerId(platformType))
        {
            string displayName = DisplayNameConfig.DisplayName;
            if (!string.IsNullOrWhiteSpace(displayName))
            {
                __result = displayName;
            }
            return;
        }

        if (RemoteDisplayNames.TryGet(playerId, out string remoteDisplayName) && !string.IsNullOrWhiteSpace(remoteDisplayName))
        {
            __result = remoteDisplayName;
        }
    }
}
