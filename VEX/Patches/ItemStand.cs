using HarmonyLib;
using UnityEngine;
using System.Linq;
using System.Collections.Generic;
using HarmonyLib.Tools;
using System.Reflection;

namespace VEX.Patches
{
    [HarmonyPatch(typeof(ItemStand), "CanAttach")]
    public class ItemStand_CanAttach_Patch
    {
        private static bool Prefix(ref ItemStand __instance, ItemDrop.ItemData item, ref bool __result)
        {
            ItemStand self = __instance;

            //first try this to see if more things can be attached
            __result = !(self.GetAttachPrefab(item.m_dropPrefab) == null);// && !self.IsUnsupported(item) && self.IsSupported(item) && self.m_supportedTypes.Contains(item.m_shared.m_itemType);

            return false;
        }
    }
}
