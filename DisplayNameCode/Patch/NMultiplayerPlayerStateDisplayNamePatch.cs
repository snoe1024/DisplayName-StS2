using System.Collections.Generic;
using DisplayName.DisplayNameCode.Multiplayer;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.addons.mega_text;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Nodes.Multiplayer;
using MegaCrit.Sts2.Core.Platform;

namespace DisplayName.DisplayNameCode.Patch;

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

        RefreshNameplate(__instance);
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
            if (player != null && player.NetId == playerId)
            {
                RefreshNameplate(widget);
            }
        }
    }

    private static void RefreshNameplate(NMultiplayerPlayerState widget)
    {
        Player player = widget.Player;
        if (player == null)
        {
            return;
        }

        MegaLabel nameplate = widget.GetNode<MegaLabel>("%NameplateLabel");
        nameplate.SetTextAutoSize(PlatformUtil.GetPlayerNameRaw(PlatformUtil.PrimaryPlatform, player.NetId));
    }
}
