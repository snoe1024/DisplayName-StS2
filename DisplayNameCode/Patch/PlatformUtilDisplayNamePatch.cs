using DisplayName.DisplayNameCode.Config;
using DisplayName.DisplayNameCode.Multiplayer;
using HarmonyLib;
using MegaCrit.Sts2.Core.Platform;

namespace DisplayName.DisplayNameCode.Patch;

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
