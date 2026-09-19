using System.Diagnostics;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.Multiplayer;
using MegaCrit.Sts2.Core.Multiplayer.Transport.Steam;

namespace DisplayName.DisplayNameCode.Patch;

/// <summary>
/// TEMPORARY, diagnostic-only. Investigating why NetHostGameService.IsConnected reads false even while
/// real client connections are clearly succeeding (see CLAUDE.md, "現在の最優先課題"). Delete this file
/// once the mystery is resolved -- it is not meant to ship.
///
/// Logs (to godot.log, always -- useful even without attaching a debugger):
/// - Every SteamHost.StartHost/StopHost call, with a full stack trace, to see WHO triggers the repeated
///   start/stop churn observed in a real playtest log ("resume from save" cycled through it 3x within
///   about a second).
/// - Every SteamHost.OnNetStatusChanged (a real Steamworks connection-state event), with that exact
///   instance's own _isConnected field read directly -- proving whether _isConnected is really true at
///   the moment a connection is actually being processed.
/// - Every time NetHostGameService.IsConnected's return value (or its underlying NetHost instance)
///   changes, with a timestamp -- an exact transition timeline independent of our own polling cadence,
///   catching ANY caller's read of it (not just DisplayNameNetworking's), including vanilla's own
///   internal SendMessage/Update/Disconnect checks.
///
/// Good Rider breakpoint spots (these are OUR code, so line numbers stay put across rebuilds -- unlike
/// trying to break inside decompiled vanilla source, where line numbers may not match exactly):
/// - StartHostPrefix / StopHostPrefix: to see the exact call site (stack trace) triggering the churn.
///   Start as non-suspending (a "tracepoint" -- log a message, don't stop) since these fire during normal
///   connection setup and suspending here could stall live Steam network processing for both peers.
/// - OnNetStatusChangedPostfix: condition `!isConnected` -- if this EVER hits, it directly disproves the
///   assumption that _isConnected must already be true whenever a connection event fires, which is the
///   core contradiction driving this whole investigation.
/// - IsConnectedGetterPostfix: condition `__result` (i.e. it just flipped true) -- breaks at the very
///   first moment IsConnected becomes true. Inspect __instance's hash and __instance.NetHost's hash there
///   against what DisplayNameNetworking.AnnounceSelf's own log lines report, and check the call stack /
///   thread to see who's reading it and from where.
/// </summary>
public static class SteamHostDiagnosticPatch
{
    private static bool? _lastIsConnected;
    private static int _lastNetHostHash;

    [HarmonyPatch(typeof(SteamHost), "StartHost")]
    [HarmonyPrefix]
    public static void StartHostPrefix(SteamHost __instance)
    {
        MainFile.Logger.Info($"DisplayName[DIAG]: SteamHost#{__instance.GetHashCode()}.StartHost() called.\n{new StackTrace(true)}");
    }

    [HarmonyPatch(typeof(SteamHost), "StopHost")]
    [HarmonyPrefix]
    public static void StopHostPrefix(SteamHost __instance, NetError reason, bool now)
    {
        MainFile.Logger.Info($"DisplayName[DIAG]: SteamHost#{__instance.GetHashCode()}.StopHost(reason={reason}, now={now}) called.\n{new StackTrace(true)}");
    }

    [HarmonyPatch(typeof(SteamHost), "OnNetStatusChanged")]
    [HarmonyPostfix]
    public static void OnNetStatusChangedPostfix(SteamHost __instance)
    {
        bool isConnected = Traverse.Create(__instance).Field("_isConnected").GetValue<bool>();
        MainFile.Logger.Info($"DisplayName[DIAG]: SteamHost#{__instance.GetHashCode()}.OnNetStatusChanged fired; _isConnected={isConnected} right now.");
    }

    [HarmonyPatch(typeof(NetHostGameService), nameof(NetHostGameService.IsConnected), MethodType.Getter)]
    [HarmonyPostfix]
    public static void IsConnectedGetterPostfix(NetHostGameService __instance, ref bool __result)
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
