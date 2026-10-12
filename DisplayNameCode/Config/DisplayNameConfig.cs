using System.IO;
using Godot;
using STS2RitsuLib;
using STS2RitsuLib.Data;
using STS2RitsuLib.Settings;
using STS2RitsuLib.Utils.Persistence;

namespace DisplayName.DisplayNameCode.Config;

public static class DisplayNameConfig
{
    private const string DataKey = "settings";
    private static ModDataStore? _store;
    private static DisplayNameSettings _settings = new();
    public static event Action? Changed;
    public static string DisplayName => _settings.Name;

    public static void Initialize()
    {
        using (RitsuLibFramework.BeginModDataRegistration(MainFile.ModId, initializeProfileIfReady: false))
        {
            _store = RitsuLibFramework.GetDataStore(MainFile.ModId);
            _store.Register<DisplayNameSettings>(DataKey, "settings.json", SaveScope.Global,
                syncToCloud: false, autoCreateIfMissing: false);
        }
        _store.InitializeGlobal();
        _settings = _store.Get<DisplayNameSettings>(DataKey);
        bool hasNewSettings = _store.HasExistingData(DataKey);
        string legacyPath = Path.Combine(OS.GetUserDataDir(), "mod_configs", "DisplayName.cfg");
        _settings.Name = LegacySettingsMigration.Resolve(hasNewSettings, _settings.Name, legacyPath,
            message => MainFile.Logger.Warn(message));
        if (!hasNewSettings)
        {
            _store.Save(DataKey);
        }

        var binding = new ModSettingsCallbackValueBinding<string>(MainFile.ModId, DataKey,
            SaveScope.Global, () => DisplayName, SetName, () => _store.Save(DataKey));
        RitsuLibFramework.RegisterModSettings(MainFile.ModId, page => page
            .WithTitle(Text("DISPLAYNAME.mod_title", "Display Name"))
            .WithModDisplayName(Text("DISPLAYNAME.mod_title", "Display Name"))
            .WithVisibleOnHostSurfaces(ModSettingsHostSurface.All)
            .AddSection("general", section => section.AddString("display_name",
                Text("DISPLAYNAME-DISPLAY_NAME.title", "Display name"), binding,
                placeholder: Text("DISPLAYNAME-DISPLAY_NAME.placeholder", "Steam username"),
                maxLength: DisplayNameRules.MaxLength,
                description: Text("DISPLAYNAME-DISPLAY_NAME.hover.desc", "Leave empty to use your Steam username."))));
    }

    private static ModSettingsText Text(string key, string fallback) =>
        ModSettingsText.LocString("settings_ui", key, fallback);

    private static void SetName(string value)
    {
        string normalized = DisplayNameRules.Normalize(value);
        if (_settings.Name == normalized) return;
        _settings.Name = normalized;
        Changed?.Invoke();
    }
}
