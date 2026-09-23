using DisplayName.DisplayNameCode.Config;
using DisplayName.DisplayNameCode.Multiplayer;
using HarmonyLib;
using MegaCrit.Sts2.Core.Platform;

namespace DisplayName.DisplayNameCode.Patch;

/// <summary>
/// Substitutes a resolved player name with their DisplayName wherever the game resolves a player name
/// (nameplates, hover tips, etc. all funnel through this single method):
/// - For the local player, always uses this account's own <see cref="DisplayNameConfig.DisplayName"/>.
/// - For remote players, uses whatever they broadcast via <see cref="DisplayNameNetworking"/>, if anything,
///   but ONLY while a lobby/run is actually active (<see cref="DisplayNameNetworking.HasActiveLobby"/>).
///   Outside of that (e.g. the main menu's "Join Friend" list, which resolves a friend's name before any
///   lobby for THIS session exists) a Steam username is what correctly identifies who you're about to
///   connect to; showing a DisplayName cached from a past session there would be misleading.
/// If nothing is set (empty DisplayName, or no sync message received yet for that player), falls through
/// to the vanilla result untouched, i.e. their Steam username.
///
/// [HarmonyPriority(Priority.Last)]: if another mod also patches this exact method with a postfix at the
/// default priority, Harmony's patch-application order (which this mod doesn't control) decides which
/// postfix runs last and therefore wins. Priority.Last makes our postfix the outermost one regardless of
/// application order, so whatever we set __result to is what every caller actually sees.
/// </summary>
[HarmonyPatch(typeof(PlatformUtil), nameof(PlatformUtil.GetPlayerNameRaw))]
public static class PlatformUtilGetPlayerNameRawPatch
{
    [HarmonyPostfix]
    [HarmonyPriority(Priority.Last)]
    public static void Postfix(PlatformType platformType, ulong playerId, ref string __result)
    {
        ulong localPlayerId = PlatformUtil.GetLocalPlayerId(platformType);
        if (playerId == localPlayerId)
        {
            string displayName = DisplayNameConfig.DisplayName;
            if (!string.IsNullOrWhiteSpace(displayName))
            {
                __result = displayName;
            }
            return;
        }

        if (!DisplayNameNetworking.HasActiveLobby)
        {
            return;
        }

        if (RemoteDisplayNames.TryGet(playerId, out string remoteDisplayName) && !string.IsNullOrWhiteSpace(remoteDisplayName))
        {
            __result = remoteDisplayName;
        }
    }
}
