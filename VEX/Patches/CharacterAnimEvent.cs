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
    [HarmonyPatch(typeof(CharacterAnimEvent), "DodgeMortal")]
    public static class CharacterAnimEvent_DodgeMortal_Patch
    {
        private static void Postfix(ref CharacterAnimEvent __instance)
        {
            CharacterAnimEvent self = __instance;

            NPC npc = self.m_character.GetComponent<NPC>();
            if (npc)
            {
                npc.OnDodgeMortal();
            }
        }
    }
 
    [HarmonyPatch(typeof(CharacterAnimEvent), "GPower")]
    public static class CharacterAnimEvent_GPower_Patch
    {
        private static void Postfix(ref CharacterAnimEvent __instance)
        {
            CharacterAnimEvent self = __instance;

            NPC npc = self.m_character.GetComponent<NPC>();
            if (npc)
            {
                npc.ActivateGuardianPower();
            }
        }
    }
}
