using HarmonyLib;
using UnityEngine;
using System.Linq;
using System.Collections.Generic;
using HarmonyLib.Tools;

namespace VEX.Patches
{
    //TODO:
    //
    //skill perk updates - overhaul them a bit to make them more impactful:
    //overhaul part 1: change the basic behaviour a bit or expand it
    //part 2: make sure each skill has at least one way to get extra skill
    //part 3: put special threshold abilities at 25,50,75,100 skill
    //part 4: give player's titles based on their highest skill(s)

    //show skill gain as floating text?
    //

    //jump skill -> greater fall distance / less fall damage           x
    //jump 25: less stam usage from wall jump x
    //jump 50: damage enemies you land on at certain velocity or enemies you wall jump off of x
    //jump 75: can fall from any height if landing on enemies x
    //jump 100: regen jump stam when landing on enemies x
    //jump off enemies = more skill
    //jump on ship (while it's moving at high speed) = more skill
    //jump off multiple walls = more skill

    //parry give more block skill                          x
    //parry projectile gives way more block skill
    //parry projectile will reflect if block skill is > 50
    //block 25: parry 
    //block 50: reflection of some projectiles
    //block 75: jump and block/parry without penalty
    //block 100: run and block

    // run skill: run faster on roads
    // run no stam on structures && in workbench
    // wall run more -> wall run = more skill

    //unarmed:
    //unarmed kick bigger, unarmed make dodge cheaper

    //swim    
    //swim in ocean give more swim skill, greatly increase swim speed + stam reduction with skill. with 50+ swim skill allow stam regen in water
    //

    //[HarmonyPatch(typeof(Player), "Start")]
    //public static class Player_Start_Patch
    //{
    //    private static void Postfix(ref Player __instance)
    //    {
    //    }
    //}

    public static class PlayerDefaults
    {
        public static float defaultSwimSpeed;
    }

    [HarmonyPatch(typeof(Player), "Start")]
    public static class Player_Start_Patch
    {
        private static void Postfix(ref Player __instance)
        {
            //TODO: remove the global cooldown for these...
            __instance.m_placeDelay = 0.167f; //default .4
            __instance.m_removeDelay = 0.167f; //default .25
            PlayerDefaults.defaultSwimSpeed = __instance.m_swimSpeed;
        }
    }

    [HarmonyPatch(typeof(Player), "PlayerAttackInput")]
    public static class Player_PlayerAttackInput_Patch
    {
        public static bool turbo = false;
        private static void Postfix(ref Player __instance, float dt)
        {
            if(turbo)
                __instance.m_queuedAttackTimer = 0f;
        }
    }

    [HarmonyPatch(typeof(Humanoid), "StartAttack")]
    public static class Humanoid_StartAttack_Patch
    {
        private static bool Prefix(ref Humanoid __instance, Character target, bool secondaryAttack, bool __result)
        {
            Humanoid self = __instance;
            self.AbortEquipQueue();
            bool isInAttack = false;
            if (Player_PlayerAttackInput_Patch.turbo)
                isInAttack = false;
            else
                isInAttack = self.InAttack();

            if ((isInAttack && !self.HaveQueuedChain()) || self.InDodge() || !self.CanMove() || self.IsKnockedBack() || self.IsStaggering() || self.InMinorAction())
            {
                __result = false;
                return false;
            }
            ItemDrop.ItemData currentWeapon = self.GetCurrentWeapon();
            if (currentWeapon == null)
            {
                __result = false;
                return false;
            }
            if (self.m_currentAttack != null)
            {
                self.m_currentAttack.Stop();
                self.m_previousAttack = self.m_currentAttack;
                self.m_currentAttack = null;
            }
            Attack attack;
            if (secondaryAttack)
            {
                if (!currentWeapon.HaveSecondaryAttack())
                {
                    __result = false;
                    return false;
                }
                attack = currentWeapon.m_shared.m_secondaryAttack.Clone();
            }
            else
            {
                if (!currentWeapon.HavePrimaryAttack())
                {
                    __result = false;
                    return false;
                }
                attack = currentWeapon.m_shared.m_attack.Clone();
            }
            if (attack.Start(self, self.m_body, self.m_zanim, self.m_animEvent, self.m_visEquipment, currentWeapon, self.m_previousAttack, self.m_timeSinceLastAttack, self.GetAttackDrawPercentage()))
            {
                self.m_currentAttack = attack;
                self.m_lastCombatTimer = 0f;
                __result = true;
                return false;
            }
            __result = false;
            return false;
        }
    }

    [HarmonyPatch(typeof(Player), "UpdateKnownRecipesList")]
    public static class Player_UpdateKnownRecipesList_Patch
    {
        static bool addchainonce = false;
        static bool addflintonce = false;
        static bool printonce;
        static void UpdateAvailableRecipes(Player self)
        {
            //TODO: create/enable more custom recipes here?

            if (!printonce)
            {
                Debug.Log("++++++++++===================================================");
                Debug.Log("++++++++++===================================================");
                Debug.Log("Recipes");
                Debug.Log("++++++++++===================================================");
            }

            List<Recipe> newRecipes = new List<Recipe>();

            foreach (Recipe recipe in ObjectDB.instance.m_recipes)
            {
                if(!printonce)
                {
                    Debug.Log("RECIPE_NAME: "+recipe.name);
                    Debug.Log("ENABLED: "+recipe.m_enabled);
                    Debug.Log("AMOUNT: "+recipe.m_amount);

                    if(recipe.m_craftingStation != null)
                        Debug.Log("CRAFTING STATION: " + recipe.m_craftingStation.m_name);

                    if (recipe.m_item != null)
                    {
                        Debug.Log("ITEM NAME: " + recipe.m_item.name);

                        if (recipe.m_item.m_itemData != null && recipe.m_item.m_itemData.m_shared != null)
                        {
                            Debug.Log("ITEM DATA NAME: " + recipe.m_item.m_itemData.m_shared.m_name);
                            Debug.Log("ITEM DATA DESC: " + recipe.m_item.m_itemData.m_shared.m_description);
                            Debug.Log("DLC?: " + recipe.m_item.m_itemData.m_shared.m_dlc);

                            if (recipe.m_item.m_itemData.m_shared.m_hitEffect != null && recipe.m_item.m_itemData.m_shared.m_hitEffect.m_effectPrefabs != null)
                            {
                                Debug.Log("Hit Effects");
                                recipe.m_item.m_itemData.m_shared.m_hitEffect.m_effectPrefabs.ToList().ForEach(x => Debug.Log(x.m_prefab.name));
                            }

                            if (recipe.m_item.m_itemData.m_shared.m_triggerEffect != null && recipe.m_item.m_itemData.m_shared.m_triggerEffect.m_effectPrefabs != null)
                            {
                                Debug.Log("Trigger Effects");
                                recipe.m_item.m_itemData.m_shared.m_triggerEffect.m_effectPrefabs.ToList().ForEach(x => Debug.Log(x.m_prefab.name));
                            }

                            if (recipe.m_item.m_itemData.m_shared.m_trailStartEffect != null && recipe.m_item.m_itemData.m_shared.m_trailStartEffect.m_effectPrefabs != null)
                            {
                                Debug.Log("Trail Effects");
                                recipe.m_item.m_itemData.m_shared.m_trailStartEffect.m_effectPrefabs.ToList().ForEach(x => Debug.Log(x.m_prefab.name));
                            }
                        }
                    }

                    Debug.Log("===================================================");
                }


                /*
                 
                //TODO: add chain craftable
                //TODO: add maypole craftable piece_maypole
                //TODO: make bone able to craft flint arrows
                //TODO: make 5x versions of food recipes


RECIPE_NAME: Recipe_IronNails 
(Filename: C:\buildslave\unity\build\Runtime/Export/Debug/Debug.bindings.h Line: 35)

ENABLED: True 
(Filename: C:\buildslave\unity\build\Runtime/Export/Debug/Debug.bindings.h Line: 35)

AMOUNT: 10 
(Filename: C:\buildslave\unity\build\Runtime/Export/Debug/Debug.bindings.h Line: 35)

CRAFTING STATION: $piece_forge 
(Filename: C:\buildslave\unity\build\Runtime/Export/Debug/Debug.bindings.h Line: 35)

ITEM NAME: IronNails 
(Filename: C:\buildslave\unity\build\Runtime/Export/Debug/Debug.bindings.h Line: 35)

ITEM DATA NAME: $item_ironnails 
(Filename: C:\buildslave\unity\build\Runtime/Export/Debug/Debug.bindings.h Line: 35)

ITEM DATA DESC: $item_ironnails_description 
(Filename: C:\buildslave\unity\build\Runtime/Export/Debug/Debug.bindings.h Line: 35)

DLC?:  
(Filename: C:\buildslave\unity\build\Runtime/Export/Debug/Debug.bindings.h Line: 35)

Hit Effects 
(Filename: C:\buildslave\unity\build\Runtime/Export/Debug/Debug.bindings.h Line: 35)

Trigger Effects 
(Filename: C:\buildslave\unity\build\Runtime/Export/Debug/Debug.bindings.h Line: 35)

Trail Effects 
(Filename: C:\buildslave\unity\build\Runtime/Export/Debug/Debug.bindings.h Line: 35)


                 */


                //enable chain
                if (recipe.name.Contains("Recipe_ArrowFlint") && !ObjectDB.instance.m_recipes.Any(r => r.name == "Recipe_ArrowFlintBone") && !addchainonce)
                {
                    addflintonce = true;
                    //copy it
                    Recipe newRecipe = ScriptableObject.Instantiate<Recipe>(recipe);
                    newRecipe.name = "Recipe_ArrowFlintBone";
                    newRecipe.m_resources = new Piece.Requirement[] { (new Piece.Requirement() { m_amount = 4, m_amountPerLevel = 0, m_recover = true, m_resItem = ObjectDB.m_instance.GetItemPrefab("BoneFragments").GetComponent<ItemDrop>() }) };
                    //newRecipe.m_item = ObjectDB.m_instance.GetItemPrefab("Chain").GetComponent<ItemDrop>();
                    //newRecipe.m_minStationLevel = 0;
                    //newRecipe.m_amount = 20;
                    //newRecipe.m_craftingStation = GameObject.FindObjectsOfType<CraftingStation>().Where(x => x.transform.parent == null && x.name == "piece_artisanstation").FirstOrDefault();

                    newRecipes.Add(newRecipe);

                    if (!self.m_knownRecipes.Contains(newRecipe.m_item.m_itemData.m_shared.m_name) && self.HaveRequirements(newRecipe, true, 0))
                    {
                        self.AddKnownRecipe(newRecipe);
                    }
                }





                if (recipe.name.Contains("Recipe_IronNails") && !ObjectDB.instance.m_recipes.Any(r => r.name == "Recipe_Chain") && !addchainonce)
                {
                    addchainonce = true;
                    //copy it
                    Recipe newRecipe = ScriptableObject.Instantiate<Recipe>(recipe);
                    newRecipe.name = "Recipe_Chain";
                    newRecipe.m_resources = new Piece.Requirement[] { (new Piece.Requirement() { m_amount = 1, m_amountPerLevel = 0, m_recover = true, m_resItem = ObjectDB.m_instance.GetItemPrefab("Iron").GetComponent<ItemDrop>() })};
                    newRecipe.m_item = ObjectDB.m_instance.GetItemPrefab("Chain").GetComponent<ItemDrop>();
                    newRecipe.m_minStationLevel = 0;
                    newRecipe.m_amount = 2;
                    //newRecipe.m_craftingStation = GameObject.FindObjectsOfType<CraftingStation>().Where(x => x.transform.parent == null && x.name == "piece_artisanstation").FirstOrDefault();
                        //CraftingStation.m_allStations.FirstOrDefault(x => x.m_name.Contains("piece_artisanstation"));
                    //if(newRecipe.m_craftingStation == null)
                    //{
                    //    Console.instance.AddString("WARNING: Could not find the required crafting station entity for Recipe_Chain! It will need to be located another way...");
                    //    Console.instance.AddString("Possible stations found: ");
                    //    GameObject.FindObjectsOfType<CraftingStation>().ToList().ForEach(s => Console.instance.AddString(s.m_name));
                    //}
                    //else
                    //{
                    //    Console.instance.AddString("Found artisan crafting station base prefab");
                    //}

                    newRecipes.Add(newRecipe);

                    if (!self.m_knownRecipes.Contains(newRecipe.m_item.m_itemData.m_shared.m_name) && self.HaveRequirements(newRecipe, true, 0))
                    {
                        self.AddKnownRecipe(newRecipe);
                    }
                }

                if (recipe.name.Contains("Recipe_CapeOdin"))
                {
                    recipe.m_enabled = true;
                    recipe.m_item.m_itemData.m_shared.m_dlc = string.Empty;
                }

                if (recipe.name.Contains("Recipe_HelmetOdin"))
                {
                    recipe.m_enabled = true;
                    recipe.m_item.m_itemData.m_shared.m_dlc = string.Empty;
                }

                //enable fire sword
                if (recipe.name.Contains("Recipe_SwordFire"))
                {
                    recipe.m_enabled = true;
                }

                if (recipe.m_enabled && !self.m_knownRecipes.Contains(recipe.m_item.m_itemData.m_shared.m_name) && self.HaveRequirements(recipe, true, 0))
                {
                    self.AddKnownRecipe(recipe);
                }
            }

            newRecipes.ForEach(r => {
                if (!ObjectDB.instance.m_recipes.Any(o => o.name == r.name))
                {
                    ObjectDB.instance.m_recipes.Add(r);
                    Console.instance.AddString("Added new recipe to database: "+r.name);
                }
            });

            newRecipes.Clear();

            printonce = true;
        }

        private static void Prefix(ref Player __instance)
        {
            Player self = __instance;

            if (Game.instance == null)
            {
                return;
            }

            if(!self.m_nview.IsValid() || self.m_isLoading)
            {
                return;
            }

            UpdateAvailableRecipes(self);
        }
    }

    [HarmonyPatch(typeof(Player), "AutoPickup")]
    public static class Player_AutoPickup_Patch
    {
        private static bool Prefix(ref Player __instance, float dt)
        {
            Player self = __instance;

            Vector3 vector = self.transform.position + Vector3.up;
            foreach (Collider collider in Physics.OverlapSphere(vector, self.m_autoPickupRange, self.m_autoPickupMask))
            {
                if (collider.attachedRigidbody)
                {
                    Fish fish = collider.attachedRigidbody.GetComponent<Fish>();
                    if (fish != null && fish.GetComponent<Floating>() != null && self.GetInventory().CanAddItem(fish.m_pickupItem, fish.m_pickupItemStackSize) && fish.GetComponent<ZNetView>().IsValid())
                    {
                        float num = Vector3.Distance(fish.transform.position, vector);
                        if (num <= self.m_autoPickupRange && self.m_inventory.GetTotalWeight() + 2 <= self.GetMaxCarryWeight())
                        {
                            if (num < 0.3f)
                            {
                                fish.Pickup(self);
                            }
                            else
                            {
                                Vector3 a = Vector3.Normalize(vector - fish.transform.position);
                                float d = 15f;
                                fish.transform.position = fish.transform.position + a * d * dt;
                            }
                        }
                        continue;
                    }

                    ItemDrop component = collider.attachedRigidbody.GetComponent<ItemDrop>();
                    if (!(component == null) && component.m_autoPickup && !self.HaveUniqueKey(component.m_itemData.m_shared.m_name) && component.GetComponent<ZNetView>().IsValid())
                    {
                        if (!component.CanPickup())
                        {
                            component.RequestOwn();
                        }
                        else if (self.m_inventory.CanAddItem(component.m_itemData, -1) && component.m_itemData.GetWeight() + self.m_inventory.GetTotalWeight() <= self.GetMaxCarryWeight())
                        {
                            float num = Vector3.Distance(component.transform.position, vector);
                            if (num <= self.m_autoPickupRange)
                            {
                                if (num < 0.3f)
                                {
                                    self.Pickup(component.gameObject);
                                }
                                else
                                {
                                    Vector3 a = Vector3.Normalize(vector - component.transform.position);
                                    float d = 15f;
                                    component.transform.position = component.transform.position + a * d * dt;
                                }
                            }
                        }
                    }
                }
            }

            return false;
        }


        [HarmonyPatch(typeof(Player), "OnSwiming")]
        public static class Player_OnSwiming_Patch
        {
            private static bool Prefix(ref Player __instance, Vector3 targetVel, float dt)
            {
                Player self = __instance;

                if (targetVel.magnitude > 0.1f)
                {
                    float skillFactor = self.m_skills.GetSkillFactor(Skills.SkillType.Swim);
                    float num = Mathf.Lerp(self.m_swimStaminaDrainMinSkill, self.m_swimStaminaDrainMaxSkill, skillFactor);
                    self.UseStamina(dt * num);
                    self.m_swimSkillImproveTimer += dt;
                    if (self.m_swimSkillImproveTimer > 1f)
                    {
                        self.m_swimSkillImproveTimer = 0f;
                        self.RaiseSkill(Skills.SkillType.Swim, 1f);
                    }
                }

                if (!self.HaveStamina(0f))
                {
                    self.m_drownDamageTimer += dt;
                    if (self.m_drownDamageTimer > 1f)
                    {
                        self.m_drownDamageTimer = 0f;
                        float damage = Mathf.Ceil(self.GetMaxHealth() / 20f);
                        HitData hitData = new HitData();
                        hitData.m_damage.m_damage = damage;
                        hitData.m_point = self.GetCenterPoint();
                        hitData.m_dir = Vector3.down;
                        hitData.m_pushForce = 10f;
                        self.Damage(hitData);
                        Vector3 position = self.transform.position;
                        position.y = self.m_waterLevel;
                        self.m_drownEffects.Create(position, self.transform.rotation, null, 1f);
                    }
                }

                return false;
            }
        }
    }

    [HarmonyPatch(typeof(Player), "UpdateStats")]
    public static class Player_UpdateStats_Patch
    {
        private static bool Prefix(ref Player __instance, float dt)
        {
            Player self = __instance;

            float swimSkillFactor = self.m_skills.GetSkillFactor(Skills.SkillType.Swim);
            float blockSkillFactor = self.m_skills.GetSkillFactor(Skills.SkillType.Blocking);

            if (self.InIntro() || self.IsTeleporting())
            {
                return false;
            }

            self.m_swimSpeed = Mathf.Lerp(PlayerDefaults.defaultSwimSpeed, PlayerDefaults.defaultSwimSpeed * 2f, swimSkillFactor);

            self.m_timeSinceDeath += dt;
            self.UpdateMovementModifier();
            self.UpdateFood(dt, false);
            bool flag = self.IsEncumbered();
            float maxStamina = self.GetMaxStamina();
            float num = 1f;

            num = UpdateStaminaFromBlocking(self, blockSkillFactor, num);

            //allow regen while "treading water" if swim skill >= 25
            bool disableRegenFromSwimming = ((swimSkillFactor < 0.25f ? self.IsSwiming() : false) && !self.IsOnGround());            

            if (disableRegenFromSwimming || self.InAttack() || self.InDodge() || self.m_wallRunning || flag)
            {
                num = 0f;
            }

            float num2 = (self.m_staminaRegen + (1f - self.m_stamina / maxStamina) * self.m_staminaRegen * self.m_staminaRegenTimeMultiplier) * num;
            float num3 = 1f;
            self.m_seman.ModifyStaminaRegen(ref num3);
            num2 *= num3;
            self.m_staminaRegenTimer -= dt;
            if (self.m_stamina < maxStamina && self.m_staminaRegenTimer <= 0f)
            {
                self.m_stamina = Mathf.Min(maxStamina, self.m_stamina + num2 * dt);
            }
            self.m_nview.GetZDO().Set("stamina", self.m_stamina);
            if (flag)
            {
                if (self.m_moveDir.magnitude > 0.1f)
                {
                    self.UseStamina(self.m_encumberedStaminaDrain * dt);
                }
                self.m_seman.AddStatusEffect("Encumbered", false);
                self.ShowTutorial("encumbered", false);
            }
            else
            {
                self.m_seman.RemoveStatusEffect("Encumbered", false);
            }
            if (!self.HardDeath())
            {
                self.m_seman.AddStatusEffect("SoftDeath", false);
            }
            else
            {
                self.m_seman.RemoveStatusEffect("SoftDeath", false);
            }
            self.UpdateEnvStatusEffects(dt);

            return false;
        }

        private static float UpdateStaminaFromBlocking(Player self, float blockSkillFactor, float num)
        {
            if (self.IsBlocking())
            {
                num *= Mathf.Lerp(0.8f, 1.0f, blockSkillFactor);
            }

            return num;
        }
    }



    [HarmonyPatch(typeof(Player), "ActivateGuardianPower")]
    public static class Player_ActivateGuardianPower_Patch
    {
        public static float guardianPowerRange = 10f;
        private static bool Prefix(ref Player __instance)
        {
            if (!NPZ.MOD_ENABLED)
                return true;

            Player self = __instance;

            if (self.m_guardianPowerCooldown > 0f)
            {
                return false;
            }
            if (self.m_guardianSE == null)
            {
                return false;
            }
            List<Player> list = new List<Player>();
            Player.GetPlayersInRange(self.transform.position, guardianPowerRange, list);
            foreach (Player player in list)
            {
                player.GetSEMan().AddStatusEffect(self.m_guardianSE.name, true);
            }
            List<NPC> list2 = new List<NPC>();
            NPC.GetFriendlyNPCsInRange(self.transform.position, guardianPowerRange, list2);
            foreach (NPC npc in list2)
            {
                npc.m_character.GetSEMan().AddStatusEffect(self.m_guardianSE.name, true);
            }
            self.m_guardianPowerCooldown = self.m_guardianSE.m_cooldown;
            return false;
        }
    }

    [HarmonyPatch(typeof(Player), "UpdateTeleport")]
    public static class Player_UpdateTeleport_Patch
    {
        public static bool enable_fastTeleport = true;

        private static void Prefix(ref Player __instance, float dt)
        {
            if (!enable_fastTeleport)
                return;

            Player self = __instance;

            if (self.m_teleporting)
            {
                if(self.m_teleportTimer < 6.5f)
                {
                    if (self.m_teleportTimer < 8f && (self.m_teleportFromPos - self.transform.position).magnitude < 200f)
                    {
                        self.m_teleportTimer = 8f;
                    }
                    else
                    {
                        self.m_teleportTimer = 6.5f;
                    }
                }
            }

            return;
        }
    }

    [HarmonyPatch(typeof(Player), "ShowTeleportAnimation")]
    public static class Player_ShowTeleportAnimation_Patch
    {
        private static bool Prefix(ref Player __instance, ref bool __result)
        {
            if (!Player_UpdateTeleport_Patch.enable_fastTeleport)
                return true;

            __result = false;

            return false;
        }
    }
}

