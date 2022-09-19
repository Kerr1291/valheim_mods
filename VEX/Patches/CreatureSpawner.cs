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
    [HarmonyPatch(typeof(CreatureSpawner), "UpdateSpawner")]
    public class CreatureSpawner_UpdateSpawner_Patch
    {
        private static bool Prefix(ref CreatureSpawner __instance)
        {
            if (!NPZ.MOD_ENABLED)
                return true;

            CreatureSpawner self = __instance;
            if (!self.m_nview.IsOwner())
            {
                return false;
            }
            ZDOID zdoid = self.m_nview.GetZDO().GetZDOID("spawn_id");
            if (self.m_respawnTimeMinuts <= 0f && !zdoid.IsNone())
            {
                return false;
            }
            if (!zdoid.IsNone() && ZDOMan.instance.GetZDO(zdoid) != null)
            {
                self.m_nview.GetZDO().Set("alive_time", ZNet.instance.GetTime().Ticks);
                return false;
            }
            if (self.m_respawnTimeMinuts > 0f)
            {
                DateTime time = ZNet.instance.GetTime();
                DateTime d = new DateTime(self.m_nview.GetZDO().GetLong("alive_time", 0L));
                if ((time - d).TotalMinutes < (double)self.m_respawnTimeMinuts)
                {
                    return false;
                }
            }
            if (!self.m_spawnAtDay && EnvMan.instance.IsDay())
            {
                return false;
            }
            if (!self.m_spawnAtNight && EnvMan.instance.IsNight())
            {
                return false;
            }
            bool requireSpawnArea = self.m_requireSpawnArea;
            if (!self.m_spawnInPlayerBase && EffectArea.IsPointInsideArea(self.transform.position, EffectArea.Type.PlayerBase, 0f))
            {
                return false;
            }
            if (self.m_triggerNoise > 0f)
            {
                if (!Player.IsPlayerInRange(self.transform.position, self.m_triggerDistance, self.m_triggerNoise))
                {
                    return !NPC.IsNPCInRange(self.transform.position, self.m_triggerDistance, self.m_triggerNoise);
                }
            }
            else if (!Player.IsPlayerInRange(self.transform.position, self.m_triggerDistance))
            {
                return !NPC.IsNPCInRange(self.transform.position, self.m_triggerDistance);
            }
            self.Spawn();

            return false;
        }
    }
}
