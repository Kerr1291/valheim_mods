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
    [HarmonyPatch(typeof(ZNetScene), "OutsideActiveArea", typeof(Vector3))]
    public class ZNetScene_OutsideActiveArea_Patch
    {
        //IEnumerable<MethodBase> TargetMethods()
        //{
        //    return AccessTools.GetTypesFromAssembly(Assembly.GetAssembly(typeof(ZNetScene)))
        //        .SelectMany(type => typeof(ZNetScene).GetMethods())
        //        .Where(method => method.ReturnType != typeof(void) && method.Name.StartsWith("OutsideActiveArea") && method.GetParameters().Length == 1)
        //        .Cast<MethodBase>();
        //}

        private static void Postfix(ref ZNetScene __instance, Vector3 point, ref bool __result)
        {
            if (!NPZ.MOD_ENABLED)
                return;

            if(!__result)
            {
                __result = NPC.IsNPCOutsideActiveArea(point);
            }

            return;
        }
    }

    [HarmonyPatch(typeof(ZNetScene), "InActiveArea", typeof(Vector2i), typeof(Vector3))]
    public static class ZNetScene_InActiveArea_Patch
    {
        private static void Postfix(ref ZNetScene __instance, Vector2i zone, Vector3 refPoint, ref bool __result)
        {
            if (!NPZ.MOD_ENABLED)
                return;

            if (!__result)
            {
                __result = NPC.IsNPCInActiveArea(zone);
            }

            return;
        }
    }

    [HarmonyPatch(typeof(ZNetScene), "CreateObjects")]
    public static class ZNetScene_CreateObjects_Patch
    {
        public static int default_maxCreatedPerFrame = 10; 
        public static int maxCreatedPerFrameIdle = 100; 
        public static int maxCreatedPerFrameLoading = 1000;

        private static bool Prefix(ref ZNetScene __instance, List<ZDO> currentNearObjects, List<ZDO> currentDistantObjects)
        {
            if (!NPZ.MOD_ENABLED)
                return true;

            int maxCreatedPerFrame = maxCreatedPerFrameIdle;
            if (__instance.InLoadingScreen())
            {
                maxCreatedPerFrame = maxCreatedPerFrameLoading;
            }
            int frameCount = Time.frameCount;
            foreach (ZDO key in __instance.m_instances.Keys)
            {
                key.m_tempCreateEarmark = frameCount;
            }
            int num = 0;
            __instance.CreateObjectsSorted(currentNearObjects, maxCreatedPerFrame, ref num);
            __instance.CreateDistantObjects(currentDistantObjects, maxCreatedPerFrame, ref num);

            return false;
        }
    }

    [HarmonyPatch(typeof(ZNetScene), "CreateDestroyObjects")]
    public static class ZNetScene_CreateDestroyObjects_Patch
    {
        public static int ActiveArea {
            get {
                return ZoneSystem.instance.m_activeArea;
            }
            set {
                ZoneSystem.instance.m_activeArea = value;
            }
        }

        private static bool Prefix(ref ZNetScene __instance)
        {
            ZNetScene self = __instance;

            if (NPZ.MOD_ENABLED)
            {
                self.m_tempCurrentObjects.Clear();
                self.m_tempCurrentDistantObjects.Clear();

                Vector2i zone = ZoneSystem.instance.GetZone(ZNet.instance.GetReferencePosition());
                ZDOMan.instance.FindSectorObjects(zone, ZoneSystem.instance.m_activeArea, ZoneSystem.instance.m_activeDistantArea, self.m_tempCurrentObjects, self.m_tempCurrentDistantObjects);

                var zonesToCheck = NPZ.zones;
                foreach (var xzone in zonesToCheck)
                {
                    foreach (var yzone in xzone.Value)
                    {
                        ZDOMan.instance.FindSectorObjects(new Vector2i(xzone.Key, yzone.Key), yzone.Value, 0, self.m_tempCurrentObjects, null);
                    }
                }

                self.CreateObjects(self.m_tempCurrentObjects, self.m_tempCurrentDistantObjects);
                self.RemoveObjects(self.m_tempCurrentObjects, self.m_tempCurrentDistantObjects);
                return false;
            }

            return true;
        }
    }
}
