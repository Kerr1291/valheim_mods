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
    [HarmonyPatch(typeof(LodFadeInOut), "Awake")]
    public static class LodFadeInOut_Awake_Patch
    {
        private static bool Prefix(ref LodFadeInOut __instance)
        {
            if (!ZNet.instance.IsServer())
                return true;

            if (!NPZ.MOD_ENABLED)
                return true;

            LodFadeInOut self = __instance;

            if (!NPC.IsNPCInRange(self.transform.position, LodFadeInOut.m_minTriggerDistance))
                return true;

            return false;
        }
    }
}
