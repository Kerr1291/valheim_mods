using HarmonyLib;
using UnityEngine;
using System.Linq;
using System.Collections.Generic;
using HarmonyLib.Tools;

namespace VEX.Patches
{

    //undo: we want this behaviour so the ship doesn't accidentally explode
    //[HarmonyPatch(typeof(Piece), "CanBeRemoved")]
    //public static class Piece_CanBeRemoved_Patch
    //{
    //    private static bool Prefix(ref Piece __instance, ref bool __result)
    //    {
    //        Piece self = __instance;

    //        Container componentInChildren = self.GetComponentInChildren<Container>();
    //        if (componentInChildren != null)
    //        {
    //            __result = componentInChildren.CanBeRemoved();
    //            return false;
    //        }

    //        __result = true;
    //        //Ship componentInChildren2 = self.GetComponentInChildren<Ship>();
    //        //__result = !(componentInChildren2 != null) || componentInChildren2.CanBeRemoved();
    //        return false;
    //    }
    //}
}
