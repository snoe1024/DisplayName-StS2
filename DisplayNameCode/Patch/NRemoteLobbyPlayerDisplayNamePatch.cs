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
/// nameplate). This patch always re-resolves and re-sets the nameplate itself right after _Ready() runs,
/// rather than trusting vanilla's one-shot resolution: a remote player's DisplayName can only arrive after
/// a network round-trip, so it often lands after this widget already rendered their Steam username; and
/// separately, the very first NRemoteLobbyPlayer created in a session (always the local player's own
/// widget, since it's created before any remote player has joined) has been observed keeping the raw
/// platform username even when PlatformUtilGetPlayerNameRawPatch's own postfix never fires for it at all --
/// something about that specific first call bypasses our patch on PlatformUtil.GetPlayerNameRaw entirely.
/// Re-resolving explicitly from our own code, after the fact, sidesteps needing to understand exactly why.
/// Also tracks every live instance and re-applies this whenever a DisplayNameSyncMessage updates that
/// specific remote player later.
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
