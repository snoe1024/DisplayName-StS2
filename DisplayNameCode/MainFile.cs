using System.Linq;
using System.Reflection;
using BaseLib.Config;
using DisplayName.DisplayNameCode.Config;
using DisplayName.DisplayNameCode.Multiplayer;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Modding;

namespace DisplayName.DisplayNameCode;

//You're recommended but not required to keep all your code in this package and all your assets in the DisplayName folder.
[ModInitializer(nameof(Initialize))]
public partial class MainFile : Node
{
    public const string ModId = "DisplayName"; //At the moment, this is used only for the Logger and harmony names.

    public static MegaCrit.Sts2.Core.Logging.Logger Logger { get; } =
        new(ModId, MegaCrit.Sts2.Core.Logging.LogType.Generic);

    public static DisplayNameConfig Config { get; private set; } = null!;

    public static void Initialize()
    {
        var assembly = Assembly.GetExecutingAssembly();

        //If you want to use scripts defined in your mod for Godot scenes, uncomment the following line.
        //Godot.Bridge.ScriptManagerBridge.LookupScriptsInAssembly(assembly);

        Config = new DisplayNameConfig();
        Config.Load();
        ModConfigRegistry.Register(ModId, Config);

        // Lets a player who edits their DisplayName while already sitting in a lobby/run immediately
        // re-broadcast it, rather than leaving other clients stuck with whatever was set when they last
        // connected.
        Config.ConfigChanged += (_, _) => DisplayNameNetworking.NotifyLocalStateChanged();

        Harmony harmony = new(ModId);

        harmony.PatchAll(assembly);

        // harmony.PatchAll() can silently patch fewer methods than intended -- a misplaced [HarmonyPatch]
        // attribute (on a method instead of its containing class) or a target-resolution mismatch (wrong
        // overload's parameter types) don't necessarily throw. Logging every actually-patched method's
        // declaring type + name on every startup makes either failure mode directly visible in godot.log
        // instead of only being caught by a confused live-debugging session. See
        // sts2_dev_knowledge/topics/harmony-patching.md.
        var patchedMethods = harmony.GetPatchedMethods().ToList();
        Logger.Info($"DisplayName: Harmony patched {patchedMethods.Count} method(s) on startup:");
        foreach (var method in patchedMethods)
        {
            Logger.Info($"DisplayName:   - {method.DeclaringType?.FullName}.{method.Name}");
        }
    }
}