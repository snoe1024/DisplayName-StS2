using DisplayName.DisplayNameCode.Config;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.Nodes.Multiplayer;
using MegaCrit.Sts2.Core.Platform;

namespace DisplayName.DisplayNameCode.Patch;

// TEMPORARY, diagnostic-only. godot.log shows PlatformUtil.GetPlayerNameRaw is never called for the local
// player's own ID while the character-select lobby is open (only for the remote player) -- this patch logs
// every NRemoteLobbyPlayer widget actually created, so we can see whether a widget for the local player is
// created at all in that screen. Delete once resolved -- not meant to ship.
[HarmonyPatch(typeof(NRemoteLobbyPlayer), nameof(NRemoteLobbyPlayer.Create), typeof(StartRunLobbyPlayer), typeof(PlatformType), typeof(bool))]
public static class NRemoteLobbyPlayerCreateDiagnosticPatch
{
    [HarmonyPostfix]
    public static void Postfix(StartRunLobbyPlayer player, PlatformType platform)
    {
        bool isLocal = player.id == PlatformUtil.GetLocalPlayerId(platform);
        MainFile.Logger.Info($"DisplayName[DIAG]: NRemoteLobbyPlayer.Create called for playerId={player.id} (isLocal={isLocal}, configuredDisplayName='{DisplayNameConfig.DisplayName}').");
    }
}
