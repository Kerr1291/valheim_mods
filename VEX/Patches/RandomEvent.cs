using HarmonyLib;
using UnityEngine;
using System.Linq;
using System.Collections.Generic;
using HarmonyLib.Tools;

namespace VEX.Patches
{
    public static class RandomEventPatchSettings
    {
        public static bool enableEventsAnywhere = false;
        public static bool ignoreEventKeys = false;
        public static List<Heightmap.Biome> normalBiomes = new List<Heightmap.Biome>() { Heightmap.Biome.DeepNorth, Heightmap.Biome.Mistlands };
    }


    [HarmonyPatch(typeof(RandomEvent), "InEventBiome")]
    public static class RandomEvent_InEventBiome_Patch
    {
        private static bool Prefix(ref RandomEvent __instance, ref bool __result)
        {
            RandomEvent self = __instance;

            if(RandomEventPatchSettings.enableEventsAnywhere)
            {
                if(RandomEventPatchSettings.normalBiomes.Count > 0 && RandomEventPatchSettings.normalBiomes.Contains(EnvMan.instance.GetCurrentBiome()))
                {
                    return true;
                }

                Console.instance.AddString("Random event starting! id = " + self.m_name);
                Debug.Log("Random event starting! " + self.m_name);

                __result = true;
                return false;
            }
            else
            {
                return true;
            }
                //(EnvMan.instance.GetCurrentBiome() & self.m_biome) > Heightmap.Biome.None;
        }
    }

    [HarmonyPatch(typeof(RandEventSystem), "HaveGlobalKeys")]
    public static class RandEventSystem_HaveGlobalKeys_Patch
    {
        private static bool Prefix(ref RandEventSystem __instance, RandomEvent ev, ref bool __result)
        {
            if (RandomEventPatchSettings.ignoreEventKeys)
            {
                if (RandomEventPatchSettings.normalBiomes.Count > 0 && RandomEventPatchSettings.normalBiomes.Contains(EnvMan.instance.GetCurrentBiome()))
                {
                    return true;
                }

                __result = true;
                return false;
            }

            return true;
        }
    }
}
