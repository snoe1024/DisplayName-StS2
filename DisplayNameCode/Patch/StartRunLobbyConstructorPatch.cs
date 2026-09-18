using DisplayName.DisplayNameCode.Multiplayer;
using HarmonyLib;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Multiplayer.Game.Lobby;
using MegaCrit.Sts2.Core.Runs;

namespace DisplayName.DisplayNameCode.Patch;

// StartRunLobby is the earliest point a NetGameService exists for the lobby, so this is where
// DisplayNameNetworking registers its message handler and PlayerConnected callback. Only the 4-arg
// constructor needs patching: the 5-arg overload (Daily runs) delegates to it via `: this(...)`.
[HarmonyPatch(typeof(StartRunLobby), MethodType.Constructor, typeof(GameMode), typeof(INetGameService), typeof(IStartRunLobbyListener), typeof(int))]
public static class StartRunLobbyConstructorPatch
{
    [HarmonyPostfix]
    public static void Postfix(StartRunLobby __instance)
    {
        DisplayNameNetworking.OnLobbyCreated(__instance);
    }
}
