using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using System.Reflection;
using UnityEngine;

namespace ConfigurableStormController
{
    [BepInPlugin(PLUGIN_GUID, PLUGIN_NAME, PLUGIN_VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        public const string PLUGIN_GUID = "com.example.configurablestormcontroller";
        public const string PLUGIN_NAME = "Configurable Storm Controller";
        public const string PLUGIN_VERSION = "3.3.1";

        internal static ManualLogSource Log;
        private readonly Harmony harmony = new Harmony(PLUGIN_GUID);

        // Config Entries
        internal static ConfigEntry<bool> PluginEnabled;
        internal static ConfigEntry<bool> ModifySnowStorms;
        internal static ConfigEntry<float> SnowStormFrequencyMultiplier;
        internal static ConfigEntry<bool> ModifyRainStorms;
        internal static ConfigEntry<float> RainStormFrequencyMultiplier;

        private void Awake()
        {
            Log = Logger;
            
            PluginEnabled = Config.Bind("1. General", "EnablePlugin", true,
                "Disables all plugin functionality if false. Requires restart.");

            ModifySnowStorms = Config.Bind("2. Snow Storm", "EnableSnowModification", true,
                "Enables/disables modifications to snow storms.");
            SnowStormFrequencyMultiplier = Config.Bind("2. Snow Storm", "SnowFrequencyMultiplier", 1.0f,
                new ConfigDescription("Storm frequency. >1: more, 1: normal, <1: less, 0: disabled.",
                new AcceptableValueRange<float>(0f, 5f)));

            ModifyRainStorms = Config.Bind("3. Rain Storm", "EnableRainModification", true,
                "Enables/disables modifications to rain storms.");
            RainStormFrequencyMultiplier = Config.Bind("3. Rain Storm", "RainFrequencyMultiplier", 1.0f,
                new ConfigDescription("Storm frequency. >1: more, 1: normal, <1: less, 0: disabled.",
                new AcceptableValueRange<float>(0f, 5f)));

            // Subscribe to the event for live config reloading.
            Config.SettingChanged += OnSettingChanged;

            if (PluginEnabled.Value)
            {
                ApplyHarmonyPatches();
            }
        }

        // Logs when a setting is changed in the config file during gameplay.
        private void OnSettingChanged(object sender, SettingChangedEventArgs e)
        {
            Log.LogInfo($"Config setting changed: [{e.ChangedSetting.Definition.Section}] {e.ChangedSetting.Definition.Key}");
        }

        private void ApplyHarmonyPatches()
        {
            MethodInfo original = AccessTools.Method("WindChillZone:GetNextWindTime");
            MethodInfo postfix = AccessTools.Method(typeof(StormPatch), nameof(StormPatch.ModifyStormDurationPostfix));
            harmony.Patch(original, postfix: new HarmonyMethod(postfix));
            Log.LogInfo("Plugin enabled and patched into storm controller.");
        }
    }

    internal class StormPatch
    {
        // Runs after WindChillZone.GetNextWindTime to modify the calm period between storms.
        public static void ModifyStormDurationPostfix(MonoBehaviour __instance, bool windActive, ref float __result)
        {
            if (windActive) return;

            StormVisual stormVisual = __instance.GetComponent<StormVisual>();
            if (stormVisual == null) return;

            if (stormVisual.stormType == StormVisual.StormType.Snow && Plugin.ModifySnowStorms.Value)
            {
                ApplyFrequencyModification("SNOW", Plugin.SnowStormFrequencyMultiplier.Value, ref __result);
            }
            else if (stormVisual.stormType == StormVisual.StormType.Rain && Plugin.ModifyRainStorms.Value)
            {
                ApplyFrequencyModification("RAIN", Plugin.RainStormFrequencyMultiplier.Value, ref __result);
            }
        }
        
        // Helper method to apply the frequency multiplier to the storm timer.
        private static void ApplyFrequencyModification(string stormTypeName, float userMultiplier, ref float timerResult)
        {
            if (Mathf.Approximately(userMultiplier, 1.0f)) return;

            float originalTime = timerResult;

            if (userMultiplier <= 0.0f) {
                timerResult = float.MaxValue;
            } else {
                timerResult /= userMultiplier;
            }
            
            if (!Mathf.Approximately(originalTime, timerResult))
            {
                Plugin.Log.LogInfo($"Applied new {stormTypeName} frequency. Calm Period: {timerResult:F1}s (Multiplier: {userMultiplier}x)");
            }
        }
    }
}
