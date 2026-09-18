using DisplayName.DisplayNameCode.Multiplayer;
using DisplayName.DisplayNameCode.Storage;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Nodes.Multiplayer;

namespace DisplayName.DisplayNameCode.Patch;

/// <summary>
/// NMultiplayerPlayerExpandedState is the player-inventory screen (relics/potions/deck) shown when you
/// click another player during a run. If THAT PLAYER has opted in to ShowSteamIdAlongside (broadcast via
/// DisplayNameSyncMessage -- see RemoteShowSteamIdFlags), this adds a small "(SteamUsername)" label
/// anchored to the bottom-right corner of the player's name label. This is the subject's own privacy
/// choice, not the local viewer's: the local viewer's own DisplayNameConfig.ShowSteamIdAlongside value is
/// irrelevant here.
/// </summary>
[HarmonyPatch(typeof(NMultiplayerPlayerExpandedState), "_Ready")]
public static class NMultiplayerPlayerExpandedStateReadyPatch
{
    [HarmonyPostfix]
    public static void Postfix(NMultiplayerPlayerExpandedState __instance)
    {
        Player? player = Traverse.Create(__instance).Field("_player").GetValue<Player>();
        if (player == null || !RemoteShowSteamIdFlags.Get(player.NetId))
        {
            return;
        }

        // Triggers PlatformUtilGetPlayerNameRawPatch, which (as a side effect) refreshes
        // RawPlatformNameCache for this player, whether or not we care about its return value here.
        MegaCrit.Sts2.Core.Platform.PlatformUtil.GetPlayerNameRaw(MegaCrit.Sts2.Core.Runs.RunManager.Instance.NetService.Platform, player.NetId);
        if (!RawPlatformNameCache.TryGet(player.NetId, out string steamName) || string.IsNullOrWhiteSpace(steamName))
        {
            return;
        }

        Control nameLabel = __instance.GetNode<Control>("%PlayerNameLabel");
        Label steamNameLabel = new()
        {
            Text = $"({steamName})",
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Bottom,
            MouseFilter = Control.MouseFilterEnum.Ignore,
            ClipText = false,
        };
        steamNameLabel.AddThemeFontSizeOverride("font_size", 14);
        steamNameLabel.SetAnchorsPreset(Control.LayoutPreset.BottomRight);
        nameLabel.AddChild(steamNameLabel);
    }
}
