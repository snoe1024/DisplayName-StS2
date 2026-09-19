using DisplayName.DisplayNameCode.Multiplayer;
using HarmonyLib;
using MegaCrit.Sts2.Core.Multiplayer.Game.Lobby;

namespace DisplayName.DisplayNameCode.Patch;

// StartRunLobby.PlayerConnected exists as a genuine C# event (unlike LoadRunLobby), but its generic type
// parameter is named StartRunLobbyPlayer on the Public Beta branch and LobbyPlayer on Production (0.107.1)
// -- identical field layout, different type name (confirmed by comparing both branches' decompiled
// sources). Subscribing to the event directly (`lobby.PlayerConnected += Handler`) would require a
// compile-time reference to whichever name matches the currently-referenced sts2.dll, breaking the build
// on the other branch. We never used the event's payload anyway (we just re-broadcast our own state
// unconditionally whenever anyone connects), so instead we Harmony-patch, by NAME ONLY, the four internal
// methods that raise it -- these method names and their other parameter types are stable across both
// branches, only the StartRunLobbyPlayer/LobbyPlayer type differs.
[HarmonyPatch(typeof(StartRunLobby), "InitializeFromMessage")]
public static class StartRunLobbyInitializeFromMessagePatch
{
    [HarmonyPostfix]
    public static void Postfix()
    {
        DisplayNameNetworking.NotifyLocalStateChanged();
    }
}

[HarmonyPatch(typeof(StartRunLobby), "AddLocalHostPlayerInternal")]
public static class StartRunLobbyAddLocalHostPlayerInternalPatch
{
    [HarmonyPostfix]
    public static void Postfix()
    {
        DisplayNameNetworking.NotifyLocalStateChanged();
    }
}

[HarmonyPatch(typeof(StartRunLobby), "HandleClientLobbyJoinRequestMessage")]
public static class StartRunLobbyHandleClientLobbyJoinRequestPatch
{
    [HarmonyPostfix]
    public static void Postfix()
    {
        DisplayNameNetworking.NotifyLocalStateChanged();
    }
}

[HarmonyPatch(typeof(StartRunLobby), "HandlePlayerJoinedMessage")]
public static class StartRunLobbyHandlePlayerJoinedMessagePatch
{
    [HarmonyPostfix]
    public static void Postfix()
    {
        DisplayNameNetworking.NotifyLocalStateChanged();
    }
}
