using System.Collections.Generic;
using DisplayName.DisplayNameCode.Multiplayer;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.addons.mega_text;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Nodes.Multiplayer;
using MegaCrit.Sts2.Core.Platform;

namespace DisplayName.DisplayNameCode.Patch;

/// <summary>
/// NMultiplayerPlayerState (the persistent in-combat nameplate/HUD widget floating above each player --
/// the thing you actually click to open NMultiplayerPlayerExpandedState) has the exact same bug as
/// NRemoteLobbyPlayer: it sets its nameplate text exactly once in _Ready() and never re-queries
/// PlatformUtil afterward (confirmed by reading the decompiled source -- none of its many Refresh* methods
/// touch the nameplate label). Since a remote player's DisplayName can only arrive after a network
/// round-trip, this widget very often renders before that arrives, and nothing would otherwise ever
/// refresh it for the rest of combat. Mirrors NRemoteLobbyPlayerDisplayNamePatch.
/// </summary>
[HarmonyPatch(typeof(NMultiplayerPlayerState), "_Ready")]
public static class NMultiplayerPlayerStateReadyPatch
{
    private static readonly List<NMultiplayerPlayerState> LiveWidgets = new();
    private static bool _subscribed;

    [HarmonyPostfix]
    public static void Postfix(NMultiplayerPlayerState __instance)
    {
        LiveWidgets.Add(__instance);
        if (!_subscribed)
        {
            _subscribed = true;
            RemoteDisplayNames.Updated += OnRemoteDisplayNameUpdated;
        }
    }

    private static void OnRemoteDisplayNameUpdated(ulong playerId)
    {
        for (int i = LiveWidgets.Count - 1; i >= 0; i--)
        {
            NMultiplayerPlayerState widget = LiveWidgets[i];
            if (!GodotObject.IsInstanceValid(widget))
            {
                LiveWidgets.RemoveAt(i);
                continue;
            }

            Player player = widget.Player;
            if (player == null || player.NetId != playerId)
            {
                continue;
            }

            MegaLabel nameplate = widget.GetNode<MegaLabel>("%NameplateLabel");
            nameplate.SetTextAutoSize(PlatformUtil.GetPlayerNameRaw(PlatformUtil.PrimaryPlatform, playerId));
        }
    }
}
