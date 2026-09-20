using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using DisplayName.DisplayNameCode;
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
    private static bool _loggedPatchInfoOnce;

    [HarmonyPostfix]
    public static void Postfix(NRemoteLobbyPlayer __instance)
    {
        LivePlayers.Add(__instance);
        if (!_subscribed)
        {
            _subscribed = true;
            RemoteDisplayNames.Updated += OnRemoteDisplayNameUpdated;
        }

        // TEMPORARY diagnostic: confirms whether vanilla's own _Ready() body (which ran just before this
        // postfix) actually set the nameplate via PlatformUtil.GetPlayerNameRaw for this instance, and
        // what _isSingleplayer was read as -- see NRemoteLobbyPlayerCreateDiagnosticPatch.cs. A real
        // playtest showed the local player's own widget ending up with the raw Steam username even though
        // isSingleplayer read false here (so vanilla's own body should have called
        // PlatformUtil.GetPlayerNameRaw) AND PlatformUtilGetPlayerNameRawPatch's postfix never logged a
        // call for that id at all -- meaning something intercepted _Ready() itself before vanilla's body
        // (or GetPlayerNameRaw) ever ran. Logging Harmony.GetPatchInfo for _Ready once to check for a
        // competing prefix from another mod. Delete once resolved.
        if (!_loggedPatchInfoOnce)
        {
            _loggedPatchInfoOnce = true;
            LogReadyPatchInfo();
        }

        bool isSingleplayer = Traverse.Create(__instance).Field("_isSingleplayer").GetValue<bool>();
        string nameplateTextNow = __instance.GetNode<MegaLabel>("%NameplateLabel").Text;
        MainFile.Logger.Info($"DisplayName[DIAG]: NRemoteLobbyPlayer._Ready postfix for playerId={__instance.PlayerId}: isSingleplayer={isSingleplayer}, nameplateText='{nameplateTextNow}'.");
    }

    private static void LogReadyPatchInfo()
    {
        MethodBase? original = AccessTools.Method(typeof(NRemoteLobbyPlayer), "_Ready");
        if (original == null)
        {
            MainFile.Logger.Warn("DisplayName[DIAG]: could not resolve NRemoteLobbyPlayer._Ready via AccessTools.");
            return;
        }

        Patches? patches = Harmony.GetPatchInfo(original);
        if (patches == null)
        {
            MainFile.Logger.Info("DisplayName[DIAG]: Harmony.GetPatchInfo returned null for NRemoteLobbyPlayer._Ready.");
            return;
        }

        string Describe(HarmonyLib.Patch p) => $"{p.owner}(index={p.index}, priority={p.priority})";
        MainFile.Logger.Info(
            "DisplayName[DIAG]: NRemoteLobbyPlayer._Ready patch info -- "
            + $"Prefixes=[{string.Join(", ", patches.Prefixes.Select(Describe))}] "
            + $"Postfixes=[{string.Join(", ", patches.Postfixes.Select(Describe))}] "
            + $"Transpilers=[{string.Join(", ", patches.Transpilers.Select(Describe))}] "
            + $"Finalizers=[{string.Join(", ", patches.Finalizers.Select(Describe))}]");
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
