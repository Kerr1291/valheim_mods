using HarmonyLib;
using UnityEngine;
using System.Linq;
using System.Collections.Generic;
using HarmonyLib.Tools;

namespace VEX.Patches
{
    [HarmonyPatch(typeof(Fish), "FixedUpdate")]
    public static class Fish_FixedUpdate_Patch
    {
        private static void Prefix(ref Fish __instance)
        {
            Fish self = __instance;

            if (self.GetComponent<Floating>() != null)
                self.m_inWater = -10000f;
        }
    }

    [HarmonyPatch(typeof(Fish), "IsOutOfWater")]
    public static class Fish_IsOutOfWater_Patch
    {
        private static void Postfix(ref Fish __instance, ref bool __result)
        {
            Fish self = __instance;

            if (self.GetComponent<Floating>() != null)
            {
                __result = true;
            }
        }
    }
}


