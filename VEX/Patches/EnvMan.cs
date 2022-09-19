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
    [HarmonyPatch(typeof(EnvMan), "Awake")]
    public static class EnvMan_Awake_Patch
    {
        private static void Postfix(ref EnvMan __instance)
        {
            var env1 = __instance.m_environments.FirstOrDefault(x => x.m_name == "GoblinKing");
            var env2 = __instance.m_environments.FirstOrDefault(x => x.m_name == "ThunderStorm");
            var env3 = __instance.m_environments.FirstOrDefault(x => x.m_name == "nofogts");
            var env4 = __instance.m_environments.FirstOrDefault(x => x.m_name == "Misty");
            var env5 = __instance.m_environments.FirstOrDefault(x => x.m_name == "Clear");
            __instance.m_biomes.FirstOrDefault(x => x.m_biome == Heightmap.Biome.Mistlands).m_environments.Clear();
            __instance.m_biomes.FirstOrDefault(x => x.m_biome == Heightmap.Biome.Mistlands).m_environments.Add(new EnvEntry() { m_env = env1, m_environment = env1.m_name, m_weight = 2f });
            __instance.m_biomes.FirstOrDefault(x => x.m_biome == Heightmap.Biome.Mistlands).m_environments.Add(new EnvEntry() { m_env = env2, m_environment = env2.m_name, m_weight = .2f });
            __instance.m_biomes.FirstOrDefault(x => x.m_biome == Heightmap.Biome.Mistlands).m_environments.Add(new EnvEntry() { m_env = env3, m_environment = env3.m_name, m_weight = .2f });
            __instance.m_biomes.FirstOrDefault(x => x.m_biome == Heightmap.Biome.Mistlands).m_environments.Add(new EnvEntry() { m_env = env4, m_environment = env4.m_name, m_weight = .7f });
            __instance.m_biomes.FirstOrDefault(x => x.m_biome == Heightmap.Biome.Mistlands).m_environments.Add(new EnvEntry() { m_env = env5, m_environment = env5.m_name, m_weight = .4f });
            //TODO: setup the environments....
        }
    }

}