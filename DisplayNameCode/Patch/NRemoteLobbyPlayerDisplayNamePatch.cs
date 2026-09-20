using System.Collections.Generic;
using DisplayName.DisplayNameCode.Multiplayer;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.addons.mega_text;
using MegaCrit.Sts2.Core.Nodes.Multiplayer;
using MegaCrit.Sts2.Core.Platform;

namespace DisplayName.DisplayNameCode.Patch;

/// <summary>
/// NRemoteLobbyPlayer (the nameplate widget in the pre-run lobby/character-select waiting room, and also
/// used by the "resume from save" lobby) sets its nameplate text exactly once in _Ready() and never
/// re-queries PlatformUtil afterward (RefreshVisuals(), called on every later player-state change, only
/// touches the character label/icon/ready-indicator, never the nameplate). This patch always re-resolves
/// and re-sets the nameplate itself right after _Ready() runs, rather than trusting vanilla's one-shot
/// resolution -- both because a remote player's DisplayName can only arrive after a network round-trip
/// (often landing after this widget already rendered their Steam username), and because
/// PlatformUtilGetPlayerNameRawPatch's own postfix does not reliably fire for vanilla's internal call
/// inside _Ready() (see sts2_dev_knowledge/topics/multiplayer-networking.md). Re-resolving from our own
/// code here is not subject to that. Also tracks every live instance and re-applies this whenever a
/// DisplayNameSyncMessage updates that specific remote player later.
/// </summary>
[HarmonyPatch(typeof(NRemoteLobbyPlayer), "_Ready")]
public static class NRemoteLobbyPlayerReadyPatch
{
    private static readonly List<NRemoteLobbyPlayer> LivePlayers = new();
    private static bool _subscribed;

    [HarmonyPostfix]
    public static void Postfix(NRemoteLobbyPlayer __instance)
    {
        LivePlayers.Add(__instance);
        if (!_subscribed)
        {
            _subscribed = true;
            RemoteDisplayNames.Updated += OnRemoteDisplayNameUpdated;
        }

        RefreshNameplate(__instance);
    }

    private static void OnRemoteDisplayNameUpdated(ulong playerId)
    {
        for (int i = LivePlayers.Count - 1; i >= 0; i--)
        {
            NRemoteLobbyPlayer player = LivePlayers[i];
            if (!GodotObject.IsInstanceValid(player))
            {
                LivePlayers.RemoveAt(i);
                continue;
            }

            if (player.PlayerId == playerId)
            {
                RefreshNameplate(player);
            }
        }
    }

    private static void RefreshNameplate(NRemoteLobbyPlayer instance)
    {
        MegaLabel nameplate = instance.GetNode<MegaLabel>("%NameplateLabel");
        nameplate.SetTextAutoSize(PlatformUtil.GetPlayerNameRaw(PlatformUtil.PrimaryPlatform, instance.PlayerId));
    }
}
