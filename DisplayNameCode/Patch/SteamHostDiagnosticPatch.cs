using System.Diagnostics;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.Multiplayer;
using MegaCrit.Sts2.Core.Multiplayer.Transport.Steam;

namespace DisplayName.DisplayNameCode.Patch;

// TEMPORARY, diagnostic-only. Investigating why NetHostGameService.IsConnected can read false during a
// LoadRunLobby ("resume from save") session even while real client connections are succeeding (see
// CLAUDE.md, "現在の最優先課題"). A fresh StartRunLobby host has been confirmed clean (IsConnected flips
// true once and stays true for the whole session, no start/stop churn) -- this diagnostic is only useful
// for retesting the LoadRunLobby path specifically. Delete these classes once the mystery is resolved --
// not meant to ship.
//
// CAUTION: suspending on SteamHostOnNetStatusChangedDiagnosticPatch or anything else on the Steam
// networking hot path for more than a couple seconds can starve SteamNetworkingSockets' own internal
// processing and has caused a native assertion failure in Steam's own client library during testing.
// Prefer a non-suspending tracepoint (log a message, don't stop) here; only escalate to a real suspending
// breakpoint once the moment of interest is narrowed down, and resume quickly.
//
// Good Rider breakpoint spots (these are OUR code, so line numbers stay put across rebuilds -- unlike
// trying to break inside decompiled vanilla source):
// - SteamHostStartHostDiagnosticPatch / SteamHostStopHostDiagnosticPatch: the exact call site (stack
//   trace) for each start/stop.
// - SteamHostOnNetStatusChangedDiagnosticPatch: condition `!isConnected` -- catches a connection event
//   being processed while _isConnected is still false on that instance.
// - NetHostGameServiceIsConnectedDiagnosticPatch: condition `__result` (i.e. it just flipped true) --
//   breaks at the first moment IsConnected becomes true. Inspect __instance's hash and
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
