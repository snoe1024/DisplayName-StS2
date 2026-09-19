using DisplayName.DisplayNameCode.Config;
using DisplayName.DisplayNameCode.Multiplayer;
using DisplayName.DisplayNameCode.Storage;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Nodes.Multiplayer;
using MegaCrit.Sts2.Core.Platform;
using MegaCrit.Sts2.Core.Runs;

namespace DisplayName.DisplayNameCode.Patch;

/// <summary>
/// NMultiplayerPlayerExpandedState is the player-inventory screen (relics/potions/deck) shown when you
/// click another player during a run. If THAT PLAYER has opted in to ShowSteamIdAlongside, this appends a
/// small "(SteamUsername)" right after their DisplayName, inline within the same centered title text
/// vanilla already builds (rather than a separately-positioned label overlapping the corner of the
/// screen -- an earlier version did that and looked disconnected from the name it was supposed to
/// annotate). This is the subject's own privacy choice, not the local viewer's -- except when the subject
/// IS the local player (see below), the local viewer's own DisplayNameConfig.ShowSteamIdAlongside value is
/// irrelevant here.
///
/// Vanilla builds the title as (Core/Nodes/Multiplayer/NMultiplayerPlayerExpandedState.cs, and
/// gameplay_ui.json's "MULTIPLAYER_EXPANDED_STATE.title" = "[center][blue]{PlayerName}[/blue] [gold]{Character}[/gold][/center]"):
///   LocString locString = new LocString("gameplay_ui", "MULTIPLAYER_EXPANDED_STATE.title");
///   locString.Add("PlayerName", PlatformUtil.GetPlayerName(platform, _player.NetId));
///   locString.Add("Character", _player.Character.Title);
///   _playerNameLabel.Text = locString.GetFormattedText();
/// We rebuild that SAME LocString (same key, so any language's word order/template is respected) but with
/// our own BBCode-wrapped "(SteamUsername)" appended to the PlayerName value being substituted in, then
/// overwrite the label's Text with it. This only changes what gets plugged into {PlayerName}, not the
/// template itself.
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

        PlatformType platform = RunManager.Instance.NetService.Platform;
        bool isLocalPlayer = player.NetId == PlatformUtil.GetLocalPlayerId(platform);
        bool subjectWantsSteamIdShown = isLocalPlayer ? DisplayNameConfig.ShowSteamIdAlongside : RemoteShowSteamIdFlags.Get(player.NetId);
        if (!subjectWantsSteamIdShown)
        {
            return;
        }

        // Triggers PlatformUtilGetPlayerNameRawPatch, which (as a side effect) refreshes
        // RawPlatformNameCache for this player, whether or not we care about its return value here.
        PlatformUtil.GetPlayerNameRaw(platform, player.NetId);
        if (!RawPlatformNameCache.TryGet(player.NetId, out string steamName) || string.IsNullOrWhiteSpace(steamName))
        {
            return;
        }

        string displayName = PlatformUtil.GetPlayerName(platform, player.NetId);
        string combinedPlayerName = $"{displayName}[font_size=14]({steamName.EscapeBbcodeTags()})[/font_size]";

        LocString locString = new("gameplay_ui", "MULTIPLAYER_EXPANDED_STATE.title");
        locString.Add("PlayerName", combinedPlayerName);
        locString.Add("Character", player.Character.Title);
        __instance.GetNode<RichTextLabel>("%PlayerNameLabel").Text = locString.GetFormattedText();
    }
}
