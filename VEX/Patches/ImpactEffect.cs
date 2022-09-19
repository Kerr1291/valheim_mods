using HarmonyLib;
using UnityEngine;
using System.Linq;
using System.Collections.Generic;
using HarmonyLib.Tools;

namespace VEX.Patches
{
    [HarmonyPatch(typeof(ImpactEffect), "OnCollisionEnter")]
    public static class ImpactEffect_OnCollisionEnter_Patch
    {
        private static bool Prefix(ref ImpactEffect __instance, Collision info)
        {
            ImpactEffect self = __instance;

            if (!self.m_nview.IsValid())
            {
                return false;
            }
            if (self.m_nview && !self.m_nview.IsOwner())
            {
                return false;
            }
            if (info.contacts.Length == 0)
            {
                return false;
            }
            if (!self.m_hitEffectEnabled)
            {
                return false;
            }
            if ((self.m_triggerMask.value & 1 << info.collider.gameObject.layer) == 0)
            {
                return false;
            }
            float magnitude = info.relativeVelocity.magnitude;
            if (magnitude < self.m_minVelocity)
            {
                return false;
            }
            ContactPoint contactPoint = info.contacts[0];
            Vector3 point = contactPoint.point;
            Vector3 pointVelocity = self.m_body.GetPointVelocity(point);
            self.m_hitEffectEnabled = false;
            self.Invoke("ResetHitTimer", self.m_interval);
            if (self.m_damages.HaveDamage())
            {
                GameObject gameObject = Projectile.FindHitObject(contactPoint.otherCollider);
                float num = Utils.LerpStep(self.m_minVelocity, self.m_maxVelocity, magnitude);
                IDestructible component = gameObject.GetComponent<IDestructible>();
                Fish fish = gameObject ? gameObject.GetComponent<Fish>() : null;
                if (component != null || fish != null)
                {
                    Character character = component as Character;
                    if (character)
                    {
                        if (!self.m_damagePlayers && character.IsPlayer())
                        {
                            return false;
                        }
                        float num2 = Vector3.Dot(-info.relativeVelocity.normalized, pointVelocity);
                        if (num2 < self.m_minVelocity)
                        {
                            return false;
                        }
                        ZLog.Log("Rel vel " + num2);
                        num = Utils.LerpStep(self.m_minVelocity, self.m_maxVelocity, num2);
                        if (character.GetSEMan().HaveStatusAttribute(StatusEffect.StatusAttribute.DoubleImpactDamage))
                        {
                            num *= 2f;
                        }
                    }

                    //if (!self.m_damageFish && gameObject.GetComponent<Fish>())
                    //{
                    //    return false;
                    //}

                    HitData hitData = new HitData();
                    hitData.m_point = point;
                    hitData.m_dir = pointVelocity.normalized;
                    hitData.m_hitCollider = info.collider;
                    hitData.m_toolTier = self.m_toolTier;
                    hitData.m_damage = self.m_damages.Clone();
                    hitData.m_damage.Modify(num);
                    
                    if (fish == null)
                    {
                        component.Damage(hitData);
                    }
                    else
                    {
                        KillFish(num, fish, hitData);
                    }
                }

                if (self.m_damageToSelf && fish == null)
                {
                    IDestructible component2 = self.GetComponent<IDestructible>();
                    if (component2 != null)
                    {
                        HitData hitData2 = new HitData();
                        hitData2.m_point = point;
                        hitData2.m_dir = -pointVelocity.normalized;
                        hitData2.m_toolTier = self.m_toolTier;
                        hitData2.m_damage = self.m_damages.Clone();
                        hitData2.m_damage.Modify(num);
                        component2.Damage(hitData2);
                    }
                }
            }
            Vector3 rhs = Vector3.Cross(-Vector3.Normalize(info.relativeVelocity), contactPoint.normal);
            Vector3 vector = Vector3.Cross(contactPoint.normal, rhs);
            Quaternion rot = Quaternion.identity;
            if (vector != Vector3.zero && contactPoint.normal != Vector3.zero)
            {
                rot = Quaternion.LookRotation(vector, contactPoint.normal);
            }
            self.m_hitEffect.Create(point, rot, null, 1f);
            if (self.m_firstHit && self.m_hitDestroyChance > 0f && UnityEngine.Random.value <= self.m_hitDestroyChance)
            {
                self.m_destroyEffect.Create(point, rot, null, 1f);
                GameObject gameObject2 = self.gameObject;
                if (self.transform.parent)
                {
                    Animator componentInParent = self.transform.GetComponentInParent<Animator>();
                    if (componentInParent)
                    {
                        gameObject2 = componentInParent.gameObject;
                    }
                }
                UnityEngine.Object.Destroy(gameObject2);
            }
            self.m_firstHit = false;
            return false;
        }

        private static void KillFish(float num, Fish fish, HitData hitData)
        {
            if (fish.GetComponent<Floating>() == null)
            {
                float waterLevel = fish.m_inWater;
                //with other patch changes, should cause fish to float above water
                var floating = fish.gameObject.AddComponent<Floating>();
                floating.m_inWater = waterLevel;
                floating.m_waterLevelOffset = 1.0f;
            }

            //try to blast the fish up and away
            Vector3 blastDir = hitData.m_dir;

            if (blastDir.y < 0)
                blastDir.y = -blastDir.y;

            if (blastDir.y < .25f)
                blastDir.y += .25f;

            fish.m_body.AddForce(num * 4f * blastDir.normalized, ForceMode.VelocityChange);
            fish.m_body.constraints = RigidbodyConstraints.None;
        }
    }
}
