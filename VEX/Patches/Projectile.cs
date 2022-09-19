using HarmonyLib;
using UnityEngine;
using System.Linq;
using System.Collections.Generic;
using HarmonyLib.Tools;

namespace VEX.Patches
{
    [HarmonyPatch(typeof(Projectile), "DoAOE")]
    public static class Projectile_DoAOE_Patch
    {
        private static bool Prefix(ref Projectile __instance, Vector3 hitPoint, ref bool hitCharacter, ref bool didDamage)
        {
            Projectile self = __instance;

            Collider[] array = Physics.OverlapSphere(hitPoint, self.m_aoe, Projectile.m_rayMaskSolids, QueryTriggerInteraction.UseGlobal);
            HashSet<GameObject> hashSet = new HashSet<GameObject>();
            foreach (Collider collider in array)
            {
                GameObject gameObject = Projectile.FindHitObject(collider);
                IDestructible component = gameObject.GetComponent<IDestructible>();

                Fish fish = gameObject ? gameObject.GetComponent<Fish>() : null;
                if ((component != null || fish != null)
                    && !hashSet.Contains(gameObject))
                {
                    hashSet.Add(gameObject);
                    if (fish != null || self.IsValidTarget(component, ref hitCharacter))
                    {
                        Vector3 vector = collider.ClosestPointOnBounds(hitPoint);
                        Vector3 vector2 = (Vector3.Distance(vector, hitPoint) > 0.1f) ? (vector - hitPoint) : self.m_vel;
                        vector2.y = 0f;
                        vector2.Normalize();
                        HitData hitData = new HitData();
                        hitData.m_hitCollider = collider;
                        hitData.m_damage = self.m_damage;
                        hitData.m_pushForce = self.m_attackForce;
                        hitData.m_backstabBonus = self.m_backstabBonus;
                        hitData.m_point = vector;
                        hitData.m_dir = vector2.normalized;
                        hitData.m_statusEffect = self.m_statusEffect;
                        hitData.m_dodgeable = self.m_dodgeable;
                        hitData.m_blockable = self.m_blockable;
                        hitData.m_skill = self.m_skill;
                        hitData.SetAttacker(self.m_owner);

                        if (fish == null)
                        {
                            component.Damage(hitData);
                        }
                        else
                        {
                            KillFish(fish, vector2);
                        }

                        didDamage = true;
                    }
                }
            }

            return false;
        }

        private static void KillFish(Fish fish, Vector3 vector2)
        {
            if (fish.GetComponent<Floating>() == null)
            {
                float waterLevel = fish.m_inWater;
                //with other patch changes, should cause fish to float above water
                var floating = fish.gameObject.AddComponent<Floating>();
                floating.m_inWater = waterLevel;
                floating.m_waterLevelOffset = 1.0f;
            }

            Vector3 normalized = Vector3.up + vector2.normalized;
            Vector3 b = Vector3.Project(vector2.normalized, normalized.normalized);
            Vector3 a = normalized.normalized * 2f - b;
            fish.m_body.AddForce(a * 8f, ForceMode.VelocityChange);
            fish.m_body.constraints = RigidbodyConstraints.None;
        }
    }

    [HarmonyPatch(typeof(Projectile), "OnHit")]
    public static class Projectile_OnHit_Patch
    {
        private static bool Prefix(ref Projectile __instance, Collider collider, Vector3 hitPoint, bool water)
        {
            Projectile self = __instance;

            GameObject gameObject = collider ? Projectile.FindHitObject(collider) : null;
            bool flag = false;
            bool flag2 = false;
            if (self.m_aoe > 0f)
            {
                self.DoAOE(hitPoint, ref flag2, ref flag);
            }
            else
            {
                IDestructible destructible = gameObject ? gameObject.GetComponent<IDestructible>() : null;
                Fish fish = gameObject ? gameObject.GetComponent<Fish>() : null;
                if (destructible != null || fish != null)
                {
                    if (fish == null)
                    {
                        if (!self.IsValidTarget(destructible, ref flag2))
                        {
                            return false;
                        }
                    }

                    HitData hitData = new HitData();
                    hitData.m_hitCollider = collider;
                    hitData.m_damage = self.m_damage;
                    hitData.m_pushForce = self.m_attackForce;
                    hitData.m_backstabBonus = self.m_backstabBonus;
                    hitData.m_point = hitPoint;
                    hitData.m_dir = self.transform.forward;
                    hitData.m_statusEffect = self.m_statusEffect;
                    hitData.m_dodgeable = self.m_dodgeable;
                    hitData.m_blockable = self.m_blockable;
                    hitData.m_skill = self.m_skill;
                    hitData.SetAttacker(self.m_owner);

                    if(fish == null)
                    {
                        destructible.Damage(hitData);
                    }
                    else
                    {
                        KillFish(self, fish);

                        //VEX.Logger.LogInfo("hitfish\n");

                        //Utils.Pull(fish.m_body, self.m_owner.transform.position, 1f, 6f, 30f, 0.1f);
                    }

                    flag = true;
                }
            }
            if (water)
            {
                self.m_hitWaterEffects.Create(hitPoint, Quaternion.identity, null, 1f);
            }
            else
            {
                self.m_hitEffects.Create(hitPoint, Quaternion.identity, null, 1f);
            }
            if (self.m_spawnOnHit != null || self.m_spawnItem != null)
            {
                self.SpawnOnHit(gameObject, collider);
            }
            if (self.m_hitNoise > 0f)
            {
                BaseAI.DoProjectileHitNoise(self.transform.position, self.m_hitNoise, self.m_owner);
            }
            if (self.m_owner != null && flag && self.m_owner.IsPlayer())
            {
                (self.m_owner as Player).RaiseSkill(self.m_skill, flag2 ? 1f : 0.5f);
            }
            self.m_didHit = true;
            self.transform.position = hitPoint;
            self.m_nview.InvokeRPC("OnHit", System.Array.Empty<object>());
            if (!self.m_stayAfterHitStatic)
            {
                ZNetScene.instance.Destroy(self.gameObject);
                return false;
            }
            if (collider && collider.attachedRigidbody != null)
            {
                self.m_ttl = Mathf.Min(1f, self.m_ttl);
            }

            return false;
        }

        private static void KillFish(Projectile self, Fish fish)
        {
            if (fish.GetComponent<Floating>() == null)
            {
                float waterLevel = fish.m_inWater;
                //with other patch changes, should cause fish to float above water
                var floating = fish.gameObject.AddComponent<Floating>();
                floating.m_inWater = waterLevel;
                floating.m_waterLevelOffset = 1.0f;
            }

            Vector3 normalized = Vector3.up + self.transform.forward;
            Vector3 b = Vector3.Project(self.transform.forward, normalized.normalized);
            Vector3 a = normalized.normalized * 2f - b;
            fish.m_body.AddForce(a * 8f, ForceMode.VelocityChange);
            fish.m_body.constraints = RigidbodyConstraints.None;
        }
    }
}
