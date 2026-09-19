using System.Diagnostics;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.Multiplayer;
using MegaCrit.Sts2.Core.Multiplayer.Transport.Steam;

namespace DisplayName.DisplayNameCode.Patch;

// TEMPORARY, diagnostic-only. Investigating why NetHostGameService.IsConnected reads false even while
// real client connections are clearly succeeding (see CLAUDE.md, "現在の最優先課題"). Delete these
// classes once the mystery is resolved -- not meant to ship.
//
// IMPORTANT (bit ourselves once already, see sts2_dev_knowledge/topics/harmony-patching.md): [HarmonyPatch]
// MUST go on the class, with [HarmonyPrefix]/[HarmonyPostfix] on the method inside. Putting [HarmonyPatch]
// directly on the method (all sharing one class, as an earlier version of this file did) compiles fine and
// harmony.PatchAll() reports no error, but silently patches NOTHING -- confirmed via
// harmony.GetPatchedMethods() after MainFile.Initialize() now logs that count on every startup specifically
// to catch this class of mistake early.
//
// Good Rider breakpoint spots (these are OUR code, so line numbers stay put across rebuilds -- unlike
// trying to break inside decompiled vanilla source):
// - SteamHostStartHostDiagnosticPatch / SteamHostStopHostDiagnosticPatch: to see the exact call site
//   (stack trace) triggering the 3x start/stop churn seen in a real "resume from save" host log. Start as
//   non-suspending (a "tracepoint" -- log a message, don't stop) since these fire during normal connection
//   setup and suspending here could stall live Steam network processing for both peers.
// - SteamHostOnNetStatusChangedDiagnosticPatch: condition `!isConnected` -- if this EVER hits, it directly
//   disproves the assumption that _isConnected must already be true whenever this callback can fire at
//   all, which is the core contradiction driving this whole investigation.
// - NetHostGameServiceIsConnectedDiagnosticPatch: condition `__result` (i.e. it just flipped true) --
//   breaks at the very first moment IsConnected becomes true. Inspect __instance's hash and
//   __instance.NetHost's hash there against what DisplayNameNetworking.AnnounceSelf's own log lines
//   report, and check the call stack / thread to see who's reading it and from where.

[HarmonyPatch(typeof(SteamHost), "StartHost")]
public static class SteamHostStartHostDiagnosticPatch
{
    [HarmonyPrefix]
    public static void Prefix(SteamHost __instance)
    {
        MainFile.Logger.Info($"DisplayName[DIAG]: SteamHost#{__instance.GetHashCode()}.StartHost() called.\n{new StackTrace(true)}");
    }
}

[HarmonyPatch(typeof(SteamHost), "StopHost")]
public static class SteamHostStopHostDiagnosticPatch
{
    [HarmonyPrefix]
    public static void Prefix(SteamHost __instance, NetError reason, bool now)
    {
        MainFile.Logger.Info($"DisplayName[DIAG]: SteamHost#{__instance.GetHashCode()}.StopHost(reason={reason}, now={now}) called.\n{new StackTrace(true)}");
    }
}

[HarmonyPatch(typeof(SteamHost), "OnNetStatusChanged")]
public static class SteamHostOnNetStatusChangedDiagnosticPatch
{
    [HarmonyPostfix]
    public static void Postfix(SteamHost __instance)
    {
        bool isConnected = Traverse.Create(__instance).Field("_isConnected").GetValue<bool>();
        MainFile.Logger.Info($"DisplayName[DIAG]: SteamHost#{__instance.GetHashCode()}.OnNetStatusChanged fired; _isConnected={isConnected} right now.");
    }
}

[HarmonyPatch(typeof(NetHostGameService), nameof(NetHostGameService.IsConnected), MethodType.Getter)]
public static class NetHostGameServiceIsConnectedDiagnosticPatch
{
    private static bool? _lastIsConnected;
    private static int _lastNetHostHash;

    [HarmonyPostfix]
    public static void Postfix(NetHostGameService __instance, ref bool __result)
    {
        int netHostHash = __instance.NetHost?.GetHashCode() ?? 0;
        if (_lastIsConnected == __result && _lastNetHostHash == netHostHash)
        {
            return;
        }

        _lastIsConnected = __result;
        _lastNetHostHash = netHostHash;
        MainFile.Logger.Info($"DisplayName[DIAG]: NetHostGameService#{__instance.GetHashCode()}.IsConnected transitioned to {__result} (NetHost=#{netHostHash}).");
    }
}
