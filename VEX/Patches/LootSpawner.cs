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
    [HarmonyPatch(typeof(LootSpawner), "UpdateSpawner")]
    public class LootSpawner_UpdateSpawner_Patch
    {
        private static bool Prefix(ref LootSpawner __instance)
        {
            if (!NPZ.MOD_ENABLED)
                return true;

            LootSpawner self = __instance;

            if (!self.m_nview.IsOwner())
            {
                return false;
            }
            if (!self.m_spawnAtDay && EnvMan.instance.IsDay())
            {
                return false;
            }
            if (!self.m_spawnAtNight && EnvMan.instance.IsNight())
            {
                return false;
            }
            if (self.m_spawnWhenEnemiesCleared)
            {
                bool flag = LootSpawner.IsMonsterInRange(self.transform.position, self.m_enemiesCheckRange);
                if (flag && !self.m_seenEnemies)
                {
                    self.m_seenEnemies = true;
                }
                if (flag || !self.m_seenEnemies)
                {
                    return false;
                }
            }
            long @long = self.m_nview.GetZDO().GetLong("spawn_time", 0L);
            DateTime time = ZNet.instance.GetTime();
            DateTime d = new DateTime(@long);
            TimeSpan timeSpan = time - d;
            if (self.m_respawnTimeMinuts <= 0f && @long != 0L)
            {
                return false;
            }
            if (timeSpan.TotalMinutes < (double)self.m_respawnTimeMinuts)
            {
                return false;
            }
            if (!Player.IsPlayerInRange(self.transform.position, 20f))
            {
                return !NPC.IsFriendlyNPCInRange(self.transform.position, 20f);
            }
            List<GameObject> dropList = self.m_items.GetDropList();
            for (int i = 0; i < dropList.Count; i++)
            {
                Vector2 vector = UnityEngine.Random.insideUnitCircle * 0.3f;
                Vector3 position = self.transform.position + new Vector3(vector.x, 0.3f * (float)i, vector.y);
                Quaternion rotation = Quaternion.Euler(0f, (float)UnityEngine.Random.Range(0, 360), 0f);
                UnityEngine.Object.Instantiate<GameObject>(dropList[i], position, rotation);
            }
            self.m_spawnEffect.Create(self.transform.position, Quaternion.identity, null, 1f);
            self.m_nview.GetZDO().Set("spawn_time", ZNet.instance.GetTime().Ticks);
            self.m_seenEnemies = false;

            return false;
        }
    }
}
