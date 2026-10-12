using DisplayName.DisplayNameCode.Multiplayer;
using HarmonyLib;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Multiplayer.Game.Lobby;
using MegaCrit.Sts2.Core.Runs;

namespace DisplayName.DisplayNameCode.Patch;

[HarmonyPatch(typeof(StartRunLobby), MethodType.Constructor, typeof(GameMode), typeof(INetGameService), typeof(IStartRunLobbyListener), typeof(int))]
public static class StartRunLobbyConstructorPatch
{
    [HarmonyPostfix]
    public static void Postfix(StartRunLobby __instance)
    {
        DisplayNameNetworking.OnLobbyCreated(__instance);
    }
}
