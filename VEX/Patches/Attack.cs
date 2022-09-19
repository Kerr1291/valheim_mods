using HarmonyLib;
using UnityEngine;
using System.Linq;
using System.Collections.Generic;
using HarmonyLib.Tools;

namespace VEX.Patches
{
    [HarmonyPatch(typeof(Attack), "DoAreaAttack")]
    public static class Attack_DoAreaAttack_Patch
    {
        public static bool canCleaveTerrain = false;

        private static void KillFish(Fish fish, HitData hitData)
        {
            if (fish.GetComponent<Floating>() == null)
            {
                float waterLevel = fish.m_inWater;
                //with other patch changes, should cause fish to float above water
                var floating = fish.gameObject.AddComponent<Floating>();
                floating.m_inWater = waterLevel;
                floating.m_waterLevelOffset = 1.0f;
            }

            fish.m_body.AddForce(hitData.m_pushForce * 1f * hitData.m_dir, ForceMode.VelocityChange);
            fish.m_body.constraints = RigidbodyConstraints.None;
        }

        private static Vector3 GetPointOnCircle(float distance, float angle)
        {
            return new Vector3(Mathf.Sin(angle) * distance, 0f, Mathf.Cos(angle) * distance);
        }

        private static bool Prefix(ref Attack __instance)
        {
            Attack self = __instance;

            Transform transform = self.m_character.transform;
            Transform attackOrigin = self.GetAttackOrigin();
            Vector3 vector = attackOrigin.position + Vector3.up * self.m_attackHeight + transform.forward * self.m_attackRange + transform.right * self.m_attackOffset;
            self.m_weapon.m_shared.m_triggerEffect.Create(vector, transform.rotation, attackOrigin, 1f);
            self.m_triggerEffect.Create(vector, transform.rotation, attackOrigin, 1f);
            Vector3 vector2 = vector - transform.position;
            vector2.y = 0f;
            vector2.Normalize();
            int num = 0;
            Vector3 vector3 = Vector3.zero;
            bool flag = false;
            bool flag2 = false;
            float randomSkillFactor = self.m_character.GetRandomSkillFactor(self.m_weapon.m_shared.m_skillType);
            int layerMask = self.m_hitTerrain ? Attack.m_attackMaskTerrain : Attack.m_attackMask;
            Collider[] array = Physics.OverlapSphere(vector, self.m_attackRayWidth, layerMask, QueryTriggerInteraction.UseGlobal);
            HashSet<GameObject> hashSet = new HashSet<GameObject>();
            foreach (Collider collider in array)
            {
                bool hitHeightmap = false;
                bool isHeightmap = collider.GetComponent<Heightmap>() != null;
                if (!(collider.gameObject == self.m_character.gameObject))
                {
                    GameObject gameObject = Projectile.FindHitObject(collider);

                    bool hashHasGO = hashSet.Contains(gameObject);
                    bool canHitObject = !hashHasGO;

                    if (!(gameObject == self.m_character.gameObject) && canHitObject)
                    {
                        if(!hashHasGO)
                            hashSet.Add(gameObject);

                        Vector3 vector4;
                        if (collider is MeshCollider)
                        {
                            vector4 = collider.ClosestPointOnBounds(vector);
                        }
                        else
                        {
                            vector4 = collider.ClosestPoint(vector);
                        }
                        IDestructible component = gameObject.GetComponent<IDestructible>();
                        Fish fish = gameObject ? gameObject.GetComponent<Fish>() : null;
                        if (component != null || fish != null)
                        {
                            Vector3 vector5 = vector4 - vector;
                            vector5.y = 0f;
                            float num2 = Vector3.Dot(vector2, vector5);
                            if (num2 < 0f)
                            {
                                vector5 += vector2 * -num2;
                            }
                            vector5.Normalize();
                            HitData hitData = new HitData();
                            hitData.m_toolTier = self.m_weapon.m_shared.m_toolTier;
                            hitData.m_statusEffect = (self.m_weapon.m_shared.m_attackStatusEffect ? self.m_weapon.m_shared.m_attackStatusEffect.name : "");
                            hitData.m_pushForce = self.m_weapon.m_shared.m_attackForce * randomSkillFactor * self.m_forceMultiplier;
                            hitData.m_backstabBonus = self.m_weapon.m_shared.m_backstabBonus;
                            hitData.m_staggerMultiplier = self.m_staggerMultiplier;
                            hitData.m_dodgeable = self.m_weapon.m_shared.m_dodgeable;
                            hitData.m_blockable = self.m_weapon.m_shared.m_blockable;
                            hitData.m_skill = self.m_weapon.m_shared.m_skillType;
                            hitData.m_damage.Add(self.m_weapon.GetDamage(), 1);
                            hitData.m_point = vector4;
                            hitData.m_dir = vector5;
                            hitData.m_hitCollider = collider;
                            hitData.SetAttacker(self.m_character);
                            hitData.m_damage.Modify(self.m_damageMultiplier);
                            hitData.m_damage.Modify(randomSkillFactor);
                            hitData.m_damage.Modify(self.GetLevelDamageFactor());
                            if (self.m_attackChainLevels > 1 && self.m_currentAttackCainLevel == self.m_attackChainLevels - 1 && self.m_lastChainDamageMultiplier > 1f)
                            {
                                hitData.m_damage.Modify(self.m_lastChainDamageMultiplier);
                                hitData.m_pushForce *= 1.2f;
                            }
                            self.m_character.GetSEMan().ModifyAttack(self.m_weapon.m_shared.m_skillType, ref hitData);
                            Character character = component as Character;
                            if (character)
                            {
                                if ((!self.m_character.IsPlayer() && !BaseAI.IsEnemy(self.m_character, character)) || (hitData.m_dodgeable && character.IsDodgeInvincible()))
                                {
                                    goto IL_407;
                                }
                                flag2 = true;
                            }
                            if (fish == null)
                            {
                                component.Damage(hitData);
                            }
                            else
                            {
                                flag2 = true;
                                KillFish(fish, hitData);
                            }
                            flag = true;
                        }
                        num++;
                        vector3 += vector4;
                    }
                }

                if (canCleaveTerrain)
                {
                    if (isHeightmap && !hitHeightmap)
                    {
                        hitHeightmap = true;

                        //spawn on initial hit
                        self.m_weapon.m_shared.m_hitTerrainEffect.Create(vector, Quaternion.identity, null, 1f);
                        self.m_hitTerrainEffect.Create(vector, Quaternion.identity, null, 1f);
                        if (self.m_weapon.m_shared.m_spawnOnHitTerrain)
                        {
                            self.SpawnOnHitTerrain(vector, self.m_weapon.m_shared.m_spawnOnHitTerrain);
                        }

                        float aoeSize = self.m_attackRayWidth;

                        for (float r = 0f; r < aoeSize; r += 1f)
                        {
                            for (float theta = 0f; theta < 360f; theta += 360f / 8f)
                            {
                                var point = vector + GetPointOnCircle(r, theta);

                                //spawn on initial hit
                                self.m_weapon.m_shared.m_hitTerrainEffect.Create(point, Quaternion.identity, null, 1f);
                                self.m_hitTerrainEffect.Create(point, Quaternion.identity, null, 1f);
                                if (self.m_weapon.m_shared.m_spawnOnHitTerrain)
                                {
                                    self.SpawnOnHitTerrain(point, self.m_weapon.m_shared.m_spawnOnHitTerrain);
                                }
                            }
                        }
                    }
                }
IL_407:;
            }
            if (num > 0)
            {
                vector3 /= (float)num;
                self.m_weapon.m_shared.m_hitEffect.Create(vector3, Quaternion.identity, null, 1f);
                self.m_hitEffect.Create(vector3, Quaternion.identity, null, 1f);
                if (self.m_weapon.m_shared.m_useDurability && self.m_character.IsPlayer())
                {
                    self.m_weapon.m_durability -= 1f;
                }
                self.m_character.AddNoise(self.m_attackHitNoise);
                if (flag)
                {
                    self.m_character.RaiseSkill(self.m_weapon.m_shared.m_skillType, flag2 ? 1.5f : 1f);
                }
            }
            if (self.m_spawnOnTrigger)
            {
                IProjectile component2 = UnityEngine.Object.Instantiate<GameObject>(self.m_spawnOnTrigger, vector, Quaternion.identity).GetComponent<IProjectile>();
                if (component2 != null)
                {
                    component2.Setup(self.m_character, self.m_character.transform.forward, -1f, null, null);
                }
            }
            return false;
        }
    }


    [HarmonyPatch(typeof(Attack), "DoMeleeAttack")]
    public static class Attack_DoMeleeAttack_Patch
    {
        private static void KillFish(Fish fish, HitData hitData)
        {
            if (fish.GetComponent<Floating>() == null)
            {
                float waterLevel = fish.m_inWater;
                //with other patch changes, should cause fish to float above water
                var floating = fish.gameObject.AddComponent<Floating>();
                floating.m_inWater = waterLevel;
                floating.m_waterLevelOffset = 1.0f;
            }

            fish.m_body.AddForce(hitData.m_pushForce * 2f * hitData.m_dir, ForceMode.VelocityChange);
            fish.m_body.constraints = RigidbodyConstraints.None;
        }

        static float GetUnarmedSkillPushBonus(float unarmedSkillFactor)
        {
            return Mathf.Lerp(1f, 5f, unarmedSkillFactor);
        }

        static Vector3 GetUnarmedSkillPushDir(Vector3 hitDir)
        {
            return (hitDir + Vector3.up * .25f).normalized;
        }

        private static void ApplyUnarmedSkillToHitData(Attack self, GameObject go, IDestructible component3, Skills.SkillType skillType, HitData hitData)
        {
            if (Skills.SkillType.Unarmed == skillType) // && self.m_attackChainLevels <= 1)
            {
                float unarmedFactor = self.m_character.GetSkillFactor(Skills.SkillType.Unarmed);
                hitData.m_pushForce *= GetUnarmedSkillPushBonus(unarmedFactor);
                hitData.m_dir = GetUnarmedSkillPushDir(hitData.m_dir);

                if (go.GetComponent<Rigidbody>() != null)
                {
                    //allow punching trees etc when unarmed >= 50
                    if (!(component3 is Character) || unarmedFactor >= .5f)
                        go.GetComponent<Rigidbody>().AddForce(hitData.m_pushForce * hitData.m_dir, ForceMode.Impulse);
                }
                //fish.m_body.AddForce(hitData.m_pushForce * hitData.m_dir, ForceMode.VelocityChange);
                //Debug.Log("hitData.m_statusEffect "+ hitData.m_statusEffect);
                //Debug.Log("hitData.m_pushForce " + hitData.m_pushForce);
                //Debug.Log("hitData.m_skill " + hitData.m_skill);
                //Debug.Log("self.m_specialHitSkill " + self.m_specialHitSkill);
                //Debug.Log("self.m_currentAttackCainLevel " + self.m_currentAttackCainLevel);
                //Debug.Log("self.m_attackChainLevels " + self.m_attackChainLevels);
                //Debug.Log("hitData.m_toolTier " + hitData.m_toolTier);
            }
        }

        private static bool Prefix(ref Attack __instance)
        {
            Attack self = __instance;
            Transform transform;
            Vector3 vector;
            self.GetMeleeAttackDir(out transform, out vector);
            Vector3 point = self.m_character.transform.InverseTransformDirection(vector);
            Quaternion quaternion = Quaternion.LookRotation(vector, Vector3.up);
            self.m_weapon.m_shared.m_triggerEffect.Create(transform.position, quaternion, transform, 1f);
            self.m_triggerEffect.Create(transform.position, quaternion, transform, 1f);
            Vector3 vector2 = transform.position + Vector3.up * self.m_attackHeight + self.m_character.transform.right * self.m_attackOffset;
            float num = self.m_attackAngle / 2f;
            float num2 = 4f;
            float attackRange = self.m_attackRange;
            List<Attack.HitPoint> list = new List<Attack.HitPoint>();
            HashSet<Skills.SkillType> hashSet = new HashSet<Skills.SkillType>();
            int layerMask = self.m_hitTerrain ? Attack.m_attackMaskTerrain : Attack.m_attackMask;
            for (float num3 = -num; num3 <= num; num3 += num2)
            {
                Quaternion rotation = Quaternion.identity;
                if (self.m_attackType == Attack.AttackType.Horizontal)
                {
                    rotation = Quaternion.Euler(0f, -num3, 0f);
                }
                else if (self.m_attackType == Attack.AttackType.Vertical)
                {
                    rotation = Quaternion.Euler(num3, 0f, 0f);
                }
                Vector3 vector3 = self.m_character.transform.TransformDirection(rotation * point);
                Debug.DrawLine(vector2, vector2 + vector3 * attackRange);
                RaycastHit[] array;
                if (self.m_attackRayWidth > 0f)
                {
                    array = Physics.SphereCastAll(vector2, self.m_attackRayWidth, vector3, Mathf.Max(0f, attackRange - self.m_attackRayWidth), layerMask, QueryTriggerInteraction.Ignore);
                }
                else
                {
                    array = Physics.RaycastAll(vector2, vector3, attackRange, layerMask, QueryTriggerInteraction.Ignore);
                }
                System.Array.Sort<RaycastHit>(array, (RaycastHit x, RaycastHit y) => x.distance.CompareTo(y.distance));
                foreach (RaycastHit raycastHit in array)
                {
                    if (!(raycastHit.collider.gameObject == self.m_character.gameObject))
                    {
                        Vector3 vector4 = raycastHit.point;
                        if (raycastHit.distance < 1.401298E-45f)
                        {
                            if (raycastHit.collider is MeshCollider)
                            {
                                vector4 = vector2 + vector3 * attackRange;
                            }
                            else
                            {
                                vector4 = raycastHit.collider.ClosestPoint(vector2);
                            }
                        }
                        if (self.m_attackAngle >= 180f || Vector3.Dot(vector4 - vector2, vector) > 0f)
                        {
                            GameObject gameObject = Projectile.FindHitObject(raycastHit.collider);
                            if (!(gameObject == self.m_character.gameObject))
                            {
                                Vagon component = gameObject.GetComponent<Vagon>();
                                if (!component || !component.IsAttached(self.m_character))
                                {
                                    Character component2 = gameObject.GetComponent<Character>();
                                    if (!(component2 != null) || ((self.m_character.IsPlayer() || BaseAI.IsEnemy(self.m_character, component2)) && (!self.m_weapon.m_shared.m_dodgeable || !component2.IsDodgeInvincible())))
                                    {
                                        self.AddHitPoint(list, gameObject, raycastHit.collider, vector4, raycastHit.distance);
                                        if (!self.m_hitThroughWalls)
                                        {
                                            break;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            int num4 = 0;
            Vector3 vector5 = Vector3.zero;
            bool flag = false;
            bool flag2 = false;
            foreach (Attack.HitPoint hitPoint in list)
            {
                GameObject go = hitPoint.go;
                Vector3 vector6 = hitPoint.avgPoint / (float)hitPoint.count;
                Vector3 vector7 = vector6;
                switch (self.m_hitPointtype)
                {
                    case Attack.HitPointType.Closest:
                        vector7 = hitPoint.closestPoint;
                        break;
                    case Attack.HitPointType.Average:
                        vector7 = vector6;
                        break;
                    case Attack.HitPointType.First:
                        vector7 = hitPoint.firstPoint;
                        break;
                }
                num4++;
                vector5 += vector6;
                self.m_weapon.m_shared.m_hitEffect.Create(vector7, Quaternion.identity, null, 1f);
                self.m_hitEffect.Create(vector7, Quaternion.identity, null, 1f);
                IDestructible component3 = go.GetComponent<IDestructible>();
                Fish fish = go ? go.GetComponent<Fish>() : null;
                if (component3 != null || fish != null)
                {
                    DestructibleType destructibleType = fish == null ? component3.GetDestructibleType() : DestructibleType.Default;
                    Skills.SkillType skillType = self.m_weapon.m_shared.m_skillType;
                    if (self.m_specialHitSkill != Skills.SkillType.None && (destructibleType & self.m_specialHitType) != DestructibleType.None)
                    {
                        skillType = self.m_specialHitSkill;
                    }

                    float num5 = self.m_character.GetRandomSkillFactor(skillType);
                    if (self.m_lowerDamagePerHit && list.Count > 1)
                    {
                        num5 /= (float)list.Count * 0.75f;
                    }
                    HitData hitData = new HitData();
                    hitData.m_toolTier = self.m_weapon.m_shared.m_toolTier;
                    hitData.m_statusEffect = (self.m_weapon.m_shared.m_attackStatusEffect ? self.m_weapon.m_shared.m_attackStatusEffect.name : "");
                    hitData.m_pushForce = self.m_weapon.m_shared.m_attackForce * num5 * self.m_forceMultiplier;
                    hitData.m_backstabBonus = self.m_weapon.m_shared.m_backstabBonus;
                    hitData.m_staggerMultiplier = self.m_staggerMultiplier;
                    hitData.m_dodgeable = self.m_weapon.m_shared.m_dodgeable;
                    hitData.m_blockable = self.m_weapon.m_shared.m_blockable;
                    hitData.m_skill = skillType;
                    hitData.m_damage = self.m_weapon.GetDamage();
                    hitData.m_point = vector7;
                    hitData.m_dir = (vector7 - vector2).normalized;
                    hitData.m_hitCollider = hitPoint.collider;
                    hitData.SetAttacker(self.m_character);
                    hitData.m_damage.Modify(self.m_damageMultiplier);
                    hitData.m_damage.Modify(num5);
                    hitData.m_damage.Modify(self.GetLevelDamageFactor());
                    if (self.m_attackChainLevels > 1 && self.m_currentAttackCainLevel == self.m_attackChainLevels - 1)
                    {
                        hitData.m_damage.Modify(2f);
                        hitData.m_pushForce *= 1.2f;
                    }

                    ApplyUnarmedSkillToHitData(self, go, component3, skillType, hitData);

                    self.m_character.GetSEMan().ModifyAttack(skillType, ref hitData);
                    if (component3 is Character)
                    {
                        flag2 = true;
                    }
                    if (fish == null)
                    {
                        component3.Damage(hitData);
                    }
                    else
                    {
                        KillFish(fish, hitData);
                    }
                    if ((destructibleType & self.m_resetChainIfHit) != DestructibleType.None)
                    {
                        self.m_nextAttackChainLevel = 0;
                    }
                    hashSet.Add(skillType);
                    if (!self.m_multiHit)
                    {
                        break;
                    }
                }
                if (go.GetComponent<Heightmap>() != null && !flag)
                {
                    flag = true;
                    self.m_weapon.m_shared.m_hitTerrainEffect.Create(vector6, quaternion, null, 1f);
                    self.m_hitTerrainEffect.Create(vector6, quaternion, null, 1f);
                    if (self.m_weapon.m_shared.m_spawnOnHitTerrain)
                    {
                        self.SpawnOnHitTerrain(vector6, self.m_weapon.m_shared.m_spawnOnHitTerrain);
                    }
                    if (!self.m_multiHit)
                    {
                        break;
                    }
                }
            }
            if (num4 > 0)
            {
                vector5 /= (float)num4;
                if (self.m_weapon.m_shared.m_useDurability && self.m_character.IsPlayer())
                {
                    self.m_weapon.m_durability -= self.m_weapon.m_shared.m_useDurabilityDrain;
                }
                self.m_character.AddNoise(self.m_attackHitNoise);
                self.m_animEvent.FreezeFrame(0.15f);
                if (self.m_weapon.m_shared.m_spawnOnHit)
                {
                    IProjectile component4 = UnityEngine.Object.Instantiate<GameObject>(self.m_weapon.m_shared.m_spawnOnHit, vector5, quaternion).GetComponent<IProjectile>();
                    if (component4 != null)
                    {
                        component4.Setup(self.m_character, Vector3.zero, self.m_attackHitNoise, null, self.m_weapon);
                    }
                }
                foreach (Skills.SkillType skill in hashSet)
                {
                    self.m_character.RaiseSkill(skill, flag2 ? 1.5f : 1f);
                }
            }
            if (self.m_spawnOnTrigger)
            {
                IProjectile component5 = UnityEngine.Object.Instantiate<GameObject>(self.m_spawnOnTrigger, vector2, Quaternion.identity).GetComponent<IProjectile>();
                if (component5 != null)
                {
                    component5.Setup(self.m_character, self.m_character.transform.forward, -1f, null, self.m_weapon);
                }
            }

            return false;
        }
    }
}
