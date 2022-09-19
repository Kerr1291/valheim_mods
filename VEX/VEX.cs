using System;
using System.Reflection;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;

namespace VEX
{
    // Token: 0x02000005 RID: 5
    [BepInPlugin("VEX", "VEX", "1.0.0")]
    public class VEX : BaseUnityPlugin
    {
        // Token: 0x06000008 RID: 8 RVA: 0x0000240C File Offset: 0x0000060C
        public void Awake()
        {
            VEX.Logger = base.Logger;
            Settings.InitConfig(base.Config);
            VEX.harmony.PatchAll(Assembly.GetExecutingAssembly());
            int num = 0;
            foreach (MethodBase methodBase in VEX.harmony.GetPatchedMethods())
            {
                VEX.Logger.LogInfo("Patched " + methodBase.DeclaringType.Name + "." + methodBase.Name);
                num++;
            }
            VEX.Logger.LogInfo(num.ToString() + " patches applied\n");
        }

        // Token: 0x0400000C RID: 12
        public const string GUID = "VEX";

        // Token: 0x0400000D RID: 13
        public const string PluginName = "VEX";

        // Token: 0x0400000E RID: 14
        public const string Version = "1.0.0";

        // Token: 0x0400000F RID: 15
        internal static ManualLogSource Logger;

        // Token: 0x04000010 RID: 16
        internal static readonly Harmony harmony = new Harmony("VEX");
    }
}
