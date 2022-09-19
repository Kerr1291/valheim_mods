using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;

namespace VEX
{
    // Token: 0x02000004 RID: 4
    internal static class Settings
    {
        // Token: 0x06000005 RID: 5 RVA: 0x0000210C File Offset: 0x0000030C
        static Settings()
        {
            Settings.typeStr.Add(typeof(bool), "bool");
            Settings.typeStr.Add(typeof(int), "int");
            Settings.typeStr.Add(typeof(float), "float");
            Settings.typeStr.Add(typeof(KeyboardShortcut), "keybind");
        }

        // Token: 0x06000006 RID: 6 RVA: 0x00002188 File Offset: 0x00000388
        public static void InitConfig(ConfigFile config)
        {
            MethodInfo methodInfo = AccessTools.Method(typeof(ConfigEntryBase), "WriteDescription", null, null);
            MethodInfo methodInfo2 = AccessTools.Method(typeof(Settings), "MyWriteDescription", null, null);
            VEX.harmony.Patch(methodInfo, new HarmonyMethod(methodInfo2), null, null, null);
            Settings.configFile = config;
            Settings.configFile.Bind<int>("Tools", "Version", 1, "Configuration Version");

            //Settings.resetRadius = Settings.configFile.Bind<float>("Tools", "ResetRadius", 5f, "Default reset radius if not specified");
            Settings.restoreMax = Settings.configFile.Bind<int>("Tools", "VEX_RestoreMax", 32, "Maximum number of restores available");
            Settings.lightDistance = Settings.configFile.Bind<float>("Debug", "VEX_LightDistance", 25f, "Distance of visible debug lights");
            Settings.lightStrength = Settings.configFile.Bind<float>("Debug", "VEX_LightStrength", 0.5f, "Strength of debug lights");
            Settings.debugToggle = Settings.configFile.Bind<KeyboardShortcut>("Debug", "VEX_Toggle", new KeyboardShortcut(KeyCode.F3, Array.Empty<KeyCode>()), "VEX debug toggle keybind");
        }

        // Token: 0x06000007 RID: 7 RVA: 0x000022B0 File Offset: 0x000004B0
        public static bool MyWriteDescription(ref ConfigEntryBase __instance, ref StreamWriter writer)
        {
            if (__instance.ConfigFile == Settings.configFile)
            {
                string name;
                if (!Settings.typeStr.TryGetValue(__instance.SettingType, out name))
                {
                    name = __instance.SettingType.Name;
                }
                string text = name + ", default: " + TomlTypeConverter.ConvertToString(__instance.DefaultValue, __instance.SettingType);
                if (!string.IsNullOrEmpty(__instance.Description.Description))
                {
                    writer.WriteLine(string.Concat(new string[]
                    {
                        "# ",
                        __instance.Description.Description.Replace("\n", "\n# "),
                        " (",
                        text,
                        ")"
                    }));
                }
                else
                {
                    writer.WriteLine("# " + text);
                }
                if (__instance.Description.AcceptableValues != null)
                {
                    writer.WriteLine(__instance.Description.AcceptableValues.ToDescriptionString());
                }
                else if (__instance.SettingType.IsEnum)
                {
                    writer.WriteLine("# Acceptable values: " + string.Join(", ", Enum.GetNames(__instance.SettingType)));
                    if (__instance.SettingType.GetCustomAttributes(typeof(FlagsAttribute), true).Any<object>())
                    {
                        writer.WriteLine("# Multiple values can be set at the same time by separating them with , (e.g. Debug, Warning)");
                    }
                }
                return false;
            }
            return true;
        }

        // Token: 0x04000005 RID: 5
        private static ConfigFile configFile;

        // Token: 0x04000006 RID: 6
        private static Dictionary<Type, string> typeStr = new Dictionary<Type, string>();

        //// Token: 0x04000007 RID: 7
        //internal static ConfigEntry<float> resetRadius;

        // Token: 0x04000008 RID: 8
        internal static ConfigEntry<int> restoreMax;

        // Token: 0x04000009 RID: 9
        internal static ConfigEntry<float> lightDistance;

        // Token: 0x0400000A RID: 10
        internal static ConfigEntry<float> lightStrength;

        // Token: 0x0400000B RID: 11
        internal static ConfigEntry<KeyboardShortcut> debugToggle;
    }
}
