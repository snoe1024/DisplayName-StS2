using DisplayName.DisplayNameCode.Multiplayer;
using HarmonyLib;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Multiplayer.Game.Lobby;
using MegaCrit.Sts2.Core.Saves;

namespace DisplayName.DisplayNameCode.Patch;


[HarmonyPatch(typeof(LoadRunLobby), MethodType.Constructor, typeof(INetGameService), typeof(ILoadRunLobbyListener), typeof(SerializableRun))]
public static class LoadRunLobbyConstructorPatch
{
    [HarmonyPostfix]
    public static void Postfix(LoadRunLobby __instance)
    {
        DisplayNameNetworking.OnLoadRunLobbyCreated(__instance);
    }
}

[HarmonyPatch(typeof(LoadRunLobby), nameof(LoadRunLobby.CleanUp))]
public static class LoadRunLobbyCleanUpPatch
{
    [HarmonyPostfix]
    public static void Postfix(LoadRunLobby __instance, bool disconnectSession)
    {
        DisplayNameNetworking.OnLoadRunLobbyCleanedUp(__instance, disconnectSession);
    }
}
