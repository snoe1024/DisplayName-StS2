using DisplayName.DisplayNameCode.Multiplayer;
using HarmonyLib;
using MegaCrit.Sts2.Core.Multiplayer.Game.Lobby;

namespace DisplayName.DisplayNameCode.Patch;

// CleanUp(disconnectSession: false) means the lobby is merely handing off to a run that's starting (same
// NetService instance carries over), so only disconnectSession: true should tear down the sync registrations.
[HarmonyPatch(typeof(StartRunLobby), nameof(StartRunLobby.CleanUp))]
public static class StartRunLobbyCleanUpPatch
{
    [HarmonyPostfix]
    public static void Postfix(StartRunLobby __instance, bool disconnectSession)
    {
        DisplayNameNetworking.OnLobbyCleanedUp(__instance, disconnectSession);
    }
}
