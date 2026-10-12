using System.Reflection;
using DisplayName.DisplayNameCode.Multiplayer;
using HarmonyLib;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Multiplayer.Game.Lobby;

namespace DisplayName.DisplayNameCode.Patch;

[HarmonyPatch]
public static class RunLobbyDisplayNamePatch
{
    private static IEnumerable<MethodBase> TargetMethods() => typeof(RunLobby).GetConstructors();

    [HarmonyPostfix]
    public static void Postfix(INetGameService __1) => DisplayNameNetworking.BindSession(__1);
}
