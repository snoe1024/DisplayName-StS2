using System.Linq;
using System.Reflection;
using DisplayName.DisplayNameCode;
using DisplayName.DisplayNameCode.Config;
using DisplayName.DisplayNameCode.Multiplayer;
using DisplayName.DisplayNameCode.Storage;
using HarmonyLib;
using MegaCrit.Sts2.Core.Platform;

namespace DisplayName.DisplayNameCode.Patch;

/// <summary>
/// Substitutes a resolved player name with their DisplayName wherever the game resolves a player name
/// (nameplates, hover tips, etc. all funnel through this single method):
/// - For the local player, always uses this account's own <see cref="DisplayNameConfig.DisplayName"/>.
/// - For remote players, uses whatever they broadcast via <see cref="DisplayNameNetworking"/>, if anything.
/// If nothing is set (empty DisplayName, or no sync message received yet for that player), falls through
/// to the vanilla result untouched, i.e. their Steam username.
/// </summary>
[HarmonyPatch(typeof(PlatformUtil), nameof(PlatformUtil.GetPlayerNameRaw))]
public static class PlatformUtilGetPlayerNameRawPatch
{
    private static bool _loggedPatchInfoOnce;

    [HarmonyPostfix]
    public static void Postfix(PlatformType platformType, ulong playerId, ref string __result)
    {
        if (!_loggedPatchInfoOnce)
        {
            // Deferred to the first real call (rather than logged eagerly from MainFile.Initialize) so
            // that every OTHER mod has definitely finished its own [ModInitializer]'s harmony.PatchAll()
            // by the time we ask Harmony who else has patched this method -- mod init order across
            // different mods is not something we can rely on otherwise.
            _loggedPatchInfoOnce = true;
            LogPatchInfo();
        }

        // __result is still whatever vanilla resolved (the Steam username) at this point -- cache it so
        // UI that wants "the underlying platform username" can recover it even after we override
        // __result below (see NMultiplayerPlayerExpandedStateReadyPatch / DisplayNameConfig.ShowSteamIdAlongside).
        RawPlatformNameCache.Set(playerId, __result);

        // Logged unconditionally (not just on the local branch) so we can directly see, for every single
        // call, whether playerId actually matches GetLocalPlayerId(platformType) -- if it mysteriously
        // doesn't for what should be the local player's own nameplate, that alone would explain the local
        // override silently never firing without needing to suspect another mod at all.
        ulong localPlayerId = PlatformUtil.GetLocalPlayerId(platformType);
        MainFile.Logger.Info($"DisplayName: GetPlayerNameRaw called: platformType={platformType}, playerId={playerId}, localPlayerId={localPlayerId}, vanillaResult='{__result}'.");

        if (playerId == localPlayerId)
        {
            string displayName = DisplayNameConfig.DisplayName;
            if (!string.IsNullOrWhiteSpace(displayName))
            {
                __result = displayName;
            }
            MainFile.Logger.Info($"DisplayName: -> treated as LOCAL player. DisplayName='{displayName}' -> returning __result='{__result}'.");
            return;
        }

        if (RemoteDisplayNames.TryGet(playerId, out string remoteDisplayName) && !string.IsNullOrWhiteSpace(remoteDisplayName))
        {
            __result = remoteDisplayName;
            MainFile.Logger.Info($"DisplayName: -> treated as REMOTE player with known DisplayName. Returning __result='{__result}'.");
        }
        else
        {
            MainFile.Logger.Info($"DisplayName: -> treated as REMOTE player with NO known DisplayName (RemoteDisplayNames has no entry, or it's empty). Leaving vanilla result as-is: '{__result}'.");
        }
    }

    // Logs every Prefix/Postfix/Transpiler/Finalizer (from ANY mod, including this one) currently applied
    // to PlatformUtil.GetPlayerNameRaw, with owner id, priority and index -- the only way to conclusively
    // tell whether another mod also touches this exact method and could be overwriting __result after us
    // (Harmony's cross-mod patch ordering is otherwise invisible from inside a single mod).
    private static void LogPatchInfo()
    {
        MethodBase? original = AccessTools.Method(typeof(PlatformUtil), nameof(PlatformUtil.GetPlayerNameRaw));
        if (original == null)
        {
            MainFile.Logger.Warn("DisplayName: could not resolve PlatformUtil.GetPlayerNameRaw via AccessTools for patch-info logging.");
            return;
        }

        Patches? patches = Harmony.GetPatchInfo(original);
        if (patches == null)
        {
            MainFile.Logger.Info("DisplayName: Harmony.GetPatchInfo returned null for PlatformUtil.GetPlayerNameRaw (no patches at all?).");
            return;
        }

        string Describe(HarmonyLib.Patch p) => $"{p.owner}(index={p.index}, priority={p.priority})";
        MainFile.Logger.Info(
            "DisplayName: PlatformUtil.GetPlayerNameRaw patch info -- "
            + $"Prefixes=[{string.Join(", ", patches.Prefixes.Select(Describe))}] "
            + $"Postfixes=[{string.Join(", ", patches.Postfixes.Select(Describe))}] "
            + $"Transpilers=[{string.Join(", ", patches.Transpilers.Select(Describe))}] "
            + $"Finalizers=[{string.Join(", ", patches.Finalizers.Select(Describe))}]");
    }
}
