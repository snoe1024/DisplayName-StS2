using DisplayName.DisplayNameCode;
using DisplayName.DisplayNameCode.Config;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Multiplayer.Game.Lobby;
using MegaCrit.Sts2.Core.Platform;

namespace DisplayName.DisplayNameCode.Multiplayer;

/// <summary>
/// Wires DisplayName (and the ShowSteamIdAlongside consent flag) announcements into a multiplayer
/// lobby/run. Every peer (host or client) broadcasts its own current state whenever ANYONE connects --
/// including itself. This covers both directions without any host-authority special-casing:
///  - A freshly-joined peer announces itself to everyone already present.
///  - Everyone already present re-announces themselves so the newcomer learns their state too (messages
///    broadcast before a peer finished joining are never delivered to it -- see NetHostGameService.SetPeerReadyForBroadcasting
///    doc comment -- so a plain one-time broadcast at startup would not reach later joiners).
/// A remote player's DisplayName is only substituted in when non-empty (see PlatformUtilGetPlayerNameRawPatch);
/// otherwise the vanilla Steam-username lookup is used as-is.
///
/// There are THREE separate lobby/connection-lifecycle classes in this game (confirmed by reading the
/// decompiled source, see sts2_dev_knowledge/topics/game-fundamentals.md): StartRunLobby (starting a brand
/// new run), LoadRunLobby (resuming a run from a save file), and RunLobby (reconnecting while a run is
/// already in progress). Each needs its own wiring since they don't share a common base type/event. Only
/// StartRunLobby and LoadRunLobby are wired so far; RunLobby (mid-run reconnect) is a known follow-up gap.
///
/// NOTE: StartRunLobby's connection-triggering methods are Harmony-patched by NAME (see
/// StartRunLobbyPlayerConnectedPatch.cs) rather than subscribed to via its PlayerConnected C# event,
/// specifically to stay compatible with both the Production and Public Beta branches: the event's generic
/// type parameter is named StartRunLobbyPlayer on Beta but LobbyPlayer on Production (0.107.1) -- same
/// field layout, different type name -- which would otherwise be a hard compile-time dependency on
/// whichever branch's sts2.dll happens to be referenced at build time. We never used the event's payload
/// anyway, so patching the (name-stable) methods that raise it sidesteps the type entirely.
/// </summary>
internal static class DisplayNameNetworking
{
    private static StartRunLobby? _activeLobby;
    private static LoadRunLobby? _activeLoadRunLobby;

    /// <summary>
    /// True only while an actual lobby/run session is active. PlatformUtilGetPlayerNameRawPatch gates its
    /// remote-name substitution on this so that pre-connection screens (e.g. the "Join Friend" list, which
    /// queries PlatformUtil for a friend's Steam ID before any lobby for THIS session exists) can never
    /// show a stale DisplayName cached from an earlier session with that same person.
    /// </summary>
    public static bool HasActiveLobby => _activeLobby != null || _activeLoadRunLobby != null;

    public static void OnLobbyCreated(StartRunLobby lobby)
    {
        MainFile.Logger.Info($"DisplayName: StartRunLobby created (NetService.Type={lobby.NetService.Type}, NetId={lobby.NetService.NetId}), registering sync handlers.");
        _activeLobby = lobby;
        RemoteDisplayNames.Clear();
        RemoteShowSteamIdFlags.Clear();
        lobby.NetService.RegisterMessageHandler<DisplayNameSyncMessage>(HandleMessage);
    }

    // Mirrors StartRunLobbyCleanUpPatch's contract: disconnectSession is false when the lobby is merely
    // handing off to a run that is about to begin (same NetService instance carries over), so only a real
    // disconnect should unregister and drop cached remote state.
    public static void OnLobbyCleanedUp(StartRunLobby lobby, bool disconnectSession)
    {
        if (!disconnectSession)
        {
            return;
        }

        MainFile.Logger.Info("DisplayName: StartRunLobby disconnected, unregistering sync handlers and clearing remote cache.");
        lobby.NetService.UnregisterMessageHandler<DisplayNameSyncMessage>(HandleMessage);
        if (_activeLobby == lobby)
        {
            _activeLobby = null;
            RemoteDisplayNames.Clear();
            RemoteShowSteamIdFlags.Clear();
        }
    }

    // LoadRunLobby (resuming a run from a save file) has no PlayerConnected-style event of its own -- it
    // only calls methods on its ILoadRunLobbyListener. So instead of one event subscription, several of its
    // own "someone (re)connected" methods are individually Harmony-postfixed to call AnnounceSelf():
    // AddLocalHostPlayer (host adds itself), the client-join-response constructor overload (client just
    // joined), HandleClientLoadJoinRequestMessage (host learns of a new client), and
    // HandlePlayerReconnectedMessage (an existing client learns of another (re)connecting player). See
    // LoadRunLobbyPatch.cs.
    public static void OnLoadRunLobbyCreated(LoadRunLobby lobby)
    {
        MainFile.Logger.Info($"DisplayName: LoadRunLobby created (NetService.Type={lobby.NetService.Type}, NetId={lobby.NetService.NetId}), registering sync handlers.");
        _activeLoadRunLobby = lobby;
        RemoteDisplayNames.Clear();
        RemoteShowSteamIdFlags.Clear();
        lobby.NetService.RegisterMessageHandler<DisplayNameSyncMessage>(HandleMessage);
    }

    public static void OnLoadRunLobbyCleanedUp(LoadRunLobby lobby, bool disconnectSession)
    {
        if (!disconnectSession)
        {
            return;
        }

        MainFile.Logger.Info("DisplayName: LoadRunLobby disconnected, unregistering sync handlers and clearing remote cache.");
        lobby.NetService.UnregisterMessageHandler<DisplayNameSyncMessage>(HandleMessage);
        if (_activeLoadRunLobby == lobby)
        {
            _activeLoadRunLobby = null;
            RemoteDisplayNames.Clear();
            RemoteShowSteamIdFlags.Clear();
        }
    }

    // Public entry point for every trigger that should (re-)announce our own current state:
    // - DisplayNameConfig.ConfigChanged (wired from MainFile) -- covers both the DisplayName text field
    //   and the ShowSteamIdAlongside checkbox, since both are part of the same broadcasted state.
    // - Every StartRunLobby/LoadRunLobby "someone (re)connected" method patched by name (see
    //   StartRunLobbyPlayerConnectedPatch.cs / LoadRunLobbyPatch.cs) -- none of them need the connecting
    //   player's identity from us, we just re-broadcast unconditionally whenever any of them fire.
    public static void NotifyLocalStateChanged()
    {
        AnnounceSelf();
    }

    private static void AnnounceSelf()
    {
        INetGameService? netService = _activeLobby?.NetService ?? _activeLoadRunLobby?.NetService;
        if (netService == null)
        {
            return;
        }

        DisplayNameSyncMessage message = new()
        {
            DisplayName = DisplayNameConfig.DisplayName,
            ShowSteamIdAlongside = DisplayNameConfig.ShowSteamIdAlongside,
        };
        MainFile.Logger.Info($"DisplayName: broadcasting own state (DisplayName='{message.DisplayName}', ShowSteamIdAlongside={message.ShowSteamIdAlongside}) as NetId={netService.NetId}.");
        netService.SendMessage(message);
    }

    private static void HandleMessage(DisplayNameSyncMessage message, ulong senderId)
    {
        MainFile.Logger.Info($"DisplayName: received sync from senderId={senderId} (DisplayName='{message.DisplayName}', ShowSteamIdAlongside={message.ShowSteamIdAlongside}).");
        if (senderId == PlatformUtil.GetLocalPlayerId(PlatformUtil.PrimaryPlatform))
        {
            MainFile.Logger.Info("DisplayName: ignoring sync message that looped back to ourselves.");
            return;
        }

        RemoteDisplayNames.Set(senderId, message.DisplayName);
        RemoteShowSteamIdFlags.Set(senderId, message.ShowSteamIdAlongside);
    }
}
