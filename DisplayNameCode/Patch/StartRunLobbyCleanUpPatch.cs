using DisplayName.DisplayNameCode.Multiplayer;
using HarmonyLib;
using MegaCrit.Sts2.Core.Multiplayer.Game.Lobby;

namespace DisplayName.DisplayNameCode.Patch;

[HarmonyPatch(typeof(StartRunLobby), nameof(StartRunLobby.CleanUp))]
public static class StartRunLobbyCleanUpPatch
{
    [HarmonyPostfix]
    public static void Postfix(StartRunLobby __instance, bool disconnectSession)
    {
        DisplayNameNetworking.OnLobbyCleanedUp(__instance, disconnectSession);
    }
}
