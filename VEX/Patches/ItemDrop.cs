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
    //items stay forever....
    [HarmonyPatch(typeof(ItemDrop), "TimedDestruction")]
    public class ItemDrop_TimedDestruction_Patch
    {
        private static bool Prefix(ref ItemDrop __instance)
        {
            return false;
        }
    }
}
