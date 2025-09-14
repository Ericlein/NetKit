using System;
using System.IO;
using System.Text.Json;

namespace NetKit;

public class HotkeySettings
{
    public bool UseCtrl { get; set; }
    public bool UseAlt { get; set; }
    public bool UseWin { get; set; } = true;
    public int VirtualKeyCode { get; set; } = 0xDE; // Default to apostrophe
    public string KeyDisplay { get; set; } = "'";
}

public class AppSettings
{
    public HotkeySettings Hotkey { get; set; } = new();
}

public class SettingsService
{
    private readonly string _settingsPath;
    private AppSettings _settings;

    public AppSettings Settings => _settings;

    public SettingsService()
    {
        var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var netKitFolder = Path.Combine(appDataPath, "NetKit");

        if (!Directory.Exists(netKitFolder))
        {
            Directory.CreateDirectory(netKitFolder);
        }

        _settingsPath = Path.Combine(netKitFolder, "settings.json");
        _settings = LoadSettings();
    }

    private AppSettings LoadSettings()
    {
        try
        {
            if (File.Exists(_settingsPath))
            {
                var json = File.ReadAllText(_settingsPath);
                var settings = JsonSerializer.Deserialize<AppSettings>(json);
                return settings ?? new AppSettings();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to load settings: {ex.Message}");
        }

        return new AppSettings();
    }

    public void SaveSettings()
    {
        try
        {
            var json = JsonSerializer.Serialize(_settings, new JsonSerializerOptions
            {
                WriteIndented = true
            });
            File.WriteAllText(_settingsPath, json);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to save settings: {ex.Message}");
        }
    }

    public void UpdateHotkey(bool useCtrl, bool useAlt, bool useWin, int virtualKeyCode, string keyDisplay)
    {
        _settings.Hotkey.UseCtrl = useCtrl;
        _settings.Hotkey.UseAlt = useAlt;
        _settings.Hotkey.UseWin = useWin;
        _settings.Hotkey.VirtualKeyCode = virtualKeyCode;
        _settings.Hotkey.KeyDisplay = keyDisplay;
        SaveSettings();
    }

    public string GetHotkeyDisplayText()
    {
        var modifiers = new List<string>();

        if (_settings.Hotkey.UseCtrl) modifiers.Add("Ctrl");
        if (_settings.Hotkey.UseAlt) modifiers.Add("Alt");
        if (_settings.Hotkey.UseWin) modifiers.Add("Win");

        var modifierText = modifiers.Count > 0 ? string.Join(" + ", modifiers) + " + " : "";
        return $"{modifierText}{_settings.Hotkey.KeyDisplay}";
    }
}