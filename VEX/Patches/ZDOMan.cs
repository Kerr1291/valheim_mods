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

    [HarmonyPatch(typeof(ZDOMan), "ReleaseNearbyZDOS")]
    public static class ZDOMan_ReleaseNearbyZDOS_Patch
    {
        //private static bool IsInPeerActiveArea(ZDOMan self, Vector2i sector, long uid)
        //{
        //    if (uid == self.m_myid)
        //    {
        //        bool inArea = ZNetScene.instance.InActiveArea(sector, ZNet.instance.GetReferencePosition());
        //        if(!inArea)
        //        {
        //            return NPC.IsNPCInActiveArea(sector);
        //        }
        //    }
        //    ZNetPeer peer = ZNet.instance.GetPeer(uid);
        //    return peer != null && ZNetScene.instance.InActiveArea(sector, peer.GetRefPos());
        //}

        private static bool Prefix(ref ZDOMan __instance, Vector3 refPosition, long uid)
        {
            if (NPZ.MOD_ENABLED)
            {
                ZDOMan self = __instance;

                Vector2i zone = ZoneSystem.instance.GetZone(refPosition);
                self.m_tempNearObjects.Clear();
                self.FindSectorObjects(zone, ZoneSystem.instance.m_activeArea, 0, self.m_tempNearObjects, null);
                var zonesToCheck = NPZ.zones;
                foreach (var xzone in zonesToCheck)
                {
                    foreach (var yzone in xzone.Value)
                    {
                        ZDOMan.instance.FindSectorObjects(new Vector2i(xzone.Key, yzone.Key), yzone.Value, 0, self.m_tempNearObjects, null);
                    }
                }

                foreach (ZDO zdo in self.m_tempNearObjects)
                {
                    if (zdo.m_persistent)
                    {
                        if (zdo.m_owner == uid)
                        {
                            if (!ZNetScene.instance.InActiveArea(zdo.GetSector(), zone))
                            {
                                bool shouldClearOwner = true;
                                foreach (var xzone in zonesToCheck)
                                {
                                    foreach (var yzone in xzone.Value)
                                    {
                                        int oldArea = ZoneSystem.instance.m_activeArea;
                                        ZoneSystem.instance.m_activeArea = yzone.Value;
                                        bool inActiveArea = ZNetScene.instance.InActiveArea(zdo.GetSector(), new Vector2i(xzone.Key, yzone.Key));
                                        ZoneSystem.instance.m_activeArea = oldArea;

                                        if (inActiveArea)
                                        {
                                            shouldClearOwner = false;
                                            break;
                                        }
                                    }

                                    if (!shouldClearOwner)
                                        break;
                                }

                                if (shouldClearOwner)
                                    zdo.SetOwner(0L);
                            }
                        }
                        else if ((zdo.m_owner == 0L || !self.IsInPeerActiveArea(zdo.GetSector(), zdo.m_owner)) && ZNetScene.instance.InActiveArea(zdo.GetSector(), zone))
                        {
                            zdo.SetOwner(uid);
                        }
                        else if (zdo.m_owner == 0L)
                        {
                            bool exitLoops = false;
                            foreach (var xzone in zonesToCheck)
                            {
                                foreach (var yzone in xzone.Value)
                                {
                                    int oldArea = ZoneSystem.instance.m_activeArea;
                                    ZoneSystem.instance.m_activeArea = yzone.Value;
                                    bool inActiveArea = ZNetScene.instance.InActiveArea(zdo.GetSector(), new Vector2i(xzone.Key, yzone.Key));
                                    ZoneSystem.instance.m_activeArea = oldArea;

                                    if (inActiveArea)
                                    {
                                        exitLoops = true;
                                        zdo.SetOwner(uid);
                                        break;
                                    }
                                }

                                if (exitLoops)
                                    break;
                            }
                        }
                    }
                }

                return false;
            }

            return true;
        }
    }

    [HarmonyPatch(typeof(ZDOMan), "CreateSyncList")]
    public static class ZDOMan_CreateSyncList_Patch
    {
        private static bool Prefix(ref ZDOMan __instance, ZDOMan.ZDOPeer peer, List<ZDO> toSync)
        {
            if (NPZ.MOD_ENABLED)
            {
                ZDOMan self = __instance;

                if (ZNet.instance.IsServer())
                {
                    Vector3 refPos = peer.m_peer.GetRefPos();
                    Vector2i zone = ZoneSystem.instance.GetZone(refPos);
                    self.m_tempToSyncDistant.Clear();
                    self.FindSectorObjects(zone, ZoneSystem.instance.m_activeArea, ZoneSystem.instance.m_activeDistantArea, toSync, self.m_tempToSyncDistant);

                    var zonesToCheck = NPZ.zones;
                    foreach (var xzone in zonesToCheck)
                    {
                        foreach (var yzone in zonesToCheck[xzone.Key])
                        {
                            ZDOMan.instance.FindSectorObjects(new Vector2i(xzone.Key, yzone.Key), yzone.Value, 0, toSync, null);
                        }
                    }

                    self.ServerSortSendZDOS(toSync, refPos, peer);
                    toSync.AddRange(self.m_tempToSyncDistant);
                    self.AddForceSendZdos(peer, toSync);
                    return false;
                }

                self.m_tempRemoveList.Clear();
                foreach (ZDOID zdoid in self.m_clientChangeQueue)
                {
                    ZDO zdo = self.GetZDO(zdoid);
                    if (zdo != null)
                    {
                        toSync.Add(zdo);
                    }
                    else
                    {
                        self.m_tempRemoveList.Add(zdoid);
                    }
                }
                foreach (ZDOID item in self.m_tempRemoveList)
                {
                    self.m_clientChangeQueue.Remove(item);
                }
                self.ClientSortSendZDOS(toSync, peer);
                self.AddForceSendZdos(peer, toSync);

                return false;
            }

            return true;
        }
    }
}
