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
/// re-queries PlatformUtil afterward (confirmed by reading the decompiled source: RefreshVisuals(), called
/// on every later player-state change, only touches the character label/icon/ready-indicator, never the
/// nameplate). Since a remote player's DisplayName can only arrive after a network round-trip, it very
/// often lands AFTER this widget has already rendered the player's Steam username, and nothing would
/// otherwise ever refresh it. This patch tracks every live instance and re-sets its nameplate text
/// whenever a DisplayNameSyncMessage updates that specific player.
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

            if (player.PlayerId != playerId)
            {
                continue;
            }

            MegaLabel nameplate = player.GetNode<MegaLabel>("%NameplateLabel");
            nameplate.SetTextAutoSize(PlatformUtil.GetPlayerNameRaw(PlatformUtil.PrimaryPlatform, playerId));
        }
    }
}
