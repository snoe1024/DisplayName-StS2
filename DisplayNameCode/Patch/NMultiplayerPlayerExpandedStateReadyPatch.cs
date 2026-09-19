using DisplayName.DisplayNameCode.Config;
using DisplayName.DisplayNameCode.Multiplayer;
using DisplayName.DisplayNameCode.Storage;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Nodes.Multiplayer;
using MegaCrit.Sts2.Core.Platform;
using MegaCrit.Sts2.Core.Runs;

namespace DisplayName.DisplayNameCode.Patch;

/// <summary>
/// NMultiplayerPlayerExpandedState is the player-inventory screen (relics/potions/deck) shown when you
/// click another player during a run. If THAT PLAYER has opted in to ShowSteamIdAlongside, this adds a
/// small "(SteamUsername)" label anchored to the bottom-right corner of the player's name label. This is
/// the subject's own privacy choice, not the local viewer's -- except when the subject IS the local player
/// (see below), the local viewer's own DisplayNameConfig.ShowSteamIdAlongside value is irrelevant here.
///
/// Vanilla only ever opens this screen for a genuinely remote player (its own doc comment says so), so the
/// subject's consent is normally read from RemoteShowSteamIdFlags (broadcast via DisplayNameSyncMessage).
/// But RemoteShowSteamIdFlags is deliberately never populated for the LOCAL player's own id (HandleMessage
/// filters out self-sent messages), so a tool that lets you open your own player screen -- e.g. a
/// same-account local-network mirroring mod, confirmed via godot.log during 2026-09 testing to reuse the
/// host's own NetId for its "remote" view -- would otherwise always suppress this for yourself regardless
/// of your own setting. Falling back to DisplayNameConfig.ShowSteamIdAlongside when the subject is the
/// local player fixes that without changing anything about how a genuinely remote player's own consent is
/// read.
/// </summary>
[HarmonyPatch(typeof(NMultiplayerPlayerExpandedState), "_Ready")]
public static class NMultiplayerPlayerExpandedStateReadyPatch
{
    [HarmonyPostfix]
    public static void Postfix(NMultiplayerPlayerExpandedState __instance)
    {
        Player? player = Traverse.Create(__instance).Field("_player").GetValue<Player>();
        if (player == null)
        {
            return;
        }

        bool isLocalPlayer = player.NetId == PlatformUtil.GetLocalPlayerId(RunManager.Instance.NetService.Platform);
        bool subjectWantsSteamIdShown = isLocalPlayer ? DisplayNameConfig.ShowSteamIdAlongside : RemoteShowSteamIdFlags.Get(player.NetId);
        if (!subjectWantsSteamIdShown)
        {
            return;
        }

        // Triggers PlatformUtilGetPlayerNameRawPatch, which (as a side effect) refreshes
        // RawPlatformNameCache for this player, whether or not we care about its return value here.
        PlatformUtil.GetPlayerNameRaw(RunManager.Instance.NetService.Platform, player.NetId);
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
