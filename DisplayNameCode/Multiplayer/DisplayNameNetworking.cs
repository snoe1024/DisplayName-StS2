using DisplayName.DisplayNameCode.Config;
using Godot;
using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Multiplayer.Game.Lobby;
using MegaCrit.Sts2.Core.Platform;
using STS2RitsuLib.Networking.Sidecar;

namespace DisplayName.DisplayNameCode.Multiplayer;

internal static class DisplayNameNetworking
{
    private static RitsuLibSidecarEndpointHandle? _endpoint;
    private static INetGameService? _service;
    private static Action<NetErrorInfo>? _disconnected;
    private static int _sessionGeneration;
    private static int _retryBudget;
    private static bool _retryScheduled;
    private static bool _needsAnnouncement;
    private static RitsuLibSidecarSendStatus? _lastFailure;

    public static bool HasActiveLobby => _service != null;

    public static void Initialize()
    {
        RegisterEndpoint();
        RitsuLibSidecarSessionManager.SessionBound += change => BindSession(change.NetService);
        RitsuLibSidecarSessionManager.SessionUnbound += _ => EndSession();
    }

    private static void RegisterEndpoint()
    {
        int generation = _sessionGeneration;
        _endpoint = RitsuLibSidecarEndpoints.Register(new RitsuLibSidecarEndpointDescriptor(
            ownerId: MainFile.ModId, name: "display-name", protocolVersion: 1,
            minimumCompatibleProtocolVersion: 1,
            deliveryProfile: RitsuLibSidecarDeliveryProfile.Control,
            topology: RitsuLibSidecarEndpointTopology.RelayGroup,
            maxPayloadBytes: DisplayNamePayload.MaxBytes,
            dispatchMode: RitsuLibSidecarEndpointDispatchMode.GodotMainLoop,
            maxOutboundPacketsPerSecond: 8, maxOutboundBytesPerSecond: 1024), message =>
            {
                if (generation == _sessionGeneration) OnMessage(message);
            });
        _endpoint.ParticipantsChanged += OnParticipantsChanged;
    }

    public static void OnLobbyCreated(StartRunLobby lobby) => BindSession(lobby.NetService);
    public static void OnLoadRunLobbyCreated(LoadRunLobby lobby) => BindSession(lobby.NetService);
    public static void OnLobbyCleanedUp(StartRunLobby lobby, bool disconnectSession)
    {
        if (disconnectSession && ReferenceEquals(_service, lobby.NetService)) EndSession();
    }
    public static void OnLoadRunLobbyCleanedUp(LoadRunLobby lobby, bool disconnectSession)
    {
        if (disconnectSession && ReferenceEquals(_service, lobby.NetService)) EndSession();
    }

    public static void BindSession(INetGameService service)
    {
        if (service.Type == NetGameType.Singleplayer) { EndSession(); return; }
        if (ReferenceEquals(_service, service)) return;
        EndSession();
        _service = service;
        RegisterEndpoint();
        _disconnected = _ => { if (ReferenceEquals(_service, service)) EndSession(); };
        service.Disconnected += _disconnected;
        RequestAnnouncement();
    }

    public static void EndSession()
    {
        if (_service != null && _disconnected != null) _service.Disconnected -= _disconnected;
        _service = null;
        _disconnected = null;
        _sessionGeneration++;
        // Disposal also cancels queued receive callbacks from the previous session.
        _endpoint?.Dispose();
        _endpoint = null;
        _retryScheduled = false;
        _needsAnnouncement = false;
        _lastFailure = null;
        RemoteDisplayNames.Clear();
    }

    public static void NotifyLocalStateChanged()
    {
        ulong localId = _service?.NetId ?? PlatformUtil.GetLocalPlayerId(PlatformUtil.PrimaryPlatform);
        RemoteDisplayNames.NotifyUpdated(localId);
        RequestAnnouncement();
    }

    private static void OnParticipantsChanged(RitsuLibSidecarEndpointParticipantsChangedEvent change)
    {
        // Read the current snapshot rather than applying a queued, superseded route event.
        if (_endpoint == null) return;
        RemoteDisplayNames.Retain(_endpoint.GetParticipantsSnapshot().ToHashSet());
        RequestAnnouncement();
    }

    private static void RequestAnnouncement()
    {
        _needsAnnouncement = true;
        _retryBudget = 10;
        TryAnnounce();
    }

    private static void TryAnnounce()
    {
        if (!_needsAnnouncement || _service == null || _endpoint == null) return;
        var result = _endpoint.Broadcast(DisplayNamePayload.Encode(DisplayNameConfig.DisplayName));
        if (result.IsAccepted)
        {
            _needsAnnouncement = false;
            _lastFailure = null;
            return;
        }

        if (_lastFailure != result.Status)
        {
            _lastFailure = result.Status;
            MainFile.Logger.Info($"DisplayName: sharing unavailable ({result.Status}); normal gameplay is unaffected.");
        }

        if (_retryScheduled || _retryBudget-- <= 0) return;
        if (Engine.GetMainLoop() is not SceneTree tree) return;
        int generation = _sessionGeneration;
        _retryScheduled = true;
        tree.CreateTimer(1, processAlways: true).Timeout += () =>
        {
            if (generation != _sessionGeneration) return;
            _retryScheduled = false;
            TryAnnounce();
        };
    }

    private static void OnMessage(RitsuLibSidecarEndpointMessage message)
    {
        if (_service == null || message.NegotiatedProtocolVersion != 1 ||
            message.OriginalSenderNetId == _service.NetId ||
            !_endpoint!.GetParticipantsSnapshot().Contains(message.OriginalSenderNetId)) return;
        if (!DisplayNamePayload.TryDecode(message.Payload.Span, out string name))
        {
            MainFile.Logger.Warn("DisplayName: rejected an invalid name payload.");
            return;
        }
        RemoteDisplayNames.Set(message.OriginalSenderNetId, name);
    }
}
