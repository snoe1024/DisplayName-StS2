using System.Collections.Generic;
using DisplayName.DisplayNameCode.Multiplayer;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.addons.mega_text;
using MegaCrit.Sts2.Core.Nodes.Multiplayer;
using MegaCrit.Sts2.Core.Platform;

namespace DisplayName.DisplayNameCode.Patch;

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
