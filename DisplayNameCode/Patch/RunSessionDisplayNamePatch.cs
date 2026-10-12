using DisplayName.DisplayNameCode.Multiplayer;
using HarmonyLib;
using MegaCrit.Sts2.Core.Runs;

namespace DisplayName.DisplayNameCode.Patch;

[HarmonyPatch(typeof(RunManager), "InitializeShared")]
public static class RunSessionDisplayNamePatch
{
    [HarmonyPostfix]
    public static void Postfix(RunManager __instance) => DisplayNameNetworking.BindSession(__instance.NetService);
}
