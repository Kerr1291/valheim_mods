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
    //[HarmonyPatch(typeof(ZSteamSocket), "SendQueuedPackages")]
    //public static class ZSteamSocket_SendQueuedPackages_Patch
    //{
    //    private static bool Prefix(ref ZSteamSocket __instance)
    //    {
    //        ZSteamSocket self = __instance;

    //        if (!self.IsConnected())
    //        {
    //            return false;
    //        }
    //        while (self.m_sendQueue.Count > 0)
    //        {
    //            Debug.Log("zsocket queue size " + self.m_sendQueue.Count);
    //            byte[] array = self.m_sendQueue.Peek();
    //            Debug.Log("zsocket size " + array.Length);
    //            IntPtr intPtr = Marshal.AllocHGlobal(array.Length);
    //            Marshal.Copy(array, 0, intPtr, array.Length);
    //            long num;
    //            EResult eresult = SteamNetworkingSockets.SendMessageToConnection(self.m_con, intPtr, (uint)array.Length, 8, out num);
    //            Debug.Log("eresult " + eresult);
    //            Debug.Log("out num " + num);
    //            Marshal.FreeHGlobal(intPtr);
    //            if (eresult != EResult.k_EResultOK)
    //            {
    //                ZLog.Log("Failed to send data " + eresult);
    //                return false;
    //            }
    //            self.m_totalSent += array.Length;
    //            self.m_sendQueue.Dequeue();
    //        }

    //        return false;
    //    }
    //}

    //[HarmonyPatch(typeof(ZSteamSocket), "Recv")]
    //public static class ZSteamSocket_Recv_Patch
    //{
    //    private static bool Prefix(ref ZSteamSocket __instance, ref ZPackage __result)
    //    {
    //        ZSteamSocket self = __instance;

    //        if (!self.IsConnected())
    //        {
    //            Debug.Log("not connected");
    //            __result = null;
    //            return false;
    //        }
    //        IntPtr[] array = new IntPtr[1];
    //        int result = SteamNetworkingSockets.ReceiveMessagesOnConnection(self.m_con, array, 1);
    //        if (result == 1)
    //        {
    //            SteamNetworkingMessage_t steamNetworkingMessage_t = Marshal.PtrToStructure<SteamNetworkingMessage_t>(array[0]);
    //            byte[] array2 = new byte[steamNetworkingMessage_t.m_cbSize];
    //            Marshal.Copy(steamNetworkingMessage_t.m_pData, array2, 0, steamNetworkingMessage_t.m_cbSize);
    //            ZPackage zpackage = new ZPackage(array2);
    //            steamNetworkingMessage_t.m_pfnRelease = array[0];
    //            steamNetworkingMessage_t.Release();
    //            self.m_totalRecv += zpackage.Size();

    //            Debug.Log("got data of size " + array2.Length);
    //            self.m_gotData = true;
    //            __result = zpackage;
    //            return false;
    //        }
    //        else
    //        {
    //            if (result > 1)
    //                Debug.Log("skipping recv because too many???");
    //        }
    //        __result = null;

    //        return false;
    //    }
    //}



    public static class MinimapShareDefaults
    {
        public static bool autoShare = false;
        public static bool acceptShare = false;
    }

    [HarmonyPatch(typeof(ZNetView), "HandleRoutedRPC")]
    public static class ZNetView_OnMapMiddleClick_Patch
    {
        private static bool Prefix(ref ZNetView __instance, ZRoutedRpc.RoutedRPCData rpcData)
        {
            ZNetView self = __instance;

            RoutedMethodBase routedMethodBase;

            if (self.m_functions.TryGetValue(rpcData.m_methodHash, out routedMethodBase))
            {
                routedMethodBase.Invoke(rpcData.m_senderPeerID, rpcData.m_parameters);
                return false;
            }
            ZLog.LogWarning("Failed to find rpc method " + rpcData.m_methodHash);
            Debug.Log(self.gameObject.name + " with view " + self.GetPrefabName());
            foreach (var f in self.m_functions)
            {
                Debug.Log("possible callback functions: " + f.Key + " , " + f.Value);
            }

            return false;
        }
    }

    [HarmonyPatch(typeof(Minimap), "OnMapMiddleClick")]
    public static class Minimap_OnMapMiddleClick_Patch
    {
        private static void Prefix(ref Minimap __instance, UIInputHandler handler)
        {
            if (MinimapShareDefaults.autoShare)
            {
                Console_InputText_Patch.BroadcastMapData(Console.instance);
            }
        }
    }

    [HarmonyPatch(typeof(Console), "InputText")]
    public class Console_InputText_Patch
    {
        static Dictionary<Vector3, Minimap.PinData> previouslyRecievedPins = new Dictionary<Vector3, Minimap.PinData>();
        public static void HandleMapData(long peerID, ZPackage mapData)
        {
            if (Player.m_localPlayer.m_nview.m_zdo.m_owner == peerID)
            {
                Debug.Log("ignoring self");
                return;
            }

            Debug.Log("wants to get map data from " + peerID);
            if (!MinimapShareDefaults.acceptShare)
            {
                Debug.Log("does not accept sharing now");
                if(incomingMapData.ContainsKey(peerID))
                    incomingMapData.Remove(peerID);
                return;
            }

            if(!incomingMapData.ContainsKey(peerID))
            {
                Debug.Log("Handling map data before ready! ERROR! Sending peer: " + peerID);
                return;
            }

            Debug.Log("Getting map data from " + peerID);

            if(incomingMapData[peerID].progress >= incomingMapData[peerID].size)
            {
                Debug.Log("Handling map data for a transfer that was already completed?! ERROR! Sending peer: " + peerID);
                return;
            }

            incomingMapData[peerID].data.Write(mapData.GetArray());
            incomingMapData[peerID].progress++;

            //ready to post
            if(incomingMapData[peerID].progress == incomingMapData[peerID].size)
            {
                ProcessMapData(incomingMapData[peerID]);
                incomingMapData.Remove(peerID);
            }
            else
            {
                Debug.Log("Got map data part " + incomingMapData[peerID].progress + " of " + incomingMapData[peerID].size + " from "+peerID);
                Console.instance.AddString("Got map data part " + incomingMapData[peerID].progress + " of " + incomingMapData[peerID].size + " from " + peerID);
            }
        }

        static void ProcessMapData(IncomingMapDataState state)
        {
            ZPackage zpackage = state.data;
            Minimap map = Minimap.instance;

            int pinsAdded = 0;
            int mapUncovered = 0;

            int version = zpackage.ReadInt();
            int size = zpackage.ReadInt();
            if (map.m_textureSize != size)
            {
                ZLog.LogWarning(string.Concat(new object[]
                {
                "Missmatching mapsize from peer ", state.owner, ". Your map size is ",
                map.m_mapTexture,
                " vs ",
                size
                }));
                return;
            }

            for (int i = 0; i < map.m_explored.Length; i++)
            {
                bool didExploreSector = zpackage.ReadBool();

                int x = i % size;
                int y = i / size;
                if (didExploreSector && !map.m_explored[i])
                {
                    map.Explore(x, y);
                    mapUncovered++;
                }
            }
            if (version >= 2)
            {
                int pinCountToImport = zpackage.ReadInt();
                for (int j = 0; j < pinCountToImport; j++)
                {
                    string name = zpackage.ReadString();
                    Vector3 pos = zpackage.ReadVector3();
                    Minimap.PinType type = (Minimap.PinType)zpackage.ReadInt();

                    if (type == Minimap.PinType.Death)
                        continue;
                    if (type == Minimap.PinType.Bed)
                        continue;

                    bool isChecked = version >= 3 && zpackage.ReadBool();

                    if (previouslyRecievedPins.ContainsKey(pos))
                    {
                        var previousPin = previouslyRecievedPins[pos];
                        if (previousPin != null)
                        {
                            previousPin.m_checked = isChecked;
                        }
                        else
                        {
                            previouslyRecievedPins.Remove(pos);
                        }

                        continue;
                    }

                    Minimap.PinData oldPin = map.m_pins.FirstOrDefault(p => Utils.DistanceXZ(pos, p.m_pos) < 1f);
                    Minimap.PinData newPin = null;

                    if (oldPin != null)
                    {
                        Console.instance.AddString("Not adding pin " + name + " from " + state.owner + " at " + pos + " because one exists there already.");
                        Debug.Log("Not adding pin " + name + " from " + state.owner + " at " + pos + " because one exists there already.");
                        newPin = oldPin;
                    }
                    else
                    {
                        pinsAdded++;

                        Console.instance.AddString("Added new pin: " + name + " at " + pos);
                        Debug.Log("Added new pin: " + name + " at " + pos);
                        newPin = map.AddPin(pos, type, name, true, isChecked);
                    }

                    previouslyRecievedPins.Add(pos, newPin);
                }
            }

            if (pinsAdded > 0 || mapUncovered > 0)
            {
                Console.instance.AddString("Finished merging maps. Total new pins added: " + pinsAdded + " Total new map explored: " + mapUncovered);
                Debug.Log("Finished merging maps. Total new pins added: " + pinsAdded + " Total new map explored: " + mapUncovered);
            }
        }

        static Dictionary<long, IncomingMapDataState> incomingMapData = new Dictionary<long, IncomingMapDataState>();
        static bool isSharing = false;
        public class IncomingMapDataState
        {
            public long owner;
            public ZPackage data;
            public int size;
            public int progress;
        }

        public static void HandleStartMapData(long peerID, ZPackage mapDataSize)
        {
            if (Player.m_localPlayer.m_nview.m_zdo.m_owner == peerID)
            {
                Debug.Log("ignoring self");
                return;
            }

            if (!MinimapShareDefaults.acceptShare)
                return;

            if (incomingMapData.ContainsKey(peerID))
            {
                //probably horrible error here....
                return;
            }

            incomingMapData.Add(peerID, new IncomingMapDataState() { owner = peerID, data = new ZPackage(), size = mapDataSize.ReadInt(), progress = 0 });
        }

        public static void BroadcastMapData(Console self)
        {
            if (isSharing)
                return;
            Player.m_localPlayer.StartCoroutine(DoBroadcastMapData());
        }

        static IEnumerator DoBroadcastMapData()
        {
            float sharePacketDelay = .1f;
            isSharing = true;
            Debug.Log("sending map data");
            var mapData = Minimap.instance.GetMapData();
            var mapDataParts = new List<ZPackage>();

            int maxPacketSize = 10000;

            bool extraSend = mapData.Length % maxPacketSize != 0;

            var mapDataSizePkg = new ZPackage();
            int packetCount = (extraSend ? 1 : 0) + (mapData.Length / maxPacketSize);
            mapDataSizePkg.Write(packetCount);
            Debug.Log("sending map size "+ packetCount);
            Player.m_localPlayer.m_nview.InvokeRPC(ZNetView.Everybody, GetMethodName("HandleStartMapData"), mapDataSizePkg);
            yield return new WaitForSeconds(sharePacketDelay);
            //send over the count

            List<byte> parts = new List<byte>();

            for (int i = 0; i < mapData.Length; ++i)
            {
                int j = i % maxPacketSize;
                int k = i / maxPacketSize;

                if (k > mapDataParts.Count)
                {
                    var newPart = new ZPackage(parts.ToArray());
                    mapDataParts.Add(newPart);
                    Debug.Log("sending map chunk " + k);
                    Console.instance.AddString("Sending map chunk: " + k + "/" + packetCount);

                    //int waitCount = 0;
                    //for(; ; ) 
                    //{
                    //    if (((ZRoutedRpc.instance.m_peers.FirstOrDefault(x => x.IsReady()).m_socket as ZSteamSocket).m_sendQueue.Count > 0))
                    //    {
                    //        waitCount++;
                    //        if (waitCount >= 60)
                    //        {
                    //            int queueSize = (ZRoutedRpc.instance.m_peers.FirstOrDefault(x => x.IsReady()).m_socket as ZSteamSocket).m_sendQueue.Count;
                    //            int queueDataSize = (ZRoutedRpc.instance.m_peers.FirstOrDefault(x => x.IsReady()).m_socket as ZSteamSocket).m_sendQueue.Peek().Length;
                    //            Debug.Log("waiting for send queue to be empty before sending next map packet... queue size: " + queueSize + " and has this much data in it " + queueDataSize);
                    //            waitCount = 0;
                    //        }
                    //        yield return new WaitForEndOfFrame();
                    //    }
                    //    else
                    //    {
                    //        Debug.Log("queue is empty, trying to send...");
                    //        break;
                    //    }
                    //}



                    Player.m_localPlayer.m_nview.InvokeRPC(ZNetView.Everybody, GetMethodName("HandleMapData"), newPart);
                    parts.Clear();
                    yield return new WaitForSeconds(sharePacketDelay);
                    //(ZRoutedRpc.instance.m_peers.FirstOrDefault(x => x.IsReady()).m_socket as ZSteamSocket).Flush();
                }

                parts.Add(mapData[i]);
            }
            yield return new WaitForSeconds(sharePacketDelay);
            //(ZRoutedRpc.instance.m_peers.FirstOrDefault(x => x.IsReady()).m_socket as ZSteamSocket).Flush();

            if (parts.Count > 0)
            {
                var newPart = new ZPackage(parts.ToArray());
                Player.m_localPlayer.m_nview.InvokeRPC(ZNetView.Everybody, GetMethodName("HandleMapData"), newPart);
                parts.Clear();
            }

            //mapDataParts.ForEach(p => Player.m_localPlayer.m_nview.InvokeRPC(ZNetView.Everybody, "HandleMapData", p));

            Debug.Log("sharing map of byte array size " + mapData.Length + " which is " + (float)mapData.Length / 1024f + " kb --> " + (float)mapData.Length / 1024f / 1024f + " mb");
            Console.instance.AddString("sharing map of byte array size " + mapData.Length + " which is " + (float)mapData.Length / 1024f + " kb --> " + (float)mapData.Length / 1024f / 1024f + " mb");
            isSharing = false;
            yield break;
        }

        static string GetMethodName(string filterName)
        {
            var result = typeof(Console_InputText_Patch).GetMethods(BindingFlags.Public | BindingFlags.Static).FirstOrDefault(x => x.Name.Contains(filterName)).Name;
            Debug.Log(result);
            return result;
        }

        public static void DoTerrainMod(Vector3 p0, Vector3 p1, float y, bool deleteOld = true, TerrainModifier.PaintType paintType = TerrainModifier.PaintType.Dirt, bool useSmooth = false)
        {
            var prefab = ObjectDB.instance.m_recipes[84].m_item.m_itemData.m_shared.m_spawnOnHitTerrain;

            //digg component
            TerrainModifier tmod = prefab.GetComponentInChildren<TerrainModifier>();

            Vector3 min = new Vector3(Mathf.Min(p0.x, p1.x), Mathf.Min(p0.y, p1.y), Mathf.Min(p0.z, p1.z));
            Vector3 max = new Vector3(Mathf.Max(p0.x, p1.x), Mathf.Max(p0.y, p1.y), Mathf.Max(p0.z, p1.z));

            if (deleteOld)
            {
                List<TerrainModifier> oldTms = new List<TerrainModifier>();
                for (float z = min.z; z < max.z; z += (tmod.m_levelRadius * 2f))
                {
                    for (float x = min.x; x < max.x; x += (tmod.m_levelRadius * 2f))
                    {
                        Vector3 pointToUse = new Vector3(x, y, z);
                        
                        TerrainModifier.GetModifiers(pointToUse, 2f, oldTms);
                    }
                }

                for(int i = 0; i < oldTms.Count; ++i)
                {
                    var item = oldTms[i];
                    if (!item.m_nview.IsValid())
                        continue;
                    item.m_nview.ClaimOwnership();
                    item.m_nview.Destroy();
                }
            }

            for (float z = min.z; z < max.z; z += (tmod.m_levelRadius * 2f))
            {
                for (float x = min.x; x < max.x; x += (tmod.m_levelRadius * 2f))
                {
                    Vector3 pointToUse = new Vector3(x, y, z);

                    TerrainModifier.SetTriggerOnPlaced(true);

                    GameObject gameObject = UnityEngine.Object.Instantiate(prefab, pointToUse, Quaternion.LookRotation(Player.m_localPlayer.transform.forward));
                    TerrainModifier newtmod = prefab.GetComponentInChildren<TerrainModifier>();
                    newtmod.m_smooth = useSmooth;
                    newtmod.m_paintType = paintType;

                    TerrainModifier.SetTriggerOnPlaced(false);
                }
            }
        }

        static Vector3 tmod_p1;
        static Vector3 tmod_p2;

        static bool setupRPC = false;
        //static float slowSpeed = 0f;
        private static void Prefix(ref Console __instance)
        {
            Console self = __instance;

            if(!setupRPC)
            {
                Player.m_localPlayer.m_nview.Register<ZPackage>(GetMethodName("HandleMapData"), new RoutedMethod<ZPackage>(HandleMapData).m_action);
                Player.m_localPlayer.m_nview.Register<ZPackage>(GetMethodName("HandleStartMapData"), new RoutedMethod<ZPackage>(HandleStartMapData).m_action);
                setupRPC = true;
            }

            string text = self.m_input.text;
            //self.AddString(text);
            string[] array = text.Split(new char[]
            {
            ' '
            });



            if (text.StartsWith("help"))
            {
                self.AddString("toggle_acceptShare - toggle the ability to accept map sharing from others");
                self.AddString("toggle_autoShare - toggle the ability to share maps when you do a ping");
                self.AddString("sharemap - shares your map with other used of the mod");
                self.AddString("stayforday - sets all nearby monster to not despawn during day");
                self.AddString("maketameable <Name> <Item> - sets all nearby monsters of type Name to be tamable by eating the Item given");
                self.AddString("setbgm <bgmname> <time> - the bgm for the biome you're currently in. only temporary and changes are gone when you log out");
                self.AddString("stopbgm - stops the currently playing bgm");
                self.AddString("setenv (l ist OR biomelist OR addenv biomename envname weight  --- adds an environment to a biome with the weight given   OR rmenv biomename envname --- removes the environment from a biome");
                self.AddString("toggle_eventsanywhere - allow events to spawn in any biome");
                self.AddString("toggle_ignoreEventKeys - ignore game progression event keys when spawning random events");
                self.AddString("setnormalevents (add|rm) <biomename> - sets the given biome to restrict it from spawning non-normal random events");
                self.AddString("tmod - commands allow you to flatten ground using a minimal amount of terrain modifiers to save fps. use tmod help for instructions");
                //self.AddString("toggle_remoteZones - enables you to try having remote zones that don't depend on player presence to be active");
                self.AddString("ZoneSystem_[Get|Set]_ActiveArea - shows/sets the active area for a zone (i think the default is 1)");
                //self.AddString("remoteZones (add|remove) <x> <y> - adds or removes remote zones --- note: toggle_remoteZones needs to be true for these to have effect");
                self.AddString("toggle_npcs - enables npc mod functions");
            }



            //if (text.StartsWith("remoteZones"))
            //{
            //    string text5 = text;
            //    char[] separator = new char[]
            //    {
            //                            ',',
            //                            ' '
            //    };
            //    string[] array3 = text5.Split(separator);
            //    if (array3.Length < 4)
            //    {
            //        if (array3.Length == 2)
            //        {
            //            string cmd2 = array3[1];
            //            if (cmd2 == "addarea")
            //            {
            //                float x = Player.m_localPlayer.transform.position.x;
            //                float y = Player.m_localPlayer.transform.position.z;
            //                for (float i = x - 200f; i < x + 200f; i += 64f)
            //                {
            //                    for (float j = y - 200f; j < y + 200f; j += 64f)
            //                    {
            //                        ZNetScene_CreateDestroyObjects_Patch.AddZone(Mathf.FloorToInt(i), Mathf.FloorToInt(j));
            //                    }
            //                }

            //                return;
            //            }
            //        }
            //        self.AddString("needs 4 arguments, ex: remoteZones add 40.0 -39.9");
            //        return;
            //    }

            //    string cmd = array3[1];
                
            //    if (float.TryParse(array3[2], out float px))
            //    {
            //        if (float.TryParse(array3[3], out float py))
            //        {
            //            if(cmd == "add")
            //            {
            //                ZNetScene_CreateDestroyObjects_Patch.AddZone(Mathf.FloorToInt(px), Mathf.FloorToInt(py));
            //            }
            //            else if (cmd == "remove")
            //            {
            //                ZNetScene_CreateDestroyObjects_Patch.AddZone(Mathf.FloorToInt(px), Mathf.FloorToInt(py));
            //            }
            //        }
            //        else
            //        {
            //            self.AddString("Failed to parse y value, needs a float");
            //        }
            //    }
            //    else
            //    {
            //        self.AddString("Failed to parse x value, needs a float");
            //    }
            //}

            if (text.StartsWith("toggle_npcs"))
            {
                NPZ.MOD_ENABLED = !NPZ.MOD_ENABLED;
                self.AddString("NPZ.MOD_ENABLED: " + NPZ.MOD_ENABLED);
                //ZNetScene_CreateDestroyObjects_Patch.ActiveArea = 4;
            }

            if (text.StartsWith("ZoneSystem_Get_ActiveArea"))
            {
                self.AddString("Zone System active area: "+ZNetScene_CreateDestroyObjects_Patch.ActiveArea);
            }

            if (text.StartsWith("ZoneSystem_Set_ActiveArea"))
            {
                string text5 = text;
                char[] separator = new char[]
                {
                                        ',',
                                        ' '
                };
                string[] array3 = text5.Split(separator);
                if (array3.Length < 2)
                {
                    self.AddString("needs 2 arguments, see help");
                    return;
                }

                if (int.TryParse(array3[1], out int area))
                {
                    ZNetScene_CreateDestroyObjects_Patch.ActiveArea = area;
                    self.AddString("Zone System active area: " + ZNetScene_CreateDestroyObjects_Patch.ActiveArea);
                }
                else
                {
                    self.AddString("Failed to parse area value. Needs to be an int");
                }
            }

            if (text.StartsWith("tmod"))
            {
                string text5 = text;
                char[] separator = new char[]
                {
                                        ',',
                                        ' '
                };
                string[] array3 = text5.Split(separator);
                if (array3.Length < 2)
                {
                    self.AddString("needs 2 or more arguments, see help");
                    return;
                }

                string cmd = array3[1];
                //string biome = array3[2];

                try
                {
                    if(cmd == "help")
                    {
                        self.AddString("setp1 = set point 1,\n setp2 = set point 2,\n domod <float:yheight> <bool:deleteold> <enum:PaintType> <bool:useSmoothing> ,\n [domod_ez_dirt | _ez_pave | _ez_cultivate] = use lowest y value of p1 and p2, delete set to true, smooth set to true, paint type is in the name of the command");
                    }
                    else if (cmd == "setp1")
                    {
                        tmod_p1 = Player.m_localPlayer.transform.position;
                    }
                    else if (cmd == "setp2")
                    {
                        tmod_p2 = Player.m_localPlayer.transform.position;
                    }
                    else if (cmd == "domod_ez_dirt")
                    {
                        float yheight = Mathf.Min(tmod_p1.y, tmod_p2.y);
                        bool deletemods = true;
                        var paveType = TerrainModifier.PaintType.Dirt;
                        bool useSmooth = true;
                        DoTerrainMod(tmod_p1, tmod_p2, yheight, deletemods, paveType, useSmooth);
                    }
                    else if (cmd == "domod_ez_pave")
                    {
                        float yheight = Mathf.Min(tmod_p1.y, tmod_p2.y);
                        bool deletemods = true;
                        var paveType = TerrainModifier.PaintType.Paved;
                        bool useSmooth = true;
                        DoTerrainMod(tmod_p1, tmod_p2, yheight, deletemods, paveType, useSmooth);
                    }
                    else if (cmd == "domod_ez_cultivate")
                    {
                        float yheight = Mathf.Min(tmod_p1.y, tmod_p2.y);
                        bool deletemods = true;
                        var paveType = TerrainModifier.PaintType.Cultivate;
                        bool useSmooth = true;
                        DoTerrainMod(tmod_p1, tmod_p2, yheight, deletemods, paveType, useSmooth);
                    }
                    else if(cmd == "domod")
                    {
                        if (array3.Length < 6)
                        {
                            self.AddString("needs 6 arguments, example: tmod domod 40.5 True Dirt True");
                            return;
                        }
                        float yheight = float.Parse(array3[2]);
                        bool deletemods = bool.Parse(array3[3]);
                        var paveType = (TerrainModifier.PaintType)System.Enum.Parse(typeof(TerrainModifier.PaintType), array3[4]);
                        bool useSmooth = bool.Parse(array3[5]);
                        DoTerrainMod(tmod_p1, tmod_p2, yheight, deletemods, paveType, useSmooth);
                    }
                }
                catch (Exception)
                {
                    self.AddString("Error: Error parsing parameters....");
                }
            }


            if (text.StartsWith("pos"))
            {
                Player localPlayer = Player.m_localPlayer;
                if (localPlayer)
                {
                    self.AddString("Player position (X,Y,Z):" + localPlayer.transform.position.ToString("F0"));
                }
            }

            if (text.StartsWith("toggle_aoecleave"))
            {
                Attack_DoAreaAttack_Patch.canCleaveTerrain = !Attack_DoAreaAttack_Patch.canCleaveTerrain;
            }

            if (text.StartsWith("toggle_turbo"))
            {
                Player_PlayerAttackInput_Patch.turbo = !Player_PlayerAttackInput_Patch.turbo;
            }
            if (text.StartsWith("toggle_eventsanywhere"))
            {
                RandomEventPatchSettings.enableEventsAnywhere = !RandomEventPatchSettings.enableEventsAnywhere;
                self.AddString("toggle_eventsanywhere = " + RandomEventPatchSettings.enableEventsAnywhere);
            }
            if (text.StartsWith("toggle_ignoreEventKeys"))
            {
                RandomEventPatchSettings.ignoreEventKeys = !RandomEventPatchSettings.ignoreEventKeys;
                self.AddString("toggle_ignoreEventKeys = "+ RandomEventPatchSettings.ignoreEventKeys);
            }
            if (text.StartsWith("setnormalevents"))
            {
                string text5 = text;
                char[] separator = new char[]
                {
                                        ',',
                                        ' '
                };
                string[] array3 = text5.Split(separator);
                if (array3.Length < 3)
                {
                    self.AddString("needs 3 arguments, see help");
                    return;
                }

                string cmd = array3[1];
                string biome = array3[2];

                try
                {
                    var biomeEnum = (Heightmap.Biome)System.Enum.Parse(typeof(Heightmap.Biome), biome);
                    var normalBiomes = RandomEventPatchSettings.normalBiomes;
                    
                    if (cmd == "add")
                    {
                        if (!normalBiomes.Contains(biomeEnum))
                            normalBiomes.Add(biomeEnum);

                        self.AddString(biome + " will now handle random events normally");
                    }
                    else if (cmd == "rm")
                    {
                        if (normalBiomes.Contains(biomeEnum))
                            normalBiomes.Remove(biomeEnum);

                        self.AddString(biome + " will now be allowed to have extra events");
                    }
                }
                catch(Exception)
                {
                    self.AddString("Error: biome name is not valid");
                }
            }


            if (text.StartsWith("setenv"))
            {
                string text5 = text;
                char[] separator = new char[]
                {
                                        ',',
                                        ' '
                };
                string[] array3 = text5.Split(separator);
                if (array3.Length < 2)
                {
                    return;
                }
                try
                {
                    string cmd = array3[1];

                    if(cmd == "list")
                    {
                        self.AddString("Environment Names");
                        EnvMan.instance.m_environments.ForEach(x => self.AddString(x.m_name));
                    }
                    if (cmd == "biomelist")
                    {
                        self.AddString("Biome Names");
                        EnvMan.instance.m_biomes.ForEach(x => self.AddString(x.m_name));
                    }
                    else if(cmd == "addenv")
                    {
                        if (array3.Length < 5)
                        {
                            self.AddString("syntax example: setenv addenv Meadows ThunderStorm 1.5  (most high freq weather is a 1.0)");
                            return;
                        }

                        string biome = array3[2];
                        string env = array3[3];
                        float weight = float.Parse(array3[4]);

                        var biomeobj = EnvMan.instance.m_biomes.FirstOrDefault(b => b.m_name == biome);
                        var envobj = EnvMan.instance.m_environments.FirstOrDefault(b => b.m_name == env);
                        
                        if (!biomeobj.m_environments.Any(x => x.m_environment == envobj.m_name))
                        {
                            EnvEntry newEnv = new EnvEntry() { m_env = envobj, m_environment = envobj.m_name, m_weight = weight };
                            biomeobj.m_environments.Add(newEnv);
                            self.AddString("added "+ envobj.m_name + " to biome "+ biomeobj.m_name);
                        }
                        else
                        {
                            self.AddString(" " + envobj.m_name + " was already in biome " + biomeobj.m_name);
                        }
                    }
                    else if (cmd == "rmenv")
                    {
                        if (array3.Length < 4)
                        {
                            self.AddString("syntax example: rmenv addenv Meadows ThunderStorm ");
                            return;
                        }

                        string biome = array3[2];
                        string env = array3[3];

                        var biomeobj = EnvMan.instance.m_biomes.FirstOrDefault(b => b.m_name == biome);
                        var envobj = EnvMan.instance.m_environments.FirstOrDefault(b => b.m_name == env);
                        var selected = biomeobj.m_environments.FirstOrDefault(x => x.m_environment == envobj.m_name);

                        if (selected != null && biomeobj.m_environments.Count > 1)
                        {
                            biomeobj.m_environments.Remove(selected);
                            self.AddString("removed " + envobj.m_name + " from biome " + biomeobj.m_name);
                        }
                        else
                        {
                            self.AddString("Either the biome only had 1 environment remaining or " + envobj.m_name + " was not in biome " + biomeobj.m_name);
                        }
                    }
                }
                catch (System.Exception ex)
                {
                    ZLog.Log("parse error:" + ex.ToString() + "  " + text5);
                }
            }

            if (text.StartsWith("stopbgm"))
            {
                MusicMan.instance.StopMusic();
            }


            if (text.StartsWith("stayforday"))
            {
                ZNetView[] views = UnityEngine.Object.FindObjectsOfType<ZNetView>();
                foreach (ZNetView v in views)
                {
                    if (v.GetComponent<MonsterAI>() != null)
                    {
                        v.GetComponent<MonsterAI>().SetDespawnInDay(false);
                    }
                }
            }

            if (text.StartsWith("setbgm"))
            {
                string text5 = text;
                char[] separator = new char[]
                {
                                        ',',
                                        ' '
                };
                string[] array3 = text5.Split(separator);
                if (array3.Length < 3)
                {
                    if(array3.Length == 2)
                    {
                        List<string> songslist = MusicMan.instance.m_music.Select(x => x.m_name).ToList();

                        self.AddString("song list:");
                        songslist.ForEach(z => self.AddString(z));
                        return;
                    }

                    self.AddString("Syntax /setbgm bgmname time    possible time options are: (day/night/morning/evening/all)");
                    self.AddString("use /setbgm list to see possible options");
                    return;
                }
                try
                {
                    string name = array3[1];
                    string time = array3[2];

                    List<string> songs = MusicMan.instance.m_music.Select(x => x.m_name).ToList();

                    if(songs.Contains(name))
                    {
                        var currentBiome = EnvMan.instance.m_biomes.FirstOrDefault(x => x.m_biome == EnvMan.instance.m_currentBiome);
                        time = time.ToLower();

                        if (time == "day")
                        {
                            currentBiome.m_musicDay = name;
                        }
                        else
                        if (time == "night")
                        {
                            currentBiome.m_musicNight = name;
                        }
                        else
                        if (time == "morning")
                        {
                            currentBiome.m_musicMorning = name;
                        }
                        else
                        if (time == "evening")
                        {
                            currentBiome.m_musicEvening = name;
                        }
                        else
                        //if (time == "all")
                        {
                            currentBiome.m_musicDay = name;
                            currentBiome.m_musicNight = name;
                            currentBiome.m_musicMorning = name;
                            currentBiome.m_musicEvening = name;
                        }
                        self.AddString("set " + name + " to the bgm for "+currentBiome.m_name + " at time " + time);
                    }
                    else
                    {
                        self.AddString("song "+name+" not found in game's internal song list");

                        self.AddString("song list:");
                        songs.ForEach(z => self.AddString(z));
                    }
                }
                catch (System.Exception ex)
                {
                    ZLog.Log("parse error:" + ex.ToString() + "  " + text5);
                }
            }

            if (text.StartsWith("maketameable"))
            {
                string text5 = text;
                char[] separator = new char[]
                {
                                        ',',
                                        ' '
                };
                string[] array3 = text5.Split(separator);
                if (array3.Length < 2)
                {
                    self.AddString("Syntax /maketameable Serpent Wood");
                    return;
                }
                try
                {
                    string name = array3[1];
                    string item = array3[2];

                    ZNetView[] views = UnityEngine.Object.FindObjectsOfType<ZNetView>();
                    foreach (ZNetView v in views)
                    {
                        if (v.GetComponent<MonsterAI>() != null && v.GetComponent<Character>().m_name.Contains(name))
                        {
                            var monster = v.GetComponent<MonsterAI>();

                            var itemP = ObjectDB.instance.GetItemPrefab(item).GetComponent<ItemDrop>();
                            if (itemP == null)
                            {
                                self.AddString("Item not found");
                                return;
                            }

                            monster.m_consumeItems.Clear();
                            monster.m_consumeItems.Add(itemP);

                            var tameable = v.GetComponent<Tameable>();
                            if(tameable == null)
                            {
                                tameable = v.gameObject.AddComponent<Tameable>();
                            }

                            self.AddString(monster.name + " is now tameable");
                        }
                    }
                }
                catch (System.Exception ex)
                {
                    ZLog.Log("parse error:" + ex.ToString() + "  " + text5);
                }

            }

            if (text.StartsWith("toggle_acceptShare"))
            {
                MinimapShareDefaults.acceptShare = !MinimapShareDefaults.acceptShare;
                self.AddString("acceptShare = " + MinimapShareDefaults.acceptShare);
            }
            if (text.StartsWith("toggle_autoShare"))
            {
                MinimapShareDefaults.autoShare = !MinimapShareDefaults.autoShare;
                self.AddString("autoShare = " + MinimapShareDefaults.autoShare);
            }
            if (text.StartsWith("sharemap"))
            {
                BroadcastMapData(self);
                //ZPackage pkg = new ZPackage(mapData);

                //ZRoutedRpc.instance.InvokeRoutedRPC(ZRoutedRpc.Everybody, "HandleMapData", pkg);
                //Player.m_localPlayer.m_nview.InvokeRPC(ZNetView.Everybody, "HandleMapData", pkg);
            }

            if (text.StartsWith("goto "))
            {
                string text5 = text.Substring(5);
                char[] separator = new char[]
                {
                                        ',',
                                        ' '
                };
                string[] array3 = text5.Split(separator);
                if (array3.Length < 2)
                {
                    self.AddString("Syntax /goto x,y");
                    return;
                }
                try
                {
                    float x = float.Parse(array3[0]);
                    float z = float.Parse(array3[1]);
                    Player localPlayer2 = Player.m_localPlayer;
                    if (localPlayer2)
                    {
                        Vector3 pos2 = new Vector3(x, localPlayer2.transform.position.y, z);
                        localPlayer2.TeleportTo(pos2, localPlayer2.transform.rotation, true);
                    }
                }
                catch (System.Exception ex)
                {
                    ZLog.Log("parse error:" + ex.ToString() + "  " + text5);
                }
                Gogan.LogEvent("Cheat", "Goto", "", 0L);
                return;
            }

            //TODO: show player stats (requires a bit more reading to get the precise flow right... do later
            //if (text.StartsWith("cstats "))
            //{
            //    string text5 = text.Substring(5);
            //    char[] separator = new char[]
            //    {
            //                            ',',
            //                            ' '
            //    };
            //    string[] array3 = text5.Split(separator);
            //    if (array3.Length < 1)
            //    {
            //        self.AddString("Syntax /cstats <PlayerName>");
            //        return;
            //    }
            //    try
            //    {
            //        string cname = array3[0];
            //        Player player2 = Player.GetAllPlayers().FirstOrDefault(x => x.m_name.Contains(cname));
            //        if (player2)
            //        {
            //            PlayerProfile.get
            //            self.AddString("Stats for player: " + player2.m_name);
            //            self.AddString("Stats for player: " + player2.m_name);

            //        }
            //    }
            //    catch (System.Exception ex)
            //    {
            //        ZLog.Log("parse error:" + ex.ToString() + "  " + text5);
            //    }
            //}

            if (text.StartsWith("god"))
            {
                Player.m_localPlayer.SetGodMode(!Player.m_localPlayer.InGodMode());
                self.Print("God mode:" + Player.m_localPlayer.InGodMode().ToString());
                Gogan.LogEvent("Cheat", "God", Player.m_localPlayer.InGodMode().ToString(), 0L);
            }
        }

    }
}


/*




		if (array[0] == "countviews")
		{
			ZNetView[] array4 = UnityEngine.Object.FindObjectsOfType<ZNetView>();
			int count = (from x in array4
			where x.GetComponent<Fish>() != null
			select x).ToList<ZNetView>().Count;
			int count2 = (from x in array4
			where x.GetComponent<Character>() != null
			select x).ToList<ZNetView>().Count;
			int count3 = (from x in array4
			where x.GetComponent<ItemDrop>() != null
			select x).ToList<ZNetView>().Count;
			int count4 = (from x in array4
			where x.GetComponent<Piece>() != null
			select x).ToList<ZNetView>().Count;
			int count5 = array4.ToList<ZNetView>().Count;
			this.AddString("fish: " + count);
			this.AddString("char: " + count2);
			this.AddString("items: " + count3);
			this.AddString("pieces: " + count4);
			this.AddString("views: " + count5);
			foreach (ZNetView v in array4)
			{
				Debug.Log("view: " + v.name);
			}
			return;
		}
        
        
		if (array[0] == "getfish")
		{
			foreach (ZNetView znetView2 in (from x in UnityEngine.Object.FindObjectsOfType<ZNetView>()
			where x.GetComponent<Fish>() != null
			select x).ToList<ZNetView>())
			{
				znetView2.transform.position = Player.m_localPlayer.transform.position + Vector3.forward * 2f;
			}
			return;
		}
		if (array[0] == "getitems")
		{
			foreach (ZNetView znetView in (from x in UnityEngine.Object.FindObjectsOfType<ZNetView>()
			where x.GetComponent<ItemDrop>() != null
			select x).ToList<ZNetView>())
			{
				if (znetView.GetComponent<ItemDrop>().CanPickup())
				{
					znetView.transform.position = Player.m_localPlayer.transform.position + Vector3.forward * 2f;
				}
			}
			return;
		}



 
*/
