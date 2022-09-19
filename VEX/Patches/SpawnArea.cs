//using HarmonyLib;
//using UnityEngine;
//using System;
//using System.Linq;
//using System.Threading;
//using System.Collections;
//using System.Collections.Generic;
//using HarmonyLib.Tools;
//using System.Reflection;
//using System.Runtime.InteropServices;
//using Steamworks;

//namespace VEX.Patches
//{
//    [HarmonyPatch(typeof(SpawnArea), "UpdateSpawn")]
//    public static class SpawnArea_UpdateSpawn_Patch
//    {
//        private static bool Prefix(ref SpawnArea __instance)
//        {
//            if (!NPZ.MOD_ENABLED)
//                return true;

//            SpawnArea self = __instance;

//            if (!self.m_nview.IsOwner())
//            {
//                return false;
//            }
//            if (ZNetScene.instance.OutsideActiveArea(self.transform.position))
//            {
//                if (!NPC.IsNPCInRange(self.transform.position, self.m_triggerDistance))
//                    return false;
//            }
//            if (!Player.IsPlayerInRange(self.transform.position, self.m_triggerDistance))
//            {
//                return false;
//            }
//            self.m_spawnTimer += 2f;
//            if (self.m_spawnTimer > self.m_spawnIntervalSec)
//            {
//                self.m_spawnTimer = 0f;
//                self.SpawnOne();
//            }

//            return false;
//        }
//    }
//}
