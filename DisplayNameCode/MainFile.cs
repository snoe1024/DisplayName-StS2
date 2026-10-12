using System.Reflection;
using DisplayName.DisplayNameCode.Config;
using DisplayName.DisplayNameCode.Multiplayer;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Modding;

namespace DisplayName.DisplayNameCode;
[ModInitializer(nameof(Initialize))]
public partial class MainFile : Node
{
    public const string ModId = "DisplayName";

    public static MegaCrit.Sts2.Core.Logging.Logger Logger { get; } =
        new(ModId, MegaCrit.Sts2.Core.Logging.LogType.Generic);


    public static void Initialize()
    {
        var assembly = Assembly.GetExecutingAssembly();

        DisplayNameConfig.Initialize();
        DisplayNameNetworking.Initialize();
        DisplayNameConfig.Changed += DisplayNameNetworking.NotifyLocalStateChanged;

        Harmony harmony = new(ModId);

        harmony.PatchAll(assembly);
    }
}
