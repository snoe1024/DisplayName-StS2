using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Multiplayer.Serialization;
using MegaCrit.Sts2.Core.Multiplayer.Transport;

namespace DisplayName.DisplayNameCode.Multiplayer;

// Announces the sender's own DisplayName to everyone else in the lobby/run. Every peer (host and clients
// alike) sends this about THEMSELVES only -- there is no host authority here, unlike SingleplayerCard's
// config sync, since each player owns their own name. ShouldBroadcast makes the host relay it to every
// other connected peer automatically (see NetHostGameService.OnPacketReceived); ShouldBuffer keeps it from
// being dropped if it arrives while the local NetMessageBus is still buffering (see
// NetMessageBus.SetBufferMessages).
internal struct DisplayNameSyncMessage : INetMessage, IPacketSerializable
{
    public string DisplayName;

    public bool ShouldBroadcast => true;

    public NetTransferMode Mode => NetTransferMode.Reliable;

    // Info (not VeryDebug) so NetMessageBus's own "received message X, sending to N handlers" line shows
    // up in a normal godot.log without needing verbose logging enabled, alongside DisplayNameNetworking's
    // own explicit Log.Info calls for the send/receive events themselves.
    public LogLevel LogLevel => LogLevel.Info;

    public bool ShouldBuffer => true;

    public void Serialize(PacketWriter writer)
    {
        writer.WriteString(DisplayName);
    }

    public void Deserialize(PacketReader reader)
    {
        DisplayName = reader.ReadString();
    }
}
