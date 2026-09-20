using System.Linq;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.Multiplayer.Game.Lobby;
using MegaCrit.Sts2.Core.Nodes.Multiplayer;

namespace DisplayName.DisplayNameCode.Patch;

// TEMPORARY, diagnostic-only. NRemoteLobbyPlayer.Create/_Ready never fire for the local player's own id in
// the character-select lobby, even though vanilla's decompiled source shows Initialize(displayLocalPlayer:
// true) should create a widget for them too. This patches the container itself (one level up) to see
// directly whether OnPlayerConnected is even invoked for the local player id, and what lobby.Players/
// displayLocalPlayer actually look like at Initialize time. Delete once resolved -- not meant to ship.
[HarmonyPatch(typeof(NRemoteLobbyPlayerContainer), nameof(NRemoteLobbyPlayerContainer.Initialize))]
public static class NRemoteLobbyPlayerContainerInitializeDiagnosticPatch
{
    [HarmonyPostfix]
    public static void Postfix(StartRunLobby lobby, bool displayLocalPlayer)
    {
        string playerIds = string.Join(", ", lobby.Players.Select(p => p.id));
        MainFile.Logger.Info($"DisplayName[DIAG]: NRemoteLobbyPlayerContainer.Initialize: displayLocalPlayer={displayLocalPlayer}, lobby.LocalPlayer.id={lobby.LocalPlayer.id}, lobby.Players=[{playerIds}].");
    }
}

[HarmonyPatch(typeof(NRemoteLobbyPlayerContainer), nameof(NRemoteLobbyPlayerContainer.OnPlayerConnected))]
public static class NRemoteLobbyPlayerContainerOnPlayerConnectedDiagnosticPatch
{
    [HarmonyPostfix]
    public static void Postfix(StartRunLobbyPlayer player)
    {
        MainFile.Logger.Info($"DisplayName[DIAG]: NRemoteLobbyPlayerContainer.OnPlayerConnected called for playerId={player.id}.");
    }
}
