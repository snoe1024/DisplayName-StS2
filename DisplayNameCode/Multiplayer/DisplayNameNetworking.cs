using DisplayName.DisplayNameCode.Config;
using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.Multiplayer.Game.Lobby;
using DisplayName.DisplayNameCode;

namespace DisplayName.DisplayNameCode.Multiplayer;

/// <summary>
/// Wires DisplayName (and the ShowSteamIdAlongside consent flag) announcements into a multiplayer
/// lobby/run. Every peer (host or client) broadcasts its own current state whenever ANYONE connects --
/// including itself, since <see cref="StartRunLobby.PlayerConnected"/> fires for the local player's own
/// join too. This covers both directions without any host-authority special-casing:
///  - A freshly-joined peer announces itself to everyone already present.
///  - Everyone already present re-announces themselves so the newcomer learns their state too (messages
///    broadcast before a peer finished joining are never delivered to it -- see NetHostGameService.SetPeerReadyForBroadcasting
///    doc comment -- so a plain one-time broadcast at startup would not reach later joiners).
/// A remote player's DisplayName is only substituted in when non-empty (see PlatformUtilGetPlayerNameRawPatch);
/// otherwise the vanilla Steam-username lookup is used as-is.
/// </summary>
internal static class DisplayNameNetworking
{
    private static StartRunLobby? _activeLobby;

    public static void OnLobbyCreated(StartRunLobby lobby)
    {
        MainFile.Logger.Info($"DisplayName: lobby created (NetService.Type={lobby.NetService.Type}, NetId={lobby.NetService.NetId}), registering sync handlers.");
        _activeLobby = lobby;
        RemoteDisplayNames.Clear();
        RemoteShowSteamIdFlags.Clear();
        lobby.NetService.RegisterMessageHandler<DisplayNameSyncMessage>(HandleMessage);
        lobby.PlayerConnected += OnPlayerConnected;
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

        MainFile.Logger.Info("DisplayName: lobby disconnected, unregistering sync handlers and clearing remote cache.");
        lobby.NetService.UnregisterMessageHandler<DisplayNameSyncMessage>(HandleMessage);
        lobby.PlayerConnected -= OnPlayerConnected;
        if (_activeLobby == lobby)
        {
            _activeLobby = null;
            RemoteDisplayNames.Clear();
            RemoteShowSteamIdFlags.Clear();
        }
    }

    // Called whenever anything in DisplayNameConfig changes (wired from MainFile via
    // DisplayNameConfig.ConfigChanged) -- covers both the DisplayName text field and the
    // ShowSteamIdAlongside checkbox, since both are part of the same broadcasted state.
    public static void NotifyLocalStateChanged()
    {
        BroadcastOwnState();
    }

    private static void OnPlayerConnected(StartRunLobbyPlayer player)
    {
        BroadcastOwnState();
    }

    private static void BroadcastOwnState()
    {
        if (_activeLobby == null)
        {
            return;
        }

        DisplayNameSyncMessage message = new()
        {
            DisplayName = DisplayNameConfig.DisplayName,
            ShowSteamIdAlongside = DisplayNameConfig.ShowSteamIdAlongside,
        };
        MainFile.Logger.Info($"DisplayName: broadcasting own state (DisplayName='{message.DisplayName}', ShowSteamIdAlongside={message.ShowSteamIdAlongside}) as NetId={_activeLobby.NetService.NetId}.");
        _activeLobby.NetService.SendMessage(message);
    }

    private static void HandleMessage(DisplayNameSyncMessage message, ulong senderId)
    {
        MainFile.Logger.Info($"DisplayName: received sync from senderId={senderId} (DisplayName='{message.DisplayName}', ShowSteamIdAlongside={message.ShowSteamIdAlongside}).");
        if (_activeLobby != null && senderId == _activeLobby.NetService.NetId)
        {
            MainFile.Logger.Info("DisplayName: ignoring sync message that looped back to ourselves.");
            return;
        }

        RemoteDisplayNames.Set(senderId, message.DisplayName);
        RemoteShowSteamIdFlags.Set(senderId, message.ShowSteamIdAlongside);
    }
}
