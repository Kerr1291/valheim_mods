using HarmonyLib;
using UnityEngine;
using System.Linq;
using System.Collections.Generic;
using HarmonyLib.Tools;
using System.Reflection;

namespace VEX.Patches
{
    [HarmonyPatch(typeof(WorldGenerator), "GetBiome", typeof(float), typeof(float))]
    public class WorldGenerator_GetBiome_Patch
    {
        //allows mountain biomes in ashlands
        private static void Postfix(ref WorldGenerator __instance, ref Heightmap.Biome __result, float wx, float wy)
        {
            WorldGenerator self = __instance;

            if (self.m_world.m_menu)
                return;
            float magnitude = new Vector2(wx, wy).magnitude;
            float baseHeight = self.GetBaseHeight(wx, wy, false);
            float num = self.WorldAngle(wx, wy) * 100f;

            if (new Vector2(wx, wy + -4000f).magnitude > 12000f + num)
            {
                if (baseHeight > 0.4f)
                {
                    __result = Heightmap.Biome.Mountain;
                }
                __result = Heightmap.Biome.AshLands;
            }
        }
    }
}
