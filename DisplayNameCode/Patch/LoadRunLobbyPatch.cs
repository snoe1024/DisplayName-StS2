using DisplayName.DisplayNameCode.Multiplayer;
using HarmonyLib;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Multiplayer.Game.Lobby;
using MegaCrit.Sts2.Core.Multiplayer.Messages.Lobby;
using MegaCrit.Sts2.Core.Saves;

namespace DisplayName.DisplayNameCode.Patch;

// LoadRunLobby is a SEPARATE class from StartRunLobby, used for "resume a run from a save file" instead of
// "start a brand new run" (per the decompiled source's own doc comment on LoadRunLobby). Unlike
// StartRunLobby, it has no single PlayerConnected event; instead several of its own methods individually
// notify ILoadRunLobbyListener when someone (re)connects, so each of those is patched here instead of one
// event.

// The primary constructor: registers our message handler. Used directly when hosting (resuming your own
// save) and indirectly by the client-join constructor below (which delegates via `: this(...)`).
[HarmonyPatch(typeof(LoadRunLobby), MethodType.Constructor, typeof(INetGameService), typeof(ILoadRunLobbyListener), typeof(SerializableRun))]
public static class LoadRunLobbyConstructorPatch
{
    [HarmonyPostfix]
    public static void Postfix(LoadRunLobby __instance)
    {
        DisplayNameNetworking.OnLoadRunLobbyCreated(__instance);
    }
}

// The client-join constructor: fires once the client has actually joined an existing host's LoadRunLobby
// (it already has the full ClientLoadJoinResponseMessage in hand at this point), so this is where the
// CLIENT should announce itself. The 3-arg constructor above already ran first via `: this(...)` and
// registered the message handler, so this postfix only needs to announce.
[HarmonyPatch(typeof(LoadRunLobby), MethodType.Constructor, typeof(INetGameService), typeof(ILoadRunLobbyListener), typeof(ClientLoadJoinResponseMessage))]
public static class LoadRunLobbyClientJoinConstructorPatch
{
    [HarmonyPostfix]
    public static void Postfix(LoadRunLobby __instance)
    {
        DisplayNameNetworking.NotifyLocalStateChanged();
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

// Host adding itself to its own freshly-created LoadRunLobby.
[HarmonyPatch(typeof(LoadRunLobby), nameof(LoadRunLobby.AddLocalHostPlayer))]
public static class LoadRunLobbyAddLocalHostPlayerPatch
{
    [HarmonyPostfix]
    public static void Postfix()
    {
        DisplayNameNetworking.NotifyLocalStateChanged();
    }
}

// Host-side: a new client just asked to join. Postfix runs after SetPeerReadyForBroadcasting(senderId) has
// already been called inside this method, so re-announcing here is guaranteed to actually reach them.
[HarmonyPatch(typeof(LoadRunLobby), "HandleClientLoadJoinRequestMessage")]
public static class LoadRunLobbyHandleClientLoadJoinRequestPatch
{
    [HarmonyPostfix]
    public static void Postfix()
    {
        DisplayNameNetworking.NotifyLocalStateChanged();
    }
}

// Client-side: the host told us some other player (re)connected. Re-announcing ourselves here means that
// player -- now marked ready-for-broadcasting on the host -- also learns about every OTHER already-present
// player as each of them reacts to the same message, not just the host.
[HarmonyPatch(typeof(LoadRunLobby), "HandlePlayerReconnectedMessage")]
public static class LoadRunLobbyHandlePlayerReconnectedPatch
{
    [HarmonyPostfix]
    public static void Postfix()
    {
        DisplayNameNetworking.NotifyLocalStateChanged();
    }
}
