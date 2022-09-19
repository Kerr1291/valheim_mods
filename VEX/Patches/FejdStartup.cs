using HarmonyLib;
using UnityEngine;
using System;
using System.Linq;
using System.Threading;
using System.Collections;
using System.Collections.Generic;
using HarmonyLib.Tools;
using System.Reflection;
using System.Runtime.InteropServices;
using Steamworks;

namespace VEX.Patches
{
    [HarmonyPatch(typeof(FejdStartup), "Awake")]
    public static class FejdStartup_Awake_Patch
    {
        public static bool isDevStartup = false;

        private static void Postfix(ref FejdStartup __instance)
        {
            FejdStartup self = __instance;
            
            string[] commandLineArgs = Environment.GetCommandLineArgs();
            for (int i = 0; i < commandLineArgs.Length; i++)
            {
                if (commandLineArgs[i] == "-dev")
                {
                    isDevStartup = true;
                }
            }
        }
    }

    [HarmonyPatch(typeof(FejdStartup), "UpdateCharacterList")]
    public static class FejdStartup_UpdateCharacterList_Patch
    {
        private static void Postfix(ref FejdStartup __instance)
        {
            FejdStartup self = __instance;

            if(FejdStartup_Awake_Patch.isDevStartup)
            {
                self.OnCharacterStart();
                self.m_world = self.FindWorld("DevHeim");
                if (self.m_world != null)
                {
                    self.OnWorldStart();
                    //Player.m_localPlayer.SetGodMode(true);
                    Console.SetConsoleEnabled(true);
                    Console.instance.m_cheat = true;
                    Player.m_debugMode = true;
                }
            }
        }
    }
}
