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

    [HarmonyPatch(typeof(Game), "UpdateRespawn")]
    public static class Game_UpdateRespawn_Patch
    {
        private static bool Prefix(ref Game __instance, float dt)
        {
            bool isDev = (__instance.m_playerProfile.m_playerName == "Nue" || __instance.m_playerProfile.m_playerName == "Tyr" || __instance.m_playerProfile.m_playerName == "Kerr");

            if (!isDev)
                return true;

            Vector3 vector;
            bool flag;
            if (__instance.m_requestRespawn && __instance.FindSpawnPoint(out vector, out flag, dt))
            {
                if (!flag)
                {
                    __instance.m_playerProfile.SetHomePoint(vector);
                }
                __instance.SpawnPlayer(vector);
                __instance.m_requestRespawn = false;
                if (__instance.m_firstSpawn)
                {
                    __instance.m_firstSpawn = false;
                    float choice = UnityEngine.Random.value;

                    if (choice > .65f)
                    {
                        Chat.instance.SendText(Talker.Type.Shout, "What is best in life?!");
                    }
                    else if (choice > .5f)
                    {
                        Chat.instance.SendText(Talker.Type.Shout, "Nice to mead you!");
                    }
                    else if (choice > .3f)
                    {
                        Chat.instance.SendText(Talker.Type.Shout, "GOOD NEWS everyone!");
                    }
                    else if (choice > .1f)
                    {
                        Chat.instance.SendText(Talker.Type.Shout, "You seek to draw the General of the Frostwolf legion out from his fortress? PREPOSTEROUS!");
                    }
                    else
                    {
                        Chat.instance.SendText(Talker.Type.Shout, "BOOOOOONNNNE STOOOORMMM!!!!");
                    }
                }

                GC.Collect();
            }

            return false;
        }
    }

}
