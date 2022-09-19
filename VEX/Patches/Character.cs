using HarmonyLib;
using UnityEngine;
using System.Linq;
using System.Collections.Generic;
using HarmonyLib.Tools;

namespace VEX.Patches
{
    [HarmonyPatch(typeof(Character), "GetHoverName")]
    public static class Character_GetHoverName_Patch
    {
        private static bool Prefix(ref Character __instance, ref string __result)
        {
            if (!NPZ.MOD_ENABLED)
                return true;

            Character self = __instance;
            NPC npc = self.GetComponent<NPC>();
            if (npc != null)
            {
                __result = npc.GetHoverName();
                return false;
            }

            return true;
        }
    }

    [HarmonyPatch(typeof(Character), "IsTeleporting")]
    public static class Character_IsTeleporting_Patch
    {
        private static bool Prefix(ref Character __instance, ref bool __result)
        {
            if (!NPZ.MOD_ENABLED)
                return true;

            Character self = __instance;
            NPC npc = self.GetComponent<NPC>();
            if (npc != null)
            {
                __result = npc.IsTeleporting();
                return false;
            }

            return true;
        }
    }

    [HarmonyPatch(typeof(Character), "CanWallRun")]
    public static class Character_CanWallRun_Patch
    {
        private static bool Prefix(ref Character __instance, ref bool __result)
        {
            if (!NPZ.MOD_ENABLED)
                return true;

            Character self = __instance;
            NPC npc = self.GetComponent<NPC>();
            if (npc != null)
            {
                __result = true;
                return false;
            }

            return true;
        }
    }

    [HarmonyPatch(typeof(Character), "Start")]
    public static class Character_Start_Patch
    {
        private static void Postfix(ref Character __instance)
        {
            Character self = __instance;
            if (self.gameObject.name.Contains("Serpent"))
            {
                MonsterAI tempMonsterAI = self.gameObject.GetComponent<MonsterAI>();

                if (tempMonsterAI.m_consumeItems == null)
                    tempMonsterAI.m_consumeItems = new List<ItemDrop>();

                if (!tempMonsterAI.m_consumeItems.Any(x => x.name.Contains("Wood")))
                {
                    tempMonsterAI.m_consumeItems.Add(ObjectDB.instance.GetItemPrefab("Wood").GetComponent<ItemDrop>());
                }

                if(self.m_nview.IsValid())
                    tempMonsterAI.SetDespawnInDay(false);
            }

            if (self.gameObject.name.Contains("Leech"))
            {
                MonsterAI tempMonsterAI = self.gameObject.GetComponent<MonsterAI>();

                if (tempMonsterAI.m_consumeItems == null)
                    tempMonsterAI.m_consumeItems = new List<ItemDrop>();

                if (!tempMonsterAI.m_consumeItems.Any(x => x.name.Contains("RawMeat")))
                {
                    tempMonsterAI.m_consumeItems.Add(ObjectDB.instance.GetItemPrefab("RawMeat").GetComponent<ItemDrop>());
                }

                //tempMonsterAI.SetDespawnInDay(false);
            }

            if (self.gameObject.name.Contains("Blob"))
            {
                MonsterAI tempMonsterAI = self.gameObject.GetComponent<MonsterAI>();

                if (tempMonsterAI.m_consumeItems == null)
                    tempMonsterAI.m_consumeItems = new List<ItemDrop>();

                if (!tempMonsterAI.m_consumeItems.Any(x => x.name.Contains("BoneFragments")))
                {
                    tempMonsterAI.m_consumeItems.Add(ObjectDB.instance.GetItemPrefab("BoneFragments").GetComponent<ItemDrop>());
                }

                //tempMonsterAI.SetDespawnInDay(false);
            }

            if (self.gameObject.name.Contains("Hatchling"))
            {
                MonsterAI tempMonsterAI = self.gameObject.GetComponent<MonsterAI>();

                if (tempMonsterAI.m_consumeItems == null)
                    tempMonsterAI.m_consumeItems = new List<ItemDrop>();

                if (!tempMonsterAI.m_consumeItems.Any(x => x.name.Contains("TrophyWolf")))
                {
                    tempMonsterAI.m_consumeItems.Add(ObjectDB.instance.GetItemPrefab("TrophyWolf").GetComponent<ItemDrop>());
                }

                //tempMonsterAI.SetDespawnInDay(false);
            }

            if (self.gameObject.name.Contains("StoneGolem"))
            {
                MonsterAI tempMonsterAI = self.gameObject.GetComponent<MonsterAI>();

                if (tempMonsterAI.m_consumeItems == null)
                    tempMonsterAI.m_consumeItems = new List<ItemDrop>();

                if (!tempMonsterAI.m_consumeItems.Any(x => x.name.Contains("FlametalOre")))
                {
                    tempMonsterAI.m_consumeItems.Add(ObjectDB.instance.GetItemPrefab("FlametalOre").GetComponent<ItemDrop>());
                }

                //tempMonsterAI.SetDespawnInDay(false);
            }
        }
    }

    [HarmonyPatch(typeof(Character), "Jump")]
    public static class Character_Jump_Patch
    {
        static float defaultJumpStaminaUsage = 0f;

        static float GetJumpStaminaUsage(Character self)
        {
            if (defaultJumpStaminaUsage <= 0f)
            {
                defaultJumpStaminaUsage = self.m_jumpStaminaUsage;
            }

            Skills skills = self.GetSkills();
            float skillFactor = 0f;
            if (skills != null)
            {
                skillFactor = skills.GetSkillFactor(Skills.SkillType.Jump);
            }

            //reduce the walljump cost if your jump skill is high enough
            if (self.m_wallRunning && skillFactor > 0.25f)
            {
                return defaultJumpStaminaUsage * 0.25f;
            }

            return defaultJumpStaminaUsage;
        }

        static void RaiseJumpSkill(Character self)
        {
            Skills skills = self.GetSkills();
            if (skills != null)
            {
                if (self.m_lastGroundBody != null)
                {
                    Character other = self.m_lastGroundBody.GetComponent<Character>();
                    if (other != null)
                    {
                        if (other.IsMonsterFaction())
                        {
                            self.RaiseSkill(Skills.SkillType.Jump, 2f);
                        }
                    }
                }

                self.RaiseSkill(Skills.SkillType.Jump, 1f);
            }
        }

        private static void CheckForWallJumpAttack(Character self, Vector3 jumpVelocity)
        {
            if(!self.IsPlayer())
            {
                return;
            }

            if (!self.IsWallRunning())
                return;

            Skills skills = self.GetSkills();
            float skillFactor = 0f;
            if (skills != null)
            {
                skillFactor = skills.GetSkillFactor(Skills.SkillType.Jump);
            }

            //require to allow wall-jump attack
            if (skillFactor < .5f)
                return;

            if (self.m_lastGroundBody != null)
            {
                Character other = self.m_lastGroundBody.GetComponent<Character>();
                if (other != null)
                {
                    if (other.IsMonsterFaction() && !other.IsTamed())
                    {
                        HitData hitData = new HitData();
                        hitData.m_point = self.m_lastGroundPoint;
                        hitData.m_dir = -jumpVelocity.normalized;
                        hitData.m_hitCollider = self.m_lastGroundCollider;
                        hitData.m_toolTier = 0;
                        hitData.m_pushForce = jumpVelocity.magnitude;
                        hitData.m_damage.Add(new HitData.DamageTypes() { m_damage = jumpVelocity.magnitude },1);
                        hitData.SetAttacker(self);
                        hitData.m_dodgeable = false;
                        hitData.m_blockable = false;
                        other.Damage(hitData);
                    }
                }
            }

        }

        private static bool Prefix(ref Character __instance)
        {
            Character self = __instance;

            if (self.IsOnGround() && !self.IsDead() && !self.InAttack() && !self.IsEncumbered() && !self.InDodge() && !self.IsKnockedBack())
            {
                if (self.IsPlayer())
                {
                    self.m_jumpStaminaUsage = GetJumpStaminaUsage(self);
                }

                bool flag = false;
                if (!self.HaveStamina(self.m_jumpStaminaUsage))
                {
                    if (self.IsPlayer())
                    {
                        Hud.instance.StaminaBarNoStaminaFlash();
                    }
                    flag = true;
                }
                float skillFactor = CalculateJumpSkillFactor(self);
                if (!flag)
                    RaiseJumpSkill(self);
                Vector3 vector = CalculateJumpVelocity(self, flag, skillFactor);



                self.m_body.WakeUp();
                self.m_body.velocity = vector;
                self.ResetGroundContact();
                self.m_lastGroundTouch = 1f;
                self.m_jumpTimer = 0f;
                self.m_zanim.SetTrigger("jump");
                self.AddNoise(30f);
                self.m_jumpEffects.Create(self.transform.position, self.transform.rotation, self.transform, 1f);
                self.OnJump();
                self.SetCrouch(false);
                self.UpdateBodyFriction();
            }

            return false;
        }

        private static float CalculateJumpSkillFactor(Character self)
        {
            float skillFactor = 0f;
            Skills skills = self.GetSkills();
            if (skills != null)
            {
                skillFactor = skills.GetSkillFactor(Skills.SkillType.Jump);
            }
            return skillFactor;
        }

        private static Vector3 CalculateJumpVelocity(Character self, bool flag, float num)
        {
            Vector3 vector = self.m_body.velocity;
            Mathf.Acos(Mathf.Clamp01(self.m_lastGroundNormal.y));
            Vector3 normalized = (self.m_lastGroundNormal + Vector3.up).normalized;
            float num2 = 1f + num * 0.4f;
            float num3 = self.m_jumpForce * num2;
            float num4 = Vector3.Dot(normalized, vector);
            if (num4 < num3)
            {
                vector += normalized * (num3 - num4);
            }
            vector += self.m_moveDir * self.m_jumpForceForward * num2;
            if (flag)
            {
                vector *= self.m_jumpForceTiredFactor;
            }

            return vector;
        }
    }

    [HarmonyPatch(typeof(Character), "UpdateGroundContact")]
    public static class Character_UpdateGroundContact_Patch
    {
        static float GetMinFallDamageHeight(Character self)
        {
            if(self.IsPlayer())
            {

                //TODO: adjust for body weight/mass/armor
                //TODO: adjust for a different calculation for each tier 25/50/75/100
                return Mathf.Lerp(4f, 15f, self.GetSkillFactor(Skills.SkillType.Jump));
            }
            else
            {
                if (self.IsFlying())
                    return float.PositiveInfinity;

                if(self.IsMonsterFaction())
                {
                    return self.m_collider.bounds.size.y * 2f;
                }
            }

            return self.m_collider.bounds.size.y * 3f;
        }

        static float GetMaxFallDamageHeight(Character self)
        {
            if (self.IsPlayer())
            {
                float skillFactor = self.GetSkillFactor(Skills.SkillType.Jump);

                if(skillFactor > 0.95f)
                {
                    return Mathf.Lerp(20f, 100f, skillFactor);
                }
                else
                {
                    //TODO: adjust for a different calculation for each tier 25/50/75/100
                    return Mathf.Lerp(20f, 60f, skillFactor);
                }
            }
            else
            {
                if (self.IsMonsterFaction())
                {
                    return self.m_collider.bounds.size.y * 2f * 5f;
                }
            }

            return self.m_collider.bounds.size.y * 3f * 5f;
        }

        static float GetMaxFallDamage(Character self)
        {
            return 100f;
        }

        static float GetFallDamage(Character self, float fallHeight)
        {
            return Mathf.Clamp01((fallHeight - GetMinFallDamageHeight(self)) / (GetMaxFallDamageHeight(self) - GetMinFallDamageHeight(self))) * GetMaxFallDamage(self);
        }

        private static bool Prefix(ref Character __instance, float dt)
        {
            Character self = __instance;

            if (!self.m_groundContact)
            {
                return false;
            }
            self.m_lastGroundCollider = self.m_lowestContactCollider;
            self.m_lastGroundNormal = self.m_groundContactNormal;
            self.m_lastGroundPoint = self.m_groundContactPoint;
            self.m_lastGroundBody = (self.m_lastGroundCollider ? self.m_lastGroundCollider.attachedRigidbody : null);
            if (!self.IsPlayer() && self.m_lastGroundBody != null && self.m_lastGroundBody.gameObject.layer == self.gameObject.layer)
            {
                self.m_lastGroundCollider = null;
                self.m_lastGroundBody = null;
            }
            float num = Mathf.Max(0f, self.m_maxAirAltitude - self.transform.position.y);
            if (num > 0.8f)
            {
                if (self.m_onLand != null)
                {
                    Vector3 lastGroundPoint = self.m_lastGroundPoint;
                    if (self.InWater())
                    {
                        lastGroundPoint.y = self.m_waterLevel;
                    }
                    self.m_onLand(self.m_lastGroundPoint);
                }
                self.ResetCloth();
            }

            if (self.IsPlayer())
            {
                CalculateFallingImpactFromPlayer(self, num);
            }
            else if(num > GetMinFallDamageHeight(self))
            {
                HitData hitData = new HitData();
                hitData.m_damage.m_damage = GetFallDamage(self, num);
                hitData.m_point = self.m_lastGroundPoint;
                hitData.m_dir = self.m_lastGroundNormal;
                self.Damage(hitData);
            }

            self.ResetGroundContact();
            self.m_lastGroundTouch = 0f;
            self.m_maxAirAltitude = self.transform.position.y;

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

        private static void CalculateFallingImpactFromPlayer(Character self, float fallHeight)
        {
            Skills skills = self.GetSkills();
            float skillFactor = 0f;
            if (skills != null)
            {
                skillFactor = skills.GetSkillFactor(Skills.SkillType.Jump);
            }

            HitData hitData = new HitData();

            //required to allow stomp attack
            if (skillFactor < .5f)
            {
                if (fallHeight > GetMinFallDamageHeight(self))
                {
                    hitData.m_damage.m_damage = GetFallDamage(self, fallHeight);
                    hitData.m_point = self.m_lastGroundPoint;
                    hitData.m_dir = self.m_lastGroundNormal;
                    self.Damage(hitData);
                }
            }
            else
            {
                if (self.m_lastGroundBody != null)
                {
                    Character other = self.m_lastGroundBody.GetComponent<Character>();
                    if (other != null)
                    {
                        if (other.IsMonsterFaction() && !other.IsTamed())
                        {
                            if(fallHeight >= 2f && skillFactor >= 1f)
                            {
                                //regen the jump stam used when landing on an enemy (yes this allows stamina regen by just falling on enemies)
                                self.AddStamina(self.m_jumpStaminaUsage);
                            }

                            if (fallHeight >= 4f)
                            {
                                float fallDmg = Mathf.Clamp01((fallHeight - 4f) / (20f - 4f)) * GetMaxFallDamage(self);
                                hitData.m_damage.m_damage = fallDmg;
                                hitData.m_point = self.m_lastGroundPoint;
                                hitData.m_dir = -self.m_lastGroundNormal;
                                hitData.m_hitCollider = self.m_lastGroundCollider;
                                hitData.m_toolTier = 0;
                                hitData.m_pushForce = fallDmg;
                                hitData.SetAttacker(self);
                                hitData.m_dodgeable = false;
                                hitData.m_blockable = false;
                                other.Damage(hitData);
                                self.RaiseSkill(Skills.SkillType.Jump, 2f);
                                return;
                            }
                        }
                    }
                }

                if (skillFactor > .75f)
                {
                    bool landedNearEnemies = false;
                    //get nearby enemy to active slam
                    float fallExplosionRadius = 5f;
                    float fallDmg = Mathf.Clamp01((fallHeight - 4f) / (40f - 4f)) * GetMaxFallDamage(self);

                    if (fallHeight >= GetMinFallDamageHeight(self))
                    {
                        Collider[] array = Physics.OverlapSphere(self.m_lastGroundPoint, fallExplosionRadius, Attack.m_attackMaskTerrain, QueryTriggerInteraction.UseGlobal);
                        HashSet<GameObject> hashSet = new HashSet<GameObject>();
                        foreach (Collider collider in array)
                        {
                            GameObject gameObject = Projectile.FindHitObject(collider);

                            Character c = gameObject.GetComponent<Character>();
                            if (c != null)
                            {
                                if (!c.IsMonsterFaction())
                                    continue;
                                landedNearEnemies = true;
                            }

                            bool hitCharacter = false;
                            IDestructible component = gameObject.GetComponent<IDestructible>();

                            Fish fish = gameObject ? gameObject.GetComponent<Fish>() : null;
                            if ((component != null || fish != null)
                                && !hashSet.Contains(gameObject))
                            {
                                hashSet.Add(gameObject);
                                if (fish != null || IsValidTarget(self,component, ref hitCharacter))
                                {
                                    Vector3 vector = collider.ClosestPointOnBounds(self.m_lastGroundPoint);
                                    Vector3 vector2 = (Vector3.Distance(vector, self.m_lastGroundPoint) > 0.1f) ? (vector - self.m_lastGroundPoint) : Vector3.up;

                                    if(vector2.y < 0)
                                        vector2.y = -vector2.y;

                                    vector2.Normalize();

                                    if (fish == null)
                                    {
                                        hitData.m_hitCollider = collider;
                                        hitData.m_damage.m_damage = fallDmg;
                                        hitData.m_pushForce = fallDmg;
                                        hitData.m_point = vector;
                                        hitData.m_dir = vector2.normalized;
                                        hitData.m_dodgeable = true;
                                        hitData.m_blockable = true;
                                        hitData.SetAttacker(self);

                                        var otherChar = gameObject.GetComponent<Character>();
                                        if (otherChar != null)
                                        {
                                            if (BaseAI.IsEnemy(self, otherChar))
                                                gameObject.GetComponent<Rigidbody>().AddForce(hitData.m_pushForce * hitData.m_dir * 0.1f, ForceMode.Impulse);
                                        }
                                        component.Damage(hitData);
                                    }
                                    else
                                    {
                                        KillFish(fish, vector2);
                                    }
                                }
                            }
                        }
                    }

                    if (landedNearEnemies)
                    {
                        self.AddNoise(30);
                        self.m_animEvent.FreezeFrame(0.25f);
                        self.RaiseSkill(Skills.SkillType.Jump, 2f);
                        return;
                    }
                }

                //default fall dmg to you
                hitData.m_damage.m_damage = GetFallDamage(self, fallHeight);
                hitData.m_point = self.m_lastGroundPoint;
                hitData.m_dir = self.m_lastGroundNormal;
                self.Damage(hitData);
            }
        }

        private static bool IsValidTarget(Character self, IDestructible destr, ref bool hitCharacter)
        {
            Character character = destr as Character;
            if (character)
            {
                if (character == self)
                {
                    return false;
                }
                if (character.IsDodgeInvincible())
                {
                    return false;
                }
                hitCharacter = true;
            }
            return true;
        }
    }
}
