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
    [HarmonyPatch(typeof(Heightmap), "Initialize")]
    internal static class DebugTexture
    {
        public static void Postfix(ref Heightmap __instance)
        {
            if (Commands.vexDebug)
            {
                Commands.m_materialInstance(__instance).SetTexture("_DiffuseTex0", Texture2D.whiteTexture);
            }
        }
    }

    [HarmonyPatch(typeof(Heightmap), "Awake")]
    public static class Heightmap_Awake_Patch
    {
        private static bool Prefix(ref Heightmap __instance)
        {
            if (!NPZ.MOD_ENABLED)
                return true;

            if (!ZNet.instance.IsServer())
                return true;

            Heightmap self = __instance;

            if (!self.m_isDistantLod)
                return true;

            if (!NPC.IsNPCInRange(self.transform.position, 40f))
                return true;

            LODGroup lods = self.GetComponentInParent<LODGroup>();
            if (lods != null)
            {
                lods.localReferencePoint = self.transform.position;
            }

            return true;
        }
    }
}
