using HarmonyLib;
using UnityEngine;
using System.Linq;
using System.Collections.Generic;
using HarmonyLib.Tools;
using System.Reflection;

namespace VEX.Patches
{
    //[HarmonyPatch(typeof(BaseAI), "GetPatrolPoint")]
    //public static class BaseAI_GetPatrolPoint_Patch
    //{
    //    private static bool Prefix(ref BaseAI __instance, ref bool __result, out Vector3 point)
    //    {
    //        point = __instance.m_patrolPoint;

    //        if (!NPZ.MOD_ENABLED)
    //            return true;
            
    //        BaseAI self = __instance;
    //        NPC npc = self.GetComponent<NPC>();
    //        if (npc != null)
    //        {
    //            if (npc.m_monster.m_patrolPoint)


    //                __result = npc.GetHoverName();
    //            return false;
    //        }

    //        return true;
    //    }
    //}

    [HarmonyPatch(typeof(BaseAI), "MoveTowards")]
    public static class BaseAI_MoveTowards_Patch
    {
        public static float GetRunCooldown(BaseAI self)
        {
            if (!self.m_nview.IsValid())
            {
                return 0f;
            }
            return self.m_nview.GetZDO().GetFloat("vex_npc_runCooldown", 0f);
        }

        public static void SetRunCooldown(BaseAI self, float value)
        {
            if (!self.m_nview.IsValid())
            {
                return;
            }
            self.m_nview.GetZDO().Set("vex_npc_runCooldown", value);
        }

        public static float enableRunDistance = 5f;
        public static float disableRunStaminaThreshold = .5f;
        public static float enableRunStaminaThreshold = .9f;

        //public static float jumpOnHeightDifference = 2f;
        public static float disableJumpStaminaThreshold = .35f;
        
        private static bool Prefix(ref BaseAI __instance, Vector3 dir, ref bool run)
        {
            if (!NPZ.MOD_ENABLED)
                return true;

            BaseAI self = __instance;
            NPC npc = self.GetComponent<NPC>();
            if (npc != null)
            {
                if (Vector3.Distance(npc.transform.position, npc.m_monster.m_patrolPoint) > enableRunDistance)
                {
                    if (npc.GetStaminaPercentage() > disableRunStaminaThreshold && GetRunCooldown(self) <= 0f)
                    {
                        run = true;
                    }
                    else
                    {
                        if (npc.GetStaminaPercentage() < disableRunStaminaThreshold)
                        {
                            SetRunCooldown(self,1f);                                
                        }
                        else if(npc.GetStaminaPercentage() >= enableRunStaminaThreshold && GetRunCooldown(self) <= enableRunStaminaThreshold)
                        {
                            SetRunCooldown(self, 0f);
                        }
                    }
                }

                return false;
            }

            return true;
        }

        private static void Postfix(ref BaseAI __instance, Vector3 dir, bool run)
        {
            if (!NPZ.MOD_ENABLED)
                return;

            BaseAI self = __instance;
            NPC npc = self.GetComponent<NPC>();
            if (npc != null)
            {

                if (npc.GetStaminaPercentage() > disableJumpStaminaThreshold)
                {
                    float num = Mathf.Acos(Mathf.Clamp01(npc.m_character.m_lastGroundNormal.y)) * 57.29578f;

                    //are we trying to move up a surface that is too steep?
                    if (num > npc.m_character.GetSlideAngle())
                    {
                        //TODO: look and see if they're heading up hill VS downhill and only trigger jump when it's not downhill

                        //TODO: look at getting the position right in front of them and seeing if they should jump over something
                        //if (npc.m_monster.m_patrolPoint.y - npc.transform.position.y > jumpOnHeightDifference)
                        {
                            npc.m_character.Jump();
                        }
                    }
                    else
                    {
                        //TODO: fill out this logic
                        bool shouldDodge = false;
                        bool shouldStealthRoll = false;

                        if (npc.m_character.m_blocking && shouldDodge)
                        {
                            Vector3 dodgeDir = npc.m_character.m_moveDir;
                            if (dodgeDir.magnitude < 0.1f)
                            {
                                dodgeDir = -npc.m_character.m_lookDir;
                                dodgeDir.y = 0f;
                                dodgeDir.Normalize();
                            }
                            npc.Dodge(dodgeDir);
                            return;
                        }
                        if (npc.IsCrouching() && shouldStealthRoll)
                        {
                            Vector3 dodgeDir2 = npc.m_character.m_moveDir;
                            if (dodgeDir2.magnitude < 0.1f)
                            {
                                dodgeDir2 = npc.m_character.m_lookDir;
                                dodgeDir2.y = 0f;
                                dodgeDir2.Normalize();
                            }
                            npc.Dodge(dodgeDir2);
                            return;
                        }
                    }
                }
            }
        }
    }
}
