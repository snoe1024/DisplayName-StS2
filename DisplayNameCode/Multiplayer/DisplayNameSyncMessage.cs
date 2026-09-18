using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Multiplayer.Serialization;
using MegaCrit.Sts2.Core.Multiplayer.Transport;

namespace DisplayName.DisplayNameCode.Multiplayer;

// Announces the sender's own DisplayName (and their own consent to reveal their Steam username
// alongside it) to everyone else in the lobby/run. Every peer (host and clients alike) sends this about
// THEMSELVES only -- there is no host authority here, unlike SingleplayerCard's config sync, since each
// player owns their own name and their own privacy preference. ShouldBroadcast makes the host relay it to
// every other connected peer automatically (see NetHostGameService.OnPacketReceived); ShouldBuffer keeps
// it from being dropped if it arrives while the local NetMessageBus is still buffering (see
// NetMessageBus.SetBufferMessages).
//
// ShowSteamIdAlongside is the SENDER's own DisplayNameConfig.ShowSteamIdAlongside value -- i.e. "do I
// consent to having MY Steam username shown next to MY name on other people's screens", not "do I want to
// see other people's Steam usernames". Each viewer's UI must therefore gate on the SUBJECT player's
// broadcasted flag (see NMultiplayerPlayerExpandedStateReadyPatch), never on the local viewer's own config.
internal struct DisplayNameSyncMessage : INetMessage, IPacketSerializable
{
    public string DisplayName;
    public bool ShowSteamIdAlongside;

    public bool ShouldBroadcast => true;

    public NetTransferMode Mode => NetTransferMode.Reliable;

    // Info (not VeryDebug) so NetMessageBus's own "received message X, sending to N handlers" line shows
    // up in a normal godot.log without needing verbose logging enabled -- this is what let us confirm from
    // an actual playtest log that send/receive itself works correctly (see DisplayNameNetworking's own
    // explicit Log.Info calls for the send/receive events themselves).
    public LogLevel LogLevel => LogLevel.Info;

    public bool ShouldBuffer => true;

    public void Serialize(PacketWriter writer)
    {
        writer.WriteString(DisplayName);
        writer.WriteBool(ShowSteamIdAlongside);
    }

    public void Deserialize(PacketReader reader)
    {
        DisplayName = reader.ReadString();
        ShowSteamIdAlongside = reader.ReadBool();
    }
}
