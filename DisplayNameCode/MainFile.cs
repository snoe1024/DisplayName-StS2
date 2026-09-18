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

        // Lets a player who flips ShowSteamIdAlongside while already sitting in a lobby/run immediately
        // re-broadcast that consent flag, rather than leaving other clients stuck with whatever was true
        // when they last connected.
        Config.ConfigChanged += (_, _) => DisplayNameNetworking.NotifyLocalStateChanged();

        Harmony harmony = new(ModId);

        harmony.PatchAll(assembly);
    }
}