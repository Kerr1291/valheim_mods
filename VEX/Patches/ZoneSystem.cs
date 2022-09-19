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
    [HarmonyPatch(typeof(ZoneSystem), "Update")]
    public static class ZoneSystem_Update_Patch
    {
        static float updateTimer = 0f;

        private static void Prefix(ref ZoneSystem __instance)
        {
            if (ZNet.GetConnectionStatus() != ZNet.ConnectionStatus.Connected)
                return;

            if (!ZNet.instance.IsServer())
                return;

            if (!NPZ.MOD_ENABLED)
                return;

            ZoneSystem self = __instance;

            updateTimer += Time.deltaTime;

            if (updateTimer > 0.1f)
            {
                updateTimer = 0f;
                foreach (NPC npc in NPC.s_npcs)
                {
                    bool zoneCreated = CreateLocalZones(self, npc);
                    if (ZNet.instance.IsServer() && !zoneCreated)
                    {
                        CreateGhostZones(self, npc);
                    }
                }
            }
        }

        private static bool CreateGhostZones(ZoneSystem self, NPC npc)
        {
            Vector2i zone = npc.npz.sector;
            GameObject gameObject;
            if (!self.IsZoneGenerated(zone) && self.SpawnZone(zone, ZoneSystem.SpawnMode.Ghost, out gameObject))
            {
                return true;
            }
            int num = npc.npz.m_activeAreaRadius;// + this.m_activeDistantArea;
            for (int i = zone.y - num; i <= zone.y + num; i++)
            {
                for (int j = zone.x - num; j <= zone.x + num; j++)
                {
                    Vector2i zoneID = new Vector2i(j, i);
                    GameObject gameObject2;
                    if (!self.IsZoneGenerated(zoneID) && self.SpawnZone(zoneID, ZoneSystem.SpawnMode.Ghost, out gameObject2))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        private static bool CreateLocalZones(ZoneSystem self, NPC npc)
        {
            Vector2i zone = npc.npz.sector;
            if (self.PokeLocalZone(zone))
            {
                return true;
            }
            int activeArea = npc.npz.m_activeAreaRadius;//self.m_activeArea //TODO: use the value from the NPZ system
            for (int i = zone.y - activeArea; i <= zone.y + activeArea; i++)
            {
                for (int j = zone.x - activeArea; j <= zone.x + activeArea; j++)
                {
                    Vector2i vector2i = new Vector2i(j, i);
                    if (!(vector2i == zone) && self.PokeLocalZone(vector2i))
                    {
                        return true;
                    }
                }
            }
            return false;
        }
    }
}
