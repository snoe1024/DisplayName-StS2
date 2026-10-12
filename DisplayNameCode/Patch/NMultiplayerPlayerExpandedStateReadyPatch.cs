using System.Collections.Generic;
using DisplayName.DisplayNameCode.Multiplayer;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Nodes.Multiplayer;
using MegaCrit.Sts2.Core.Platform;
using MegaCrit.Sts2.Core.Runs;

namespace DisplayName.DisplayNameCode.Patch;

[HarmonyPatch(typeof(NMultiplayerPlayerExpandedState), "_Ready")]
public static class NMultiplayerPlayerExpandedStateReadyPatch
{
    private static readonly List<NMultiplayerPlayerExpandedState> LiveWidgets = new();
    private static bool _subscribed;

    [HarmonyPostfix]
    public static void Postfix(NMultiplayerPlayerExpandedState __instance)
    {
        LiveWidgets.Add(__instance);
        if (!_subscribed)
        {
            _subscribed = true;
            RemoteDisplayNames.Updated += OnRemoteDisplayNameUpdated;
        }

        RefreshName(__instance);
    }

    private static void OnRemoteDisplayNameUpdated(ulong playerId)
    {
        for (int i = LiveWidgets.Count - 1; i >= 0; i--)
        {
            NMultiplayerPlayerExpandedState widget = LiveWidgets[i];
            if (!GodotObject.IsInstanceValid(widget))
            {
                LiveWidgets.RemoveAt(i);
                continue;
            }

            Player? player = Traverse.Create(widget).Field("_player").GetValue<Player>();
            if (player != null && player.NetId == playerId)
            {
                RefreshName(widget);
            }
        }
    }

    private static void RefreshName(NMultiplayerPlayerExpandedState instance)
    {
        Player? player = Traverse.Create(instance).Field("_player").GetValue<Player>();
        if (player == null)
        {
            return;
        }

        PlatformType platform = RunManager.Instance.NetService.Platform;
        string playerName = PlatformUtil.GetPlayerName(platform, player.NetId);

        LocString locString = new("gameplay_ui", "MULTIPLAYER_EXPANDED_STATE.title");
        locString.Add("PlayerName", playerName);
        locString.Add("Character", player.Character.Title);
        instance.GetNode<RichTextLabel>("%PlayerNameLabel").Text = locString.GetFormattedText();
    }
}
