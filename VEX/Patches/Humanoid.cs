using HarmonyLib;
using UnityEngine;
using System.Linq;
using System.Collections.Generic;
using HarmonyLib.Tools;

namespace VEX.Patches
{
    [HarmonyPatch(typeof(Character), "IsDodgeInvincible")]
    public static class Character_IsDodgeInvincible_Patch
    {
        private static void Postfix(ref Character __instance, ref bool __result)
        {
            if (!NPZ.MOD_ENABLED)
                return;

            if (__result)
                return;

            Character self = __instance;
            NPC npc = self.GetComponent<NPC>();
            if (npc != null)
            {
                __result = npc.IsDodgeInvincible();
                return;
            }
        }
    }

    [HarmonyPatch(typeof(Character), "InEmote")]
    public static class Character_InEmote_Patch
    {
        private static void Postfix(ref Character __instance, ref bool __result)
        {
            if (!NPZ.MOD_ENABLED)
                return;

            if (__result)
                return;

            Character self = __instance;
            NPC npc = self.GetComponent<NPC>();
            if (npc != null)
            {
                __result = npc.InEmote();
                return;
            }
        }
    }

    [HarmonyPatch(typeof(Character), "InDodge")]
    public static class Character_InDodge_Patch
    {
        private static void Postfix(ref Humanoid __instance, ref bool __result)
        {
            if (!NPZ.MOD_ENABLED)
                return;

            if (__result)
                return;

            Character self = __instance;
            NPC npc = self.GetComponent<NPC>();
            if (npc != null)
            {
                __result = npc.InDodge();
                return;
            }
        }
    }

    [HarmonyPatch(typeof(Humanoid), "BlockAttack")]
    public static class Humanoid_BlockAttack_Patch
    {
        public static float defaultSkillIncreaseFromParry = 2f;
        public static float blockingSkillIncreaseFromParry = 5f;

        static float GetBlockingSkillIncreaseFromParry()
        {
            //default was 2f
            return blockingSkillIncreaseFromParry;
        }

        private static bool Prefix(ref Humanoid __instance, HitData hit, Character attacker, ref bool __result)
        {
            Humanoid self = __instance;

            if (Vector3.Dot(hit.m_dir, self.transform.forward) > 0f)
            {
                __result = false;
                return false;
            }
            ItemDrop.ItemData currentBlocker = self.GetCurrentBlocker();
            if (currentBlocker == null)
            {
                __result = false;
                return false;
            }
            bool flag = currentBlocker.m_shared.m_timedBlockBonus > 1f && self.m_blockTimer != -1f && self.m_blockTimer < 0.25f;
            float skillFactor = self.GetSkillFactor(Skills.SkillType.Blocking);
            float num = currentBlocker.GetBlockPower(skillFactor);
            if (flag)
            {
                num *= currentBlocker.m_shared.m_timedBlockBonus;
            }
            float totalBlockableDamage = hit.GetTotalBlockableDamage();
            float num2 = Mathf.Min(totalBlockableDamage, num);
            float num3 = Mathf.Clamp01(num2 / num);
            float stamina = self.m_blockStaminaDrain * num3;
            self.UseStamina(stamina);
            bool flag2 = self.HaveStamina(0f);
            bool flag3 = flag2 && num >= totalBlockableDamage;
            if (flag2)
            {
                hit.m_statusEffect = "";
                hit.BlockDamage(num2);
                DamageText.instance.ShowText(DamageText.TextType.Blocked, hit.m_point + Vector3.up * 0.5f, num2, false);
            }
            if (!flag2 || !flag3)
            {
                self.Stagger(hit.m_dir);
            }
            if (currentBlocker.m_shared.m_useDurability)
            {
                float num4 = currentBlocker.m_shared.m_useDurabilityDrain * num3;
                currentBlocker.m_durability -= num4;
            }
            self.RaiseSkill(Skills.SkillType.Blocking, flag ? GetBlockingSkillIncreaseFromParry() : 1f);
            currentBlocker.m_shared.m_blockEffect.Create(hit.m_point, Quaternion.identity, null, 1f);
            if (attacker && flag && flag3)
            {
                self.m_perfectBlockEffect.Create(hit.m_point, Quaternion.identity, null, 1f);
                if (attacker.m_staggerWhenBlocked)
                {
                    attacker.Stagger(-hit.m_dir);
                }
            }
            if (flag3)
            {
                float num5 = Mathf.Clamp01(num3 * 0.5f);
                hit.m_pushForce *= num5;
                if (attacker && flag)
                {
                    HitData hitData = new HitData();
                    hitData.m_pushForce = currentBlocker.GetDeflectionForce() * (1f - num5);
                    hitData.m_dir = attacker.transform.position - self.transform.position;
                    hitData.m_dir.y = 0f;
                    hitData.m_dir.Normalize();
                    attacker.Damage(hitData);
                }
            }

            __result = true;
            return false;
        }
    }
}
