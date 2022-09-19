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
    [HarmonyPatch(typeof(MonsterAI), "Awake")]
    public static class MonsterAI_Awake_Patch
    {
        private static void Postfix(ref MonsterAI __instance)
        {
            MonsterAI self = __instance;

            if (!NPZ.MOD_ENABLED)
                return;
        }
    }

    [HarmonyPatch(typeof(MonsterAI), "Start")]
    public static class MonsterAI_Start_Patch
    {
        private static void Postfix(ref MonsterAI __instance)
        {
            MonsterAI self = __instance;

            if (!NPZ.MOD_ENABLED)
                return;
        }
    }

    [HarmonyPatch(typeof(MonsterAI), "OnDamaged")]
    public static class MonsterAI_OnDamaged_Patch
    {
        private static void Postfix(ref MonsterAI __instance, float damage, Character attacker)
        {
            MonsterAI self = __instance;

            if (!NPZ.MOD_ENABLED)
                return;
        }
    }

    [HarmonyPatch(typeof(MonsterAI), "SetTarget")]
    public static class MonsterAI_SetTarget_Patch
    {
        private static void Postfix(ref MonsterAI __instance, Character attacker)
        {
            MonsterAI self = __instance;

            if (!NPZ.MOD_ENABLED)
                return;
        }
    }

    [HarmonyPatch(typeof(MonsterAI), "RPC_OnNearProjectileHit")]
    public static class MonsterAI_RPC_OnNearProjectileHit_Patch
    {
        private static void Postfix(ref MonsterAI __instance, long sender, Vector3 center, float range, ZDOID attackerID)
        {
            MonsterAI self = __instance;

            if (!NPZ.MOD_ENABLED)
                return;
        }
    }

    [HarmonyPatch(typeof(MonsterAI), "MakeTame")]
    public static class MonsterAI_MakeTame_Patch
    {
        private static void Postfix(ref MonsterAI __instance)
        {
            MonsterAI self = __instance;

            if (!NPZ.MOD_ENABLED)
                return;
        }
    }

    [HarmonyPatch(typeof(MonsterAI), "UpdateTarget", new Type[] { typeof(Humanoid), typeof(float), typeof(bool), typeof(bool) }, new ArgumentType[] { ArgumentType.Normal, ArgumentType.Normal, ArgumentType.Out, ArgumentType.Out } )]
    public static class MonsterAI_UpdateTarget_Patch
    {
        private static bool Prefix(ref MonsterAI __instance, Humanoid humanoid, float dt, ref bool canHearTarget, ref bool canSeeTarget)
        {
            MonsterAI self = __instance;

            if (!NPZ.MOD_ENABLED)
                return true;

            canHearTarget = false;
            canSeeTarget = false;

            NPC npc = self.GetComponent<NPC>();

            self.m_updateTargetTimer -= dt;
            npc.CheckForNearbyEnemies(self);

            if (self.m_targetCreature && self.m_targetCreature.IsDead())
                self.m_targetCreature = null;
            
            if (self.m_targetCreature)
            {
                canHearTarget = self.CanHearTarget(self.m_targetCreature);
                canSeeTarget = self.CanSeeTarget(self.m_targetCreature);
                if (canSeeTarget | canHearTarget)
                {
                    self.m_timeSinceSensedTargetCreature = 0f;
                }
                if (self.m_targetCreature.IsPlayer())
                {
                    self.m_targetCreature.OnTargeted(canSeeTarget | canHearTarget, self.IsAlerted());
                }
                self.SetTargetInfo(self.m_targetCreature.GetZDOID());
            }
            else
            {
                self.SetTargetInfo(ZDOID.None);
            }

            self.m_timeSinceSensedTargetCreature += dt;
            self.m_timeSinceAttacking += dt;

            npc.CheckIfShouldLoseAggro(self);

            return false;
        }
    }

    [HarmonyPatch(typeof(MonsterAI), "UpdateAI")]
    public static class MonsterAI_UpdateAI_Patch
    {
        private static bool Prefix(ref MonsterAI __instance, float dt)
        {
            MonsterAI self = __instance;

            if (!NPZ.MOD_ENABLED)
                return true;

            NPC npc = self.GetComponent<NPC>();

            self.m_alerted = self.m_nview.GetZDO().GetBool("alert", false);

            if (!self.m_nview.IsOwner())
                return false;

            if (self.m_randomMoveUpdateTimer > 0f)
                self.m_randomMoveUpdateTimer -= dt;

            self.m_timeSinceHurt += dt;
            self.m_aiStatus = "";

            //check for nearby danger/targets
            Humanoid humanoid = self.m_character as Humanoid;
            bool canHearTarget;
            bool canSeeTarget;
            self.UpdateTarget(humanoid, dt, out canHearTarget, out canSeeTarget);

            //check if should run away
            bool shouldFlee = npc.CheckIfShouldFlee(self);
            if (shouldFlee)
            {
                self.Flee(dt, self.m_targetCreature.transform.position);
                return false;
            }
            
            //eating AI
            bool shouldEat = npc.CheckIfShouldEat(self);
            if (shouldEat)
            {
                self.UpdateConsumeItem(npc.m_humanoid, dt);
                return false;
            }

            //attacking AI
            bool didAttack = npc.TryAndUpdateAttack(self, dt, canSeeTarget, canHearTarget);
            if (didAttack)
                return false;

            //misc
            if (self.m_follow)
            {
                self.Follow(self.m_follow, dt);
                self.m_aiStatus = "Follow";
                return false;
            }            

            self.m_aiStatus = string.Concat(new object[]
            {
            "Random movement (weapon: ",
            (npc.currentWeapon != null) ? npc.currentWeapon.m_shared.m_name : "none",
            ") (targetpiece: ",
            self.m_targetStatic,
            ") (target: ",
            self.m_targetCreature ? self.m_targetCreature.gameObject.name : "none",
            ")"
            });

            self.IdleMovement(dt);
            //satisfy the find and go to water AI
            //if (self.m_avoidLand && !self.m_character.IsSwiming())
            //{
            //    self.m_aiStatus = "Move to water";
            //    self.MoveToWater(dt, 20f);
            //    return false;
            //}

            //circle around the target AI
            //if (self.m_circleTargetInterval > 0f && self.m_targetCreature)
            //{
            //    if (self.m_targetCreature)
            //    {
            //        self.m_pauseTimer += dt;
            //        if (self.m_pauseTimer > self.m_circleTargetInterval)
            //        {
            //            if (self.m_pauseTimer > self.m_circleTargetInterval + self.m_circleTargetDuration)
            //            {
            //                self.m_pauseTimer = 0f;
            //            }
            //            self.RandomMovementArroundPoint(dt, self.m_targetCreature.transform.position, self.m_circleTargetDistance, self.IsAlerted());
            //            self.m_aiStatus = "Attack pause";
            //            return false;
            //        }
            //    }
            //    else
            //    {
            //        self.m_pauseTimer = 0f;
            //    }
            //}




            //flying AI
            //if ((self.m_character.IsFlying() ? self.m_circulateWhileChargingFlying : self.m_circulateWhileCharging) && (self.m_targetStatic != null || self.m_targetCreature != null) && weaponItem != null && !flag3 && !self.m_character.InAttack())
            //{
            //    self.m_aiStatus = "Move around target weapon ready:" + flag3.ToString();
            //    if (weaponItem != null)
            //    {
            //        self.m_aiStatus = self.m_aiStatus + " Weapon:" + weaponItem.m_shared.m_name;
            //    }
            //    Vector3 point = self.m_targetCreature ? self.m_targetCreature.transform.position : self.m_targetStatic.transform.position;
            //    self.RandomMovementArroundPoint(dt, point, self.m_randomMoveRange, self.IsAlerted());
            //    return false;
            //}

            return false;
        }

        private static void Postfix(ref MonsterAI __instance, float dt)
        {
            MonsterAI self = __instance;

            if (!NPZ.MOD_ENABLED)
                return;
        }
    }

    [HarmonyPatch(typeof(MonsterAI), "UpdateConsumeItem")]
    public static class MonsterAI_UpdateConsumeItem_Patch
    {
        private static bool Postfix(bool result, ref MonsterAI __instance, Humanoid humanoid, float dt)
        {
            MonsterAI self = __instance;

            if (!NPZ.MOD_ENABLED)
                return result;
            return result;
        }
    }

    [HarmonyPatch(typeof(MonsterAI), "FindClosestConsumableItem")]
    public static class MonsterAI_FindClosestConsumableItem_Patch
    {
        private static ItemDrop Postfix(ItemDrop item, ref MonsterAI __instance, float maxRange)
        {
            MonsterAI self = __instance;

            if (!NPZ.MOD_ENABLED)
                return item;
            return item;
        }
    }

    [HarmonyPatch(typeof(MonsterAI), "CanConsume")]
    public static class MonsterAI_CanConsume_Patch
    {
        private static bool Postfix(bool result, ref MonsterAI __instance, ItemDrop.ItemData item)
        {
            MonsterAI self = __instance;

            if (!NPZ.MOD_ENABLED)
                return result;
            return result;
        }
    }

    [HarmonyPatch(typeof(MonsterAI), "SelectBestAttack")]
    public static class MonsterAI_SelectBestAttack_Patch
    {
        private static ItemDrop.ItemData Postfix(ItemDrop.ItemData weapon, ref MonsterAI __instance, Humanoid humanoid, float dt)
        {
            MonsterAI self = __instance;

            if (!NPZ.MOD_ENABLED)
                return weapon;
            return weapon;
        }
    }

    [HarmonyPatch(typeof(MonsterAI), "DoAttack")]
    public static class MonsterAI_DoAttack_Patch
    {
        private static bool Postfix(bool result, ref MonsterAI __instance, Character target, bool isFriend)
        {
            MonsterAI self = __instance;

            if (!NPZ.MOD_ENABLED)
                return result;
            return result;
        }
    }

    [HarmonyPatch(typeof(MonsterAI), "SetDespawnInDay")]
    public static class MonsterAI_SetDespawnInDay_Patch
    {
        private static void Postfix(ref MonsterAI __instance, bool despawn)
        {
            MonsterAI self = __instance;

            if (!NPZ.MOD_ENABLED)
                return;
        }
    }

    [HarmonyPatch(typeof(MonsterAI), "DespawnInDay")]
    public static class MonsterAI_DespawnInDay_Patch
    {
        private static bool Postfix(bool despawnInDay, ref MonsterAI __instance)
        {
            MonsterAI self = __instance;

            if (!NPZ.MOD_ENABLED)
                return despawnInDay;

            return despawnInDay;
        }
    }

    [HarmonyPatch(typeof(MonsterAI), "SetEventCreature")]
    public static class MonsterAI_SetEventCreature_Patch
    {
        private static void Postfix(ref MonsterAI __instance, bool despawn)
        {
            MonsterAI self = __instance;

            if (!NPZ.MOD_ENABLED)
                return;
        }
    }

    [HarmonyPatch(typeof(MonsterAI), "IsEventCreature")]
    public static class MonsterAI_IsEventCreature_Patch
    {
        private static bool Postfix(bool result, ref MonsterAI __instance)
        {
            MonsterAI self = __instance;

            if (!NPZ.MOD_ENABLED)
                return result;

            return result;
        }
    }

    [HarmonyPatch(typeof(MonsterAI), "GetTargetCreature")]
    public static class MonsterAI_GetTargetCreature_Patch
    {
        private static Character Postfix(Character target, ref MonsterAI __instance)
        {
            MonsterAI self = __instance;

            if (!NPZ.MOD_ENABLED)
                return target;

            return target;
        }
    }

    [HarmonyPatch(typeof(MonsterAI), "UpdateSleep")]
    public static class MonsterAI_UpdateSleep_Patch
    {
        private static void Postfix(ref MonsterAI __instance, float dt)
        {
            MonsterAI self = __instance;

            if (!NPZ.MOD_ENABLED)
                return;
        }
    }

    [HarmonyPatch(typeof(MonsterAI), "Wakeup")]
    public static class MonsterAI_Wakeup_Patch
    {
        private static void Postfix(ref MonsterAI __instance)
        {
            MonsterAI self = __instance;

            if (!NPZ.MOD_ENABLED)
                return;
        }
    }

    [HarmonyPatch(typeof(MonsterAI), "IsSleeping")]
    public static class MonsterAI_IsSleeping_Patch
    {
        private static bool Postfix(bool result, ref MonsterAI __instance)
        {
            MonsterAI self = __instance;

            if (!NPZ.MOD_ENABLED)
                return result;

            return result;
        }
    }

    [HarmonyPatch(typeof(MonsterAI), "SetAlerted")]
    public static class MonsterAI_SetAlerted_Patch
    {
        private static void Postfix(ref MonsterAI __instance, bool alert)
        {
            MonsterAI self = __instance;

            if (!NPZ.MOD_ENABLED)
                return;
        }
    }

    [HarmonyPatch(typeof(MonsterAI), "HuntPlayer")]
    public static class MonsterAI_HuntPlayer_Patch
    {
        private static bool Postfix(bool result, ref MonsterAI __instance)
        {
            MonsterAI self = __instance;

            if (!NPZ.MOD_ENABLED)
                return result;

            return result;
        }
    }

    [HarmonyPatch(typeof(MonsterAI), "GetFollowTarget")]
    public static class MonsterAI_GetFollowTarget_Patch
    {
        private static GameObject Postfix(GameObject go, ref MonsterAI __instance)
        {
            MonsterAI self = __instance;

            if (!NPZ.MOD_ENABLED)
                return go;

            return go;
        }
    }

    [HarmonyPatch(typeof(MonsterAI), "SetFollowTarget")]
    public static class MonsterAI_SetFollowTarget_Patch
    {
        private static void Postfix(ref MonsterAI __instance, GameObject go)
        {
            MonsterAI self = __instance;

            if (!NPZ.MOD_ENABLED)
                return;
        }
    }
}
