using HarmonyLib;
using UnityEngine;
using System.Linq;
using System.Collections.Generic;
using HarmonyLib.Tools;
using System.Reflection;

namespace VEX.Patches
{
    [HarmonyPatch(typeof(FootStep), "FindActiveFoot")]
    public static class FootStep_FindActiveFoot_Patch
    {
        private static void Prefix(ref FootStep __instance)
        {
            FootStep self = __instance;

            if (self.m_feet.Length > 0 && self.m_feet[0] == null)
            {
                Debug.Log("fixing foot");
                self.FindJoints();
            }
        }
    }

    [HarmonyPatch(typeof(FootStep), "OnFoot", typeof(Transform))]
    public static class FootStep_OnFoot_Patch
    {
        static float defaultPlayerRunSpeed = 0f;
        static float defaultPlayerAnimationSpeed = 0f;
        static float speedFactor = 1f;

        static float GetRunSpeed(FootStep self, FootStep.GroundMaterial groundMaterial)
        {
            //TODO: adjust for body weight/mass/armor
            //TODO: adjust for a different calculation for each tier 25/50/75/100
            speedFactor = 1f;// Mathf.Lerp(1f, 1.1f, self.m_character.GetSkillFactor(Skills.SkillType.Run));
            
            //obvious
            if (groundMaterial == FootStep.GroundMaterial.Wood)
            {
                speedFactor *= 1.3f;
            }
            else
            //yep
            if (groundMaterial == FootStep.GroundMaterial.Metal)
            {
                speedFactor *= 2f;
            }
            else
            //swamp
            if (groundMaterial == FootStep.GroundMaterial.Mud)
            {
                //default
            }
            else
            //mountain
            if (groundMaterial == FootStep.GroundMaterial.Snow)
            {
                speedFactor *= .95f;
            }
            else
            //it's stone.
            if (groundMaterial == FootStep.GroundMaterial.Stone)
            {
                speedFactor *= 1.4f;
            }
            else
            //almost everywhere else in the game
            if (groundMaterial == FootStep.GroundMaterial.GenericGround)
            {
                speedFactor *= 1.2f;
            }
            else
            //meadows, maybe black forest
            if (groundMaterial == FootStep.GroundMaterial.Grass)
            {
                //default
            }
            else
            //....
            if (groundMaterial == FootStep.GroundMaterial.Water)
            {
                speedFactor *= .85f;
            }

            return defaultPlayerRunSpeed * speedFactor;
        }

        static void ResetRunSpeed(FootStep self)
        {
            self.m_character.m_runSpeed = defaultPlayerRunSpeed;
            self.m_character.m_zanim.SetSpeed(defaultPlayerAnimationSpeed);
        }

        //TODO: do this for no stamina drain while running
        //check for in player base: EffectArea.IsPointInsideArea(spawnPoint, EffectArea.Type.PlayerBase, 0f)

        private static void Postfix(ref FootStep __instance, Transform foot)
        {
            FootStep self = __instance;

            if (!self.m_nview.IsValid())
            {
                return;
            }

            if (!self.m_character.IsPlayer())
                return;

            if (defaultPlayerRunSpeed <= 0f)
            {
                defaultPlayerRunSpeed = self.m_character.m_runSpeed;
                defaultPlayerAnimationSpeed = self.m_character.m_zanim.m_animator.speed;
            }

            Vector3 vector = (foot != null) ? foot.position : self.transform.position;
            FootStep.MotionType motionType = self.GetMotionType(self.m_character);
            FootStep.GroundMaterial groundMaterial = self.GetGroundMaterial(self.m_character, vector);

            //Debug.Log("motionType: " + motionType);
            //Debug.Log("groundMaterial: " + groundMaterial);

            if (motionType == FootStep.MotionType.Walk)
            {
                ResetRunSpeed(self);
            }
            else if(motionType == FootStep.MotionType.Jog)
            {
                ResetRunSpeed(self);
            }
            else if (motionType == FootStep.MotionType.Run)
            {
                self.m_character.m_runSpeed = GetRunSpeed(self, groundMaterial);
                self.m_character.m_zanim.SetSpeed(speedFactor);
            }
            else if (motionType == FootStep.MotionType.Swiming)
            {
                ResetRunSpeed(self);
            }
            else if (motionType == FootStep.MotionType.Sneak)
            {
                ResetRunSpeed(self);
            }
            else if (motionType == FootStep.MotionType.Land)
            {
                ResetRunSpeed(self);
            }
            else if (motionType == FootStep.MotionType.Climbing) //wall-running
            {
                self.m_character.m_runSpeed = GetRunSpeed(self, groundMaterial);
                self.m_character.m_zanim.SetSpeed(speedFactor);
            }

            //Debug.Log("self.m_character.m_runSpeed: " + self.m_character.m_runSpeed);
            //Debug.Log("self.m_character.m_zanim.m_animator.speed: " + self.m_character.m_zanim.m_animator.speed);

            //if (!this.m_nview.IsValid())
            //{
            //    return;
            //}
            //Vector3 vector = (foot != null) ? foot.position : base.transform.position;
            //FootStep.MotionType motionType = this.GetMotionType(this.m_character);
            //FootStep.GroundMaterial groundMaterial = this.GetGroundMaterial(this.m_character, vector);
            //int num = this.FindBestStepEffect(groundMaterial, motionType);
            //if (num != -1)
            //{
            //    this.m_nview.InvokeRPC(ZNetView.Everybody, "Step", new object[]
            //    {
            //    num,
            //    vector
            //    });
            //}
        }
    }
}
