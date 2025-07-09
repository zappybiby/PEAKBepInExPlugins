using BepInEx;
using BepInEx.Logging;
using SettingsExtender; // Depends on https://thunderstore.io/c/peak/p/JSPAPP/Settings_Extender/
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.SceneManagement;
using Zorro.Settings;

// A single namespace for the entire plugin
namespace ConfigurableStormController
{
    #region Main Plugin Class
    [BepInPlugin(PLUGIN_GUID, PLUGIN_NAME, PLUGIN_VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        public const string PLUGIN_GUID = "com.example.configurablestormcontroller";
        public const string PLUGIN_NAME = "Configurable Storm Controller";
        public const string PLUGIN_VERSION = "1.0.1";
        public const string SETTINGS_PAGE_NAME = "Storm Controller";

        internal static ManualLogSource Log = null!;

        private void Awake()
        {
            Log = Logger;
            SettingsRegistry.Register(SETTINGS_PAGE_NAME);
            SceneManager.sceneLoaded += OnSceneLoaded;
            Log.LogInfo($"{PLUGIN_NAME} is loaded and ready.");
        }

        private void Start()
        {
            var handler = SettingsHandler.Instance;
            handler.AddSetting(new EnableModSetting());
            handler.AddSetting(new EnableRainSetting());
            handler.AddSetting(new RainFrequencySetting());
            handler.AddSetting(new RainDurationSetting());
            handler.AddSetting(new EnableSnowSetting());
            handler.AddSetting(new SnowFrequencySetting());
            handler.AddSetting(new SnowDurationSetting());
            Log.LogInfo("Added all storm settings to the in-game UI.");
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.name.StartsWith("Level_"))
            {
                Log.LogInfo($"Game scene '{scene.name}' loaded. Initializing Storm Controller...");
                StormController.Initialize();
            }
        }
    }
    #endregion

    #region Storm Controller Logic
    public static class StormController
    {
        // FIX: Initialize with null! to suppress CS8618. We guarantee they are set in Initialize before use.
        private static WindChillZone _rainZone = null!;
        private static WindChillZone _snowZone = null!;
        private static Vector2 _defaultRainOn, _defaultRainOff;
        private static Vector2 _defaultSnowOn, _defaultSnowOff;
        private static bool _isInitialized;

        public static void Initialize()
        {
            var zones = Object.FindObjectsByType<WindChillZone>(FindObjectsSortMode.None);
            _rainZone = null!; _snowZone = null!; // Temporarily satisfy compiler before loop
            foreach (var zone in zones)
            {
                var visual = zone.GetComponentInChildren<StormVisual>(true);
                if (visual.stormType == StormVisual.StormType.Rain) _rainZone = zone;
                else if (visual.stormType == StormVisual.StormType.Snow) _snowZone = zone;
            }

            if (_rainZone == null || _snowZone == null)
            {
                Plugin.Log.LogError("Failed to find both Rain and Snow storm controllers!");
                _isInitialized = false;
                return;
            }
            
            _defaultRainOn = _rainZone.windTimeRangeOn;
            _defaultRainOff = _rainZone.windTimeRangeOff;
            _defaultSnowOn = _snowZone.windTimeRangeOn;
            _defaultSnowOff = _snowZone.windTimeRangeOff;
            
            _isInitialized = true;
            Plugin.Log.LogInfo("Storm Controller initialized successfully.");
            ApplyAllSettings();
        }

        public static void ApplyAllSettings()
        {
            if (!_isInitialized) return;

            var settings = SettingsHandler.Instance;
            bool isModEnabled = settings.GetSetting<EnableModSetting>().Value;
            
            Plugin.Log.LogInfo($"Applying all settings. Master switch is {(isModEnabled ? "ON" : "OFF")}.");

            if (!isModEnabled)
            {
                RestoreVanillaValues();
                return;
            }
            
            bool rainEnabled = settings.GetSetting<EnableRainSetting>().Value;
            if (rainEnabled)
            {
                float rainFreq = settings.GetSetting<RainFrequencySetting>().Value;
                float rainDur = settings.GetSetting<RainDurationSetting>().Value;
                _rainZone.windTimeRangeOff = new Vector2(_defaultRainOff.x / rainFreq, _defaultRainOff.y / rainFreq);
                _rainZone.windTimeRangeOn = new Vector2(rainDur * 0.9f, rainDur * 1.1f);
            }
            else { _rainZone.windTimeRangeOff = new Vector2(float.MaxValue, float.MaxValue); if (_rainZone.windActive) _rainZone.windActive = false; }
            
            bool snowEnabled = settings.GetSetting<EnableSnowSetting>().Value;
            if (snowEnabled)
            {
                float snowFreq = settings.GetSetting<SnowFrequencySetting>().Value;
                float snowDur = settings.GetSetting<SnowDurationSetting>().Value;
                _snowZone.windTimeRangeOff = new Vector2(_defaultSnowOff.x / snowFreq, _defaultSnowOff.y / snowFreq);
                _snowZone.windTimeRangeOn = new Vector2(snowDur * 0.9f, snowDur * 1.1f);
            }
            else { _snowZone.windTimeRangeOff = new Vector2(float.MaxValue, float.MaxValue); if (_snowZone.windActive) _snowZone.windActive = false; }
        }

        private static void RestoreVanillaValues()
        {
            if (!_isInitialized) return;
            Plugin.Log.LogInfo("Restoring vanilla storm behavior.");
            _rainZone.windTimeRangeOn = _defaultRainOn;
            _rainZone.windTimeRangeOff = _defaultRainOff;
            _snowZone.windTimeRangeOn = _defaultSnowOn;
            _snowZone.windTimeRangeOff = _defaultSnowOff;
        }
    }
    #endregion

    #region UI Settings Classes
    public class EnableModSetting : BoolSetting, IExposedSetting
    {
        public string GetDisplayName() => "Enable Storm Controller";
        protected override bool GetDefaultValue() => true;
        public string GetCategory() => SettingsRegistry.GetPageId(Plugin.SETTINGS_PAGE_NAME);
        public override void ApplyValue() => StormController.ApplyAllSettings();
        // FIX: Use null-forgiving operator (!) to suppress CS8625.
        // This tells the compiler that null is an intentional value handled by the base class.
        public override LocalizedString OffString => null!;
        public override LocalizedString OnString => null!;
    }

    public abstract class StepFloatSetting : FloatSetting, IExposedSetting
    {
        protected abstract float GetStep();
        public override float Clamp(float value) { float step = GetStep(); if (step > 0) value = Mathf.Round(value / step) * step; return base.Clamp(value); }
        public abstract string GetDisplayName();
        public string GetCategory() => SettingsRegistry.GetPageId(Plugin.SETTINGS_PAGE_NAME);
        public override void ApplyValue() => StormController.ApplyAllSettings();
    }
    
    public abstract class StormToggleSetting : BoolSetting, IExposedSetting
    {
        public abstract string GetDisplayName();
        public string GetCategory() => SettingsRegistry.GetPageId(Plugin.SETTINGS_PAGE_NAME);

        public override LocalizedString OffString => null!;
        public override LocalizedString OnString => null!;
        public override void ApplyValue() => StormController.ApplyAllSettings();
    }
    
    public class EnableRainSetting : StormToggleSetting { public override string GetDisplayName() => "Enable Rain"; protected override bool GetDefaultValue() => true; }
    public class RainFrequencySetting : StepFloatSetting { public override string GetDisplayName() => "Rain Frequency"; protected override float GetDefaultValue() => 1.0f; protected override float2 GetMinMaxValue() => new(0.1f, 2f); protected override float GetStep() => 0.1f; public override string Expose(float result) => result.ToString("F1") + "x"; }
    public class RainDurationSetting : StepFloatSetting { public override string GetDisplayName() => "Rain Duration"; protected override float GetDefaultValue() => 30f; protected override float2 GetMinMaxValue() => new(15f, 90f); protected override float GetStep() => 5f; public override string Expose(float result) => result.ToString("F0") + "s"; }

    public class EnableSnowSetting : StormToggleSetting { public override string GetDisplayName() => "Enable Snow"; protected override bool GetDefaultValue() => true; }
    public class SnowFrequencySetting : StepFloatSetting { public override string GetDisplayName() => "Snow Frequency"; protected override float GetDefaultValue() => 1.0f; protected override float2 GetMinMaxValue() => new(0.1f, 2f); protected override float GetStep() => 0.1f; public override string Expose(float result) => result.ToString("F1") + "x"; }
    public class SnowDurationSetting : StepFloatSetting { public override string GetDisplayName() => "Snow Duration"; protected override float GetDefaultValue() => 20f; protected override float2 GetMinMaxValue() => new(15f, 90f); protected override float GetStep() => 5f; public override string Expose(float result) => result.ToString("F0") + "s"; }
    #endregion
}
