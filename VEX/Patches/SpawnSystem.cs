using HarmonyLib;
using UnityEngine;
using System.Linq;
using System.Collections.Generic;
using HarmonyLib.Tools;

namespace VEX.Patches
{
    public static class SpawnSystem_SharedSpawnCount
    {
        public static Dictionary<SpawnSystem, Dictionary<int, int>> spawnCounter = new Dictionary<SpawnSystem, Dictionary<int, int>>();

        public static void UpdateSpawnCounters(SpawnSystem self, List<SpawnSystem.SpawnData> spawners, System.DateTime currentTime, bool isEvent)
        {
            string str = isEvent ? "e_" : "b_";
            int num = 0;
            foreach (SpawnSystem.SpawnData spawnData in spawners)
            {
                num++;
                if (spawnData.m_enabled && self.m_heightmap.HaveBiome(spawnData.m_biome))
                {
                    int stableHashCode = (str + spawnData.m_prefab.name + num.ToString()).GetStableHashCode();
                    System.DateTime d = new System.DateTime(self.m_nview.GetZDO().GetLong(stableHashCode, 0L));
                    System.TimeSpan timeSpan = currentTime - d;
                    int counter = Mathf.Min(spawnData.m_maxSpawned, (int)(timeSpan.TotalSeconds / (double)spawnData.m_spawnInterval));

                    if (spawnCounter[self].ContainsKey(stableHashCode))
                        spawnCounter[self][stableHashCode] = counter;
                    else
                        spawnCounter[self].Add(stableHashCode, counter);

                    if (counter > 0)
                    {
                        self.m_nview.GetZDO().Set(stableHashCode, currentTime.Ticks);
                    }
                }
            }
        }
    }

    [HarmonyPatch(typeof(SpawnSystem), "Awake")]
    public static class SpawnSystem_Awake_Patch
    {
        private static void Postfix(ref SpawnSystem __instance)
        {
            SpawnSystem_SharedSpawnCount.spawnCounter[__instance] = new Dictionary<int, int>();
        }
    }

    [HarmonyPatch(typeof(SpawnSystem), "OnDestroy")]
    public static class SpawnSystem_OnDestroy_Patch
    {
        private static void Postfix(ref SpawnSystem __instance)
        {
            SpawnSystem_SharedSpawnCount.spawnCounter.Remove(__instance);
        }
    }

    [HarmonyPatch(typeof(SpawnSystem), "FindBaseSpawnPoint")]
    public static class SpawnSystem_FindBaseSpawnPoint_Patch
    {
        private static bool Prefix(ref SpawnSystem __instance, SpawnSystem.SpawnData spawn, List<Player> allPlayers, out Vector3 spawnCenter, out Player targetPlayer, ref bool __result)
        {
            spawnCenter = Vector3.zero;
            targetPlayer = null;

            if (!NPZ.MOD_ENABLED)
                return true;

            SpawnSystem self = __instance;
            float spawnRange = ZoneSystem.instance.m_zoneSize * 0.5f;
            Vector3 spawnRangeCenter = self.transform.position;

            float max = spawnRange;// (spawn.m_spawnRadiusMax > 0f) ? spawn.m_spawnRadiusMax : 80f;
            for (int i = 0; i < 20; i++)
            {
                Vector3 a = Quaternion.Euler(0f, (float)UnityEngine.Random.Range(0, 360), 0f) * Vector3.forward;
                Vector3 vector = spawnRangeCenter + a * UnityEngine.Random.Range(0f, max);
                if (self.IsSpawnPointGood(spawn, ref vector))
                {
                    spawnCenter = vector;
                    __result = true;
                    return false;
                }
            }

            spawnCenter = Vector3.zero;
            __result = false;
            return false;
        }
    }

    [HarmonyPatch(typeof(SpawnSystem), "UpdateSpawnList")]
    public static class SpawnSystem_UpdateSpawnList_Patch
    {
        private static bool Prefix(ref SpawnSystem __instance, List<SpawnSystem.SpawnData> spawners, System.DateTime currentTime, bool eventSpawners)
        {
            if (!NPZ.MOD_ENABLED)
                return true;

            SpawnSystem self = __instance;

            float spawnRange = ZoneSystem.instance.m_zoneSize * 0.5f;
            float spawnArea = ZoneSystem.instance.m_activeArea * spawnRange + spawnRange;
            Vector3 spawnRangeCenter = self.transform.position;

            List<Player> playersInRange = new List<Player>();
            int pcount = Player.GetPlayersInRangeXZ(spawnRangeCenter, spawnArea);
            
            if (pcount <= 0)
            {
                return false;
            }

            string str = eventSpawners ? "e_" : "b_";
            int num = 0;
            foreach (SpawnSystem.SpawnData spawnData in spawners)
            {
                num++;
                if (spawnData.m_enabled && self.m_heightmap.HaveBiome(spawnData.m_biome))
                {
                    int stableHashCode = (str + spawnData.m_prefab.name + num.ToString()).GetStableHashCode();
                    int num2 = SpawnSystem_SharedSpawnCount.spawnCounter[self][stableHashCode];

                    for (int i = 0; i < num2; i++)
                    {
                        if (UnityEngine.Random.Range(0f, 100f) <= spawnData.m_spawnChance)
                        {
                            float spawnCapRange = spawnArea;// ZoneSystem.instance.m_zoneSize * 0.5f;

                            //patch: changes the # of instances cap count to use the location of this spawn system and the size of the zone above
                            if ((!string.IsNullOrEmpty(spawnData.m_requiredGlobalKey) && !ZoneSystem.instance.GetGlobalKey(spawnData.m_requiredGlobalKey)) || (spawnData.m_requiredEnvironments.Count > 0 && !EnvMan.instance.IsEnvironment(spawnData.m_requiredEnvironments)) || (!spawnData.m_spawnAtDay && EnvMan.instance.IsDay()) || (!spawnData.m_spawnAtNight && EnvMan.instance.IsNight()) || SpawnSystem.GetNrOfInstances(spawnData.m_prefab, self.transform.position, spawnCapRange, eventSpawners, false) >= spawnData.m_maxSpawned)
                            {
                                break;
                            }

                            Vector3 spawnPoint;

                            //no longer care about players, out for player will always return null
                            bool canSpawn = self.FindBaseSpawnPoint(spawnData, null, out spawnPoint, out _);
                            bool hasSpace = (spawnData.m_spawnDistance <= 0f || !SpawnSystem.HaveInstanceInRange(spawnData.m_prefab, spawnPoint, spawnData.m_spawnDistance * 2f));

                            if (canSpawn && hasSpace)
                            {
                                int num3 = UnityEngine.Random.Range(spawnData.m_groupSizeMin, spawnData.m_groupSizeMax + 1);
                                float d2 = (num3 > 1) ? spawnData.m_groupRadius : 0f;
                                int num4 = 0;
                                for (int j = 0; j < num3 * 2; j++)
                                {
                                    Vector2 insideUnitCircle = UnityEngine.Random.insideUnitCircle;
                                    Vector3 a = spawnPoint + new Vector3(insideUnitCircle.x, 0f, insideUnitCircle.y) * d2;
                                    if (self.IsSpawnPointGood(spawnData, ref a))
                                    {
                                        self.Spawn(spawnData, a + Vector3.up * spawnData.m_groundOffset, eventSpawners);
                                        num4++;
                                        if (num4 >= num3)
                                        {
                                            break;
                                        }
                                    }
                                }
                                ZLog.Log(string.Concat(new object[]
                                {
                                "Spawned ",
                                spawnData.m_prefab.name,
                                " x ",
                                num4
                                }));
                            }
                        }
                    }
                }
            }

            return false;
        }
    }

    //TODO: delete this patch after debugging
    [HarmonyPatch(typeof(SpawnSystem), "IsSpawnPointGood")]
    public static class SpawnSystem_IsSpawnPointGood_Patch
    {
        public static bool DEBUG = false;
        private static bool Prefix(ref SpawnSystem __instance, SpawnSystem.SpawnData spawn, ref Vector3 spawnPoint, ref bool __result)
        {
            if (!DEBUG)
                return true;

            Vector3 vector;
            Heightmap.Biome biome;
            Heightmap.BiomeArea biomeArea;
            Heightmap heightmap;
            ZoneSystem.instance.GetGroundData(ref spawnPoint, out vector, out biome, out biomeArea, out heightmap);
            if ((spawn.m_biome & biome) == Heightmap.Biome.None)
            {
                Debug.Log("SPAWN POINT BAD: WRONG BIOME");
                return false;
            }
            if ((spawn.m_biomeArea & biomeArea) == (Heightmap.BiomeArea)0)
            {
                Debug.Log("SPAWN POINT BAD: NO BIOME AREA");
                return false;
            }
            if (ZoneSystem.instance.IsBlocked(spawnPoint))
            {
                Debug.Log("SPAWN POINT BAD: POINT BLOCKED");
                return false;
            }
            float num = spawnPoint.y - ZoneSystem.instance.m_waterLevel;
            if (num < spawn.m_minAltitude || num > spawn.m_maxAltitude)
            {
                Debug.Log("SPAWN POINT BAD: WRONG ALTITUDE");
                return false;
            }
            float num2 = Mathf.Cos(0.0174532924f * spawn.m_maxTilt);
            float num3 = Mathf.Cos(0.0174532924f * spawn.m_minTilt);
            if (vector.y < num2 || vector.y > num3)
            {
                Debug.Log("SPAWN POINT BAD: WRONG TILT");
                return false;
            }
            float range = (spawn.m_spawnRadiusMin > 0f) ? spawn.m_spawnRadiusMin : 40f;
            if (Player.IsPlayerInRange(spawnPoint, range))
            {
                Debug.Log("SPAWN POINT BAD: PLAYER TOO CLOSE");
                return false;
            }
            if (EffectArea.IsPointInsideArea(spawnPoint, EffectArea.Type.PlayerBase, 0f))
            {
                Debug.Log("SPAWN POINT BAD: INSIDE PLAYER BASE");
                return false;
            }
            if (!spawn.m_inForest || !spawn.m_outsideForest)
            {
                bool flag = WorldGenerator.InForest(spawnPoint);
                if (!spawn.m_inForest && flag)
                {
                    Debug.Log("SPAWN POINT BAD: SHOULDNT BE IN FOREST");
                    return false;
                }
                if (!spawn.m_outsideForest && !flag)
                {
                    Debug.Log("SPAWN POINT BAD: SHOULD BE IN FOREST");
                    return false;
                }
            }
            if (spawn.m_minOceanDepth != spawn.m_maxOceanDepth && heightmap != null)
            {
                float oceanDepth = heightmap.GetOceanDepth(spawnPoint);
                if (oceanDepth < spawn.m_minOceanDepth || oceanDepth > spawn.m_maxOceanDepth)
                {
                    Debug.Log("SPAWN POINT BAD: WRONG OCEAN DEPTH");
                    return false;
                }
            }
            return true;
        }
    }

    [HarmonyPatch(typeof(SpawnSystem), "UpdateSpawning")]
    public static class SpawnSystem_UpdateSpawning_Patch
    {
        private static bool Prefix(ref SpawnSystem __instance)
        {
            if (!NPZ.MOD_ENABLED)
                return true;

            SpawnSystem self = __instance;

            if (!self.m_nview.IsValid())
            {
                return false;
            }

            bool isOwner = self.m_nview.IsOwner();
            bool hasOwner = self.m_nview.HasOwner();

            if (!ZNet.instance.IsServer())
            {
                if (!isOwner)
                    return false;

                if (Player.m_localPlayer == null)
                    return false;
            }
            else
            {
                if (!isOwner && hasOwner)
                {
                    self.m_nview.ClaimOwnership();
                }

                if (!hasOwner)
                    return false;
            }

            System.DateTime time = ZNet.instance.GetTime();
            self.m_nearPlayers.Clear();

            List<SpawnSystem.SpawnData> currentSpawners = RandEventSystem.instance.GetCurrentSpawners();
            if (currentSpawners != null)
            {
                SpawnSystem_SharedSpawnCount.UpdateSpawnCounters(self, currentSpawners, time, true);
                self.UpdateSpawnList(currentSpawners, time, true);
            }

            SpawnSystem_SharedSpawnCount.UpdateSpawnCounters(self, self.m_spawners, time, false);

            self.UpdateSpawnList(self.m_spawners, time, false);

            if (ZNet.instance.IsServer())
            {
                UpdateSpawnList(self, self.m_spawners, time);
            }

            return false;
        }

        public static int FindBaseSpawnPoint_spawnAttempts = 5;//default was 20
        private static bool FindBaseSpawnPoint(SpawnSystem self, List<NPC> npcsInZone, SpawnSystem.SpawnData spawn, out Vector3 spawnCenter)
        {
            spawnCenter = Vector3.zero;
            float spawnRange = ZoneSystem.instance.m_zoneSize * 0.5f;
            Vector3 spawnRangeCenter = self.transform.position;
            
            float max = spawnRange;// (spawn.m_spawnRadiusMax > 0f) ? spawn.m_spawnRadiusMax : 80f;
            NPC npc = npcsInZone[UnityEngine.Random.Range(0, npcsInZone.Count)];
            for (int i = 0; i < FindBaseSpawnPoint_spawnAttempts; i++)
            {
                Vector3 a = Quaternion.Euler(0f, (float)UnityEngine.Random.Range(0, 360), 0f) * Vector3.forward;
                Vector3 vector = spawnRangeCenter + a * UnityEngine.Random.Range(0f, max);
                Vector3 spawnPoint = vector;
                if (IsSpawnPointGood(self, spawn, ref vector, ref spawnPoint, out spawnCenter))
                    return true;
            }

            return false;            
        }

        private static bool IsSpawnPointGood(SpawnSystem self, SpawnSystem.SpawnData spawn, ref Vector3 vector, ref Vector3 spawnPoint, out Vector3 spawnCenter)
        {
            spawnCenter = Vector3.zero;
            ZoneSystem.instance.GetGroundData(ref spawnPoint, out _, out _, out _, out _);
            float range = (spawn.m_spawnRadiusMin > 0f) ? spawn.m_spawnRadiusMin : 20f;
            if (!NPC.IsNPCInRange(spawnPoint, range))
            {
                if (self.IsSpawnPointGood(spawn, ref vector))
                {
                    spawnCenter = vector;
                    return true;
                }
            }
            return false;
        }

        private static void UpdateSpawnList(SpawnSystem self, List<SpawnSystem.SpawnData> spawners, System.DateTime currentTime, bool eventSpawners = false)
        {
            float spawnRange = ZoneSystem.instance.m_zoneSize * 0.5f;
            Vector3 spawnRangeCenter = self.transform.position;
            float spawnArea = ZoneSystem.instance.m_activeArea * spawnRange + spawnRange;            

            List<NPC> npcsInRange = new List<NPC>();
            NPC.GetNPCsInRange(spawnRangeCenter, spawnRange, npcsInRange);

            if (npcsInRange.Count <= 0)
                return;

            //Debug.Log("spawner has "+ npcsInRange.Count + " NPCs in zone "+ ZoneSystem.instance.GetZone(spawnRangeCenter));

            string str = eventSpawners ? "e_" : "b_";
            int num = 0;
            foreach (SpawnSystem.SpawnData spawnData in spawners)
            {
                num++;
                if (spawnData.m_enabled && self.m_heightmap.HaveBiome(spawnData.m_biome))
                {
                    int stableHashCode = (str + spawnData.m_prefab.name + num.ToString()).GetStableHashCode();
                    //System.DateTime d = new System.DateTime(self.m_nview.GetZDO().GetLong(stableHashCode, 0L));
                    //System.TimeSpan timeSpan = currentTime - d;
                    //int num2 = Mathf.Min(spawnData.m_maxSpawned, (int)(timeSpan.TotalSeconds / (double)spawnData.m_spawnInterval));
                    //if (num2 > 0)
                    //{
                    //    self.m_nview.GetZDO().Set(stableHashCode, currentTime.Ticks);
                    //}
                    int num2 = SpawnSystem_SharedSpawnCount.spawnCounter[self][stableHashCode];
                    //if(num2>0)
                    //{
                    //    Debug.Log("want to spawn " + num2 + " things of type" + spawnData.m_prefab.name);
                    //}
                    for (int i = 0; i < num2; i++)
                    {
                        if (UnityEngine.Random.Range(0f, 100f) <= spawnData.m_spawnChance)
                        {
                            bool isMissingGlobalKeys = string.IsNullOrEmpty(spawnData.m_requiredGlobalKey) && ZoneSystem.instance.GetGlobalKey(spawnData.m_requiredGlobalKey);
                            //(spawnData.m_requiredEnvironments.Count > 0 && !EnvMan.instance.IsEnvironment(spawnData.m_requiredEnvironments))
                            bool isBadSpawnTime = (!spawnData.m_spawnAtDay && EnvMan.instance.IsDay()) || (!spawnData.m_spawnAtNight && EnvMan.instance.IsNight());
                            bool isSpawnCapped = SpawnSystem.GetNrOfInstances(spawnData.m_prefab, self.transform.position, spawnRange, eventSpawners, false) >= spawnData.m_maxSpawned;

                            //Debug.Log("isMissingGlobalKeys " + isMissingGlobalKeys);
                            //Debug.Log("isBadSpawnTime "+isBadSpawnTime);
                            //Debug.Log("isSpawnCapped " + isSpawnCapped);
                            if (isMissingGlobalKeys || isBadSpawnTime || isSpawnCapped)
                            {
                                break;
                            }

                            bool canSpawn = FindBaseSpawnPoint(self, npcsInRange, spawnData, out Vector3 spawnPoint);
                            bool hasSpace = (spawnData.m_spawnDistance <= 0f || !SpawnSystem.HaveInstanceInRange(spawnData.m_prefab, spawnPoint, spawnData.m_spawnDistance));


                            if (canSpawn && hasSpace)
                            {
                                Vector3 spawnPoint2 = Vector3.zero;
                                Vector3 result = Vector3.zero;
                                int num3 = UnityEngine.Random.Range(spawnData.m_groupSizeMin, spawnData.m_groupSizeMax + 1);
                                float d2 = (num3 > 1) ? spawnData.m_groupRadius : 0f;
                                int num4 = 0;
                                for (int j = 0; j < num3 * 2; j++)
                                {
                                    Vector2 insideUnitCircle = UnityEngine.Random.insideUnitCircle;
                                    Vector3 a = spawnPoint + new Vector3(insideUnitCircle.x, 0f, insideUnitCircle.y) * d2;
                                    Vector3 offset = Vector3.up * spawnData.m_groundOffset;
                                    if (IsSpawnPointGood(self, spawnData, ref a, ref spawnPoint2, out result))
                                    {
                                        self.Spawn(spawnData, result + offset, eventSpawners);
                                        num4++;
                                        if (num4 >= num3)
                                        {
                                            break;
                                        }
                                    }
                                }

                                ZLog.Log(string.Concat(new object[]
                                {
                                "NPC Spawned ",
                                spawnData.m_prefab.name,
                                " x ",
                                num4
                                }));

                                if (num4 == 0)
                                {
                                    float range = (spawnData.m_spawnRadiusMin > 0f) ? spawnData.m_spawnRadiusMin : 20f;
                                    bool debug_inrange = NPC.IsNPCInRange(spawnPoint, range);
                                    Debug.Log("WAS NPC TOO CLOSE?: " + debug_inrange);
                                    if (!debug_inrange)
                                    {
                                        SpawnSystem_IsSpawnPointGood_Patch.DEBUG = true;
                                        IsSpawnPointGood(self, spawnData, ref result, ref spawnPoint2, out _);
                                        SpawnSystem_IsSpawnPointGood_Patch.DEBUG = false;
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}
