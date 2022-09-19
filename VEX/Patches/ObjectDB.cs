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
    [HarmonyPatch(typeof(ObjectDB), "CopyOtherDB")]
    public static class ObjectDB_Patch
    {
        public static void Postfix(ref ObjectDB __instance, ObjectDB other)
        {
            var items = __instance.m_items;

            var sledge = items.Where(x => x.GetComponent<ItemDrop>() != null).Select(x => x.GetComponent<ItemDrop>()).FirstOrDefault(x => x.m_itemData.m_shared.m_name == "SledgeIron");
            if(sledge != null)
            {
                sledge.m_itemData.m_shared.m_toolTier = 0;
                sledge.m_itemData.m_shared.m_damages.m_pickaxe = 200f;
                sledge.m_itemData.m_shared.m_damages.m_chop = 40f;
                sledge.m_itemData.m_shared.m_damages.m_lightning = 20f;
                sledge.m_itemData.m_shared.m_damagesPerLevel.m_pickaxe = 50f;
                sledge.m_itemData.m_shared.m_damagesPerLevel.m_chop = 10f;
                sledge.m_itemData.m_shared.m_damagesPerLevel.m_lightning = 5f;
                sledge.m_itemData.m_shared.m_durabilityPerLevel = 200f;
                sledge.m_itemData.m_shared.m_attack.m_hitTerrain = true;
                sledge.m_itemData.m_shared.m_attack.m_forceMultiplier *= 5f;
            }
        }
    }
}
