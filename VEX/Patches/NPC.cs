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
    public class NPC : MonoBehaviour
    {
        static bool loadedMasks = false;
        public static int m_placeRayMask;
        public static int m_placeGroundRayMask;
        public static int m_placeWaterRayMask;
        public static int m_removeRayMask;
        public static int m_interactMask;
        public static int m_autoPickupMask;

        static void CreateMasks()
        {
            if (loadedMasks)
                return;

            if (!loadedMasks)
                loadedMasks = true;
            
            m_placeRayMask = LayerMask.GetMask(new string[]
            {
            "Default",
            "static_solid",
            "Default_small",
            "piece",
            "piece_nonsolid",
            "terrain",
            "vehicle"
            });
            m_placeWaterRayMask = LayerMask.GetMask(new string[]
            {
            "Default",
            "static_solid",
            "Default_small",
            "piece",
            "piece_nonsolid",
            "terrain",
            "Water",
            "vehicle"
            });
            m_removeRayMask = LayerMask.GetMask(new string[]
            {
            "Default",
            "static_solid",
            "Default_small",
            "piece",
            "piece_nonsolid",
            "terrain",
            "vehicle"
            });
            m_interactMask = LayerMask.GetMask(new string[]
            {
            "item",
            "piece",
            "piece_nonsolid",
            "Default",
            "static_solid",
            "Default_small",
            "character",
            "character_net",
            "terrain",
            "vehicle"
            });
            m_autoPickupMask = LayerMask.GetMask(new string[]
            {
            "item"
            });
        }



        public NPZ npz;
        public MonsterAI m_monster;
        public Rigidbody m_body;
        public Character m_character;
        public Humanoid m_humanoid;
        public ZNetView m_nview;
        public Skills m_skills;
        public Animator m_animator;
		public GameObject m_visual;
        public VisEquipment m_visEquipment;
        public Inventory inventory {
            get {
                return m_humanoid.m_inventory;
            }
        }

        public bool canTravel = true;
        public bool showPin = true;
        public Minimap.PinData m_pin;

        //TODO: fill this out....
        public string sayTrigger;

        public bool m_teleporting;
        public float m_teleportCooldown;
        public float m_teleportTimer;
        public Quaternion m_teleportTargetRot;
        public Vector3    m_teleportTargetPos;
        public bool m_distantTeleport;
        public Quaternion m_teleportFromRot;
        public Vector3 m_teleportFromPos;

        public string TEMP_lastHeardString;
        public float m_hideDialogDelay = 5f;

        public int m_modelIndex;
        public Vector3 m_skinColor = Vector3.one;
        public Vector3 m_hairColor = Vector3.one;

        private const float m_baseHP = 25f;
        private const float m_baseStamina = 75f;
        private const int m_maxFoods = 3;
        private const float m_foodDrainPerSec = 0.1f;
        private float m_foodUpdateTimer;
        private float m_foodRegenTimer;
        private List<Player.Food> m_foods = new List<Player.Food>();

        public float m_staminaRegen = 5f;
        public float m_staminaRegenTimeMultiplier = 1f;
        public float m_staminaRegenDelay = 1f;
        public float m_runStaminaDrain = 10f;
        public float m_sneakStaminaDrain = 5f;
        public float m_swimStaminaDrainMinSkill = 5f;
        public float m_swimStaminaDrainMaxSkill = 2f;
        public float m_dodgeStaminaUsage = 10f;
        public float m_weightStaminaFactor = 0.1f;
        public float m_autoPickupRange = 2f;
        public float m_maxCarryWeight = 300f;
        public float m_encumberedStaminaDrain = 10f;
        private float m_stamina = 100f;
        private float m_maxStamina = 100f;
        private float m_staminaRegenTimer;

        public float m_queuedDodgeTimer;
        public float m_equipmentMovementModifier;
        public Vector3 m_queuedDodgeDir;
        public EffectList m_dodgeEffects;
        public bool m_inDodge;
        public bool m_dodgeInvincible;

        public float m_guardianPowerCooldown;
        public StatusEffect m_guardianSE;
        public float guardianPowerRange = 10f;
        public string m_guardianPower;

        bool removedOldModel;

        protected virtual void Awake()
        {
            CreateMasks();
            this.m_nview = this.GetComponent<ZNetView>();
            NPC.s_npcs.Add(this);
            int z = 0;
            Debug.Log("step " + ++z);
            m_humanoid = GetComponent<Humanoid>();
            Debug.Log("step "+ ++z);
            this.m_character = m_humanoid.GetComponent<Character>();
            Debug.Log("step " + ++z);
            this.m_body = m_character.m_body;
            Debug.Log("step " + ++z);
            this.m_monster = m_humanoid.GetComponent<MonsterAI>();
            Debug.Log("step " + ++z);
            m_visual = base.transform.Find("Visual").gameObject;
            Debug.Log("step " + ++z);
            m_animator = m_visual.GetComponent<Animator>();
            Debug.Log("step " + ++z);
            m_visEquipment = GetComponent<VisEquipment>();
            Debug.Log("step " + ++z);
            npz = GetComponent<NPZ>();
            Debug.Log("step " + ++z);
            if (npz == null)
                npz = gameObject.AddComponent<NPZ>();
            Debug.Log("step " + ++z);

            //TODO: virtual the couple functions that need it for skills
            m_skills = GetComponent<Skills>();
            Debug.Log("step " + ++z);
            if (m_skills == null)
                m_skills = gameObject.AddComponent<Skills>();
            Debug.Log("step " + ++z);

            SetupAwake();
            Debug.Log("step " + ++z);
            m_nview.GetZDO().m_persistent = true;
            Debug.Log("step " + ++z);

            if (m_character != null)
            {
                Debug.Log("step " + ++z);
                if (m_monster != null)
                    m_monster.SetDespawnInDay(false);
                Debug.Log("step " + ++z);
                m_pin = Minimap.instance.AddPin(transform.position, Minimap.PinType.Player, m_character.m_name, false, false);

                Debug.Log("step " + ++z);
                //TEMP
                SetPlayerFaction();
            }

            Debug.Log("step " + ++z);
            if (this.m_nview.IsOwner())
            {
                Debug.Log("step " + ++z);
                this.m_nview.Register<bool, bool>("OnTargeted", new Action<long, bool, bool>(this.RPC_OnTargeted));
                Debug.Log("step " + ++z);
                this.m_nview.Register<float>("UseStamina", new Action<long, float>(this.RPC_UseStamina));
                Debug.Log("step " + ++z);
                this.UpdateKnownRecipesList();
                Debug.Log("step " + ++z);
                this.UpdateAvailablePiecesList();
                Debug.Log("step " + ++z);
                this.SetupPlacementGhost();
            }
        }

        public void SetNPCID(long playerID, string name)
        {
            if (this.m_nview.GetZDO() == null)
            {
                return;
            }
            if (this.GetNPCID() != 0L)
            {
                return;
            }
            this.m_nview.GetZDO().Set("npcID", playerID);
            this.m_nview.GetZDO().Set("npcName", name);
        }

        public long GetNPCID()
        {
            if (this.m_nview.IsValid())
            {
                return this.m_nview.GetZDO().GetLong("npcID", 0L);
            }
            return 0L;
        }

        public string GetNPCName()
        {
            if (this.m_nview.IsValid())
            {
                return this.m_nview.GetZDO().GetString("npcName", "...");
            }
            return "";
        }

        public virtual string GetHoverText()
        {
            //TODO: show what the NPC is doing
            return "";
        }

        public virtual string GetHoverName()
        {
            return this.GetNPCName();
        }

        protected virtual void Start()
        {
            this.m_nview.GetZDO();
        }

        protected virtual void OnDestroy()
        {
            if (this.m_placementGhost)
            {
                UnityEngine.Object.Destroy(this.m_placementGhost);
                this.m_placementGhost = null;
            }
            NPC.s_npcs.Remove(this);
            Minimap.instance.RemovePin(m_pin);
            this.m_pin = null;
        }

        protected virtual void FixedUpdate()
        {
            if (this.m_nview.GetZDO() == null)
                return;

            if (!this.m_nview.IsOwner())
                return;

            if (m_character.IsDead())
                return;

            float fixedDeltaTime = Time.fixedDeltaTime;
            this.UpdateAwake(fixedDeltaTime);
            this.UpdateTargeted(fixedDeltaTime);
            this.UpdateEquipQueue(fixedDeltaTime);
            this.UpdateAttach();
            this.UpdateShipControl(fixedDeltaTime);
            this.UpdateCrouch(fixedDeltaTime);
            this.UpdateDodge(fixedDeltaTime);
            this.UpdateCover(fixedDeltaTime);
            this.UpdateStations(fixedDeltaTime);
            this.UpdateGuardianPower(fixedDeltaTime);
            this.UpdateBaseValue(fixedDeltaTime);
            this.UpdateStats(fixedDeltaTime);
            this.UpdateTeleport(fixedDeltaTime);
            this.AutoPickup(fixedDeltaTime);
            this.EdgeOfWorldKill(fixedDeltaTime);
            this.UpdateBiome(fixedDeltaTime);
            this.UpdateStealth(fixedDeltaTime);
        }

        public static int useCommand_holding = 1;
        public static int useCommand_nohold = 2;
        public static int hideCommand = 3;
        public static int toggleWalkCommand = 4;
        public static int toggleSitCommand = 5;
        public static int removeCommand = 6;
        public static int buildCommand = 7;
        public static int startGuardianPowerCommand = 8;

        public GameObject m_hovering;

        protected virtual void Update()
        {
            if (m_nview == null)
                return;

            if (!this.m_nview.IsValid())
            {
                return;
            }
            if (!this.m_nview.IsOwner())
            {
                return;
            }

            if(!removedOldModel)
            {
                Debug.Log("trying to cleanup old skeleton");
                Transform oldSkeleton = gameObject.GetComponentsInChildren<Transform>().FirstOrDefault(x => x.name == "_skeleton_base");
                if (oldSkeleton != null)
                {
                    Debug.Log("found skeleton base");
                    Transform go = oldSkeleton.parent;
                    if (go != null)
                    {
                        Debug.Log("destroying parent of npc skeleton base");
                        GameObject.Destroy(go.gameObject);
                        removedOldModel = true;
                    }
                }
            }

            //TODO: implement the AI to do these commands
            int inputCommand = 0;
            if(inputCommand != 0)
            {
                if (inputCommand == useCommand_nohold)
                {
                    if (this.m_hovering)
                    {
                        this.Interact(this.m_hovering, false);
                    }
                    else if (this.m_shipControl)
                    {
                        this.StopShipControl();
                    }
                }
                else if (inputCommand == useCommand_holding)
                {
                    this.Interact(this.m_hovering, true);
                }
                if (inputCommand == hideCommand)
                {
                    if (m_humanoid.GetRightItem() != null || m_humanoid.GetLeftItem() != null)
                    {
                        if (!this.InAttack())
                        {
                            m_humanoid.HideHandItems();
                        }
                    }
                    else if (!m_humanoid.IsSwiming() || m_humanoid.IsOnGround())
                    {
                        m_humanoid.ShowHandItems();
                    }
                }
                if (inputCommand == toggleWalkCommand)
                {
                    m_humanoid.SetWalk(!m_humanoid.GetWalk());
                    //if (m_humanoid.GetWalk())
                    //{
                    //    this.Message(MessageHud.MessageType.TopLeft, "$msg_walk 1", 0, null);
                    //}
                    //else
                    //{
                    //    this.Message(MessageHud.MessageType.TopLeft, "$msg_walk 0", 0, null);
                    //}
                }
                if (inputCommand == toggleSitCommand)
                {
                    if (this.InEmote() && m_humanoid.IsSitting())
                    {
                        this.StopEmote();
                    }
                    else
                    {
                        this.StartEmote("sit", false);
                    }
                }
                if (inputCommand == startGuardianPowerCommand)
                {
                    this.StartGuardianPower();
                }
            }

            this.UpdatePlacement(inputCommand, Time.deltaTime);
        }


        private void UpdatePlacement(int inputCommand, float dt)
        {
            //this.UpdateWearNTearHover();
            if (!this.InPlaceMode())
            {
                if (this.m_placementGhost)
                {
                    this.m_placementGhost.SetActive(false);
                }
                return;
            }
            if (inputCommand == 0)
            {
                return;
            }
            ItemDrop.ItemData rightItem = m_humanoid.GetRightItem();
            if (inputCommand == removeCommand && rightItem.m_shared.m_buildPieces.m_canRemovePieces)
            {
                if (this.HaveStamina(rightItem.m_shared.m_attack.m_attackStamina))
                {
                    if (this.RemovePiece())
                    {
                        m_humanoid.AddNoise(50f);
                        this.UseStamina(rightItem.m_shared.m_attack.m_attackStamina);
                        if (rightItem.m_shared.m_useDurability)
                        {
                            rightItem.m_durability -= rightItem.m_shared.m_useDurabilityDrain;
                        }
                    }
                }
                else
                {
                    Say("I don't have enough stamina to remove a building piece");
                    //Hud.instance.StaminaBarNoStaminaFlash();
                }
            }
            Piece selectedPiece = this.m_buildPieces.GetSelectedPiece();
            if (selectedPiece != null)
            {
                if (inputCommand == buildCommand)
                {
                    if (this.HaveStamina(rightItem.m_shared.m_attack.m_attackStamina))
                    {
                        if (selectedPiece.m_repairPiece)
                        {
                            this.Repair(rightItem, selectedPiece);
                        }
                        else if (this.m_placementGhost != null)
                        {
                            if (this.HaveRequirements(selectedPiece, Player.RequirementMode.CanBuild))
                            {
                                if (this.PlacePiece(selectedPiece))
                                {
                                    this.ConsumeResources(selectedPiece.m_resources, 0);
                                    this.UseStamina(rightItem.m_shared.m_attack.m_attackStamina);
                                    if (rightItem.m_shared.m_useDurability)
                                    {
                                        rightItem.m_durability -= rightItem.m_shared.m_useDurabilityDrain;
                                    }
                                }
                            }
                            else
                            {
                                Say("I don't have the required items to build what I want");
                            }
                        }
                    }
                    else
                    {
                        Say("I don't have enough stamina to remove a building piece");
                        //Hud.instance.StaminaBarNoStaminaFlash();
                    }
                }
            }
        }

        public void SetSelectedPiece(Vector2Int p)
        {
            if (this.m_buildPieces && this.m_buildPieces.GetSelectedIndex() != p)
            {
                this.m_buildPieces.SetSelected(p);
                this.SetupPlacementGhost();
            }
        }

        public Piece GetPiece(Vector2Int p)
        {
            if (this.m_buildPieces)
            {
                return this.m_buildPieces.GetPiece(p);
            }
            return null;
        }

        public bool IsPieceAvailable(Piece piece)
        {
            return this.m_buildPieces && this.m_buildPieces.IsPieceAvailable(piece);
        }

        public Piece GetSelectedPiece()
        {
            if (this.m_buildPieces)
            {
                return this.m_buildPieces.GetSelectedPiece();
            }
            return null;
        }

        private void LateUpdate()
        {
            if (!this.m_nview.IsValid())
            {
                return;
            }
            this.UpdateEmote();
            if (this.m_nview.IsOwner())
            {
                ZNet.instance.SetReferencePosition(m_humanoid.transform.position);
                this.UpdatePlacementGhost(false);
            }

            if (canTravel && m_monster != null)
            {
                m_monster.m_spawnPoint = transform.position;
                m_monster.m_nview.GetZDO().Set("spawnpoint", transform.position);
            }

            if (m_pin != null)
            {
                m_pin.m_pos = transform.position;
            }
        }

        private void SetupAwake()
        {
            if (this.m_nview.GetZDO() == null)
            {
                this.m_animator.SetBool("wakeup", false);
                return;
            }
            bool isWakeup = this.m_nview.GetZDO().GetBool("wakeup", true);
            this.m_animator.SetBool("wakeup", isWakeup);
            if (isWakeup)
            {
                this.m_wakeupTimer = 0f;
            }
        }

        private void UpdateAwake(float dt)
        {
            if (this.m_wakeupTimer >= 0f)
            {
                this.m_wakeupTimer += dt;
                if (this.m_wakeupTimer > 1f)
                {
                    this.m_wakeupTimer = -1f;
                    this.m_animator.SetBool("wakeup", false);
                    if (this.m_nview.IsOwner())
                    {
                        this.m_nview.GetZDO().Set("wakeup", false);
                    }
                }
            }
        }

        private void EdgeOfWorldKill(float dt)
        {
            if (this.IsDead())
            {
                return;
            }
            float magnitude = m_humanoid.transform.position.magnitude;
            float num = 10420f;
            if (magnitude > num && (m_humanoid.IsSwiming() || m_humanoid.transform.position.y < ZoneSystem.instance.m_waterLevel))
            {
                Vector3 a = Vector3.Normalize(m_humanoid.transform.position);
                float d = Utils.LerpStep(num, 10500f, magnitude) * 10f;
                this.m_body.MovePosition(this.m_body.position + a * d * dt);
            }
            if (magnitude > num && m_humanoid.transform.position.y < ZoneSystem.instance.m_waterLevel - 40f)
            {
                HitData hitData = new HitData();
                hitData.m_damage.m_damage = 99999f;
                m_humanoid.Damage(hitData);
            }
        }

        private void AutoPickup(float dt)
        {
            if (this.IsTeleporting())
            {
                return;
            }
            Vector3 vector = m_humanoid.transform.position + Vector3.up;
            foreach (Collider collider in Physics.OverlapSphere(vector, this.m_autoPickupRange, m_autoPickupMask))
            {
                if (collider.attachedRigidbody)
                {
                    ItemDrop component = collider.attachedRigidbody.GetComponent<ItemDrop>();
                    if (!(component == null) && component.m_autoPickup && !this.HaveUniqueKey(component.m_itemData.m_shared.m_name) && component.GetComponent<ZNetView>().IsValid())
                    {
                        if (!component.CanPickup())
                        {
                            component.RequestOwn();
                        }
                        else if (m_humanoid.m_inventory.CanAddItem(component.m_itemData, -1) && component.m_itemData.GetWeight() + m_humanoid.m_inventory.GetTotalWeight() <= this.GetMaxCarryWeight())
                        {
                            float num = Vector3.Distance(component.transform.position, vector);
                            if (num <= this.m_autoPickupRange)
                            {
                                if (num < 0.3f)
                                {
                                    m_humanoid.Pickup(component.gameObject);
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
        }

        public static int SE_Rested_CalculateComfortLevel(NPC npc)
        {
            List<Piece> nearbyPieces = SE_Rested.GetNearbyPieces(npc.transform.position);
            nearbyPieces.Sort(new Comparison<Piece>(SE_Rested.PieceComfortSort));
            int num = 1;
            if (npc.InShelter())
            {
                num++;
                int i = 0;
                while (i < nearbyPieces.Count)
                {
                    Piece piece = nearbyPieces[i];
                    if (i <= 0)
                    {
                        goto IL_77;
                    }
                    Piece piece2 = nearbyPieces[i - 1];
                    if ((piece.m_comfortGroup == Piece.ComfortGroup.None || piece.m_comfortGroup != piece2.m_comfortGroup) && !(piece.m_name == piece2.m_name))
                    {
                        goto IL_77;
                    }
IL_80:
                    i++;
                    continue;
IL_77:
                    num += piece.m_comfort;
                    goto IL_80;
                }
            }
            return num;
        }

        private void UpdateBaseValue(float dt)
        {
            this.m_baseValueUpdatetimer += dt;
            if (this.m_baseValueUpdatetimer > 2f)
            {
                this.m_baseValueUpdatetimer = 0f;
                this.m_baseValue = EffectArea.GetBaseValue(m_humanoid.transform.position, 20f);
                this.m_nview.GetZDO().Set("baseValue", this.m_baseValue);
                this.m_comfortLevel = SE_Rested_CalculateComfortLevel(this);
            }
        }

        public int GetComfortLevel()
        {
            return this.m_comfortLevel;
        }

        public int GetBaseValue()
        {
            if (!this.m_nview.IsValid())
            {
                return 0;
            }
            if (this.m_nview.IsOwner())
            {
                return this.m_baseValue;
            }
            return this.m_nview.GetZDO().GetInt("baseValue", 0);
        }

        public bool IsSafeInHome()
        {
            return this.m_safeInHome;
        }

        private void UpdateBiome(float dt)
        {
            this.m_biomeTimer += dt;
            if (this.m_biomeTimer > 1f)
            {
                this.m_biomeTimer = 0f;
                Heightmap.Biome biome = Heightmap.FindBiome(m_humanoid.transform.position);
                if (this.m_currentBiome != biome)
                {
                    this.m_currentBiome = biome;
                    this.AddKnownBiome(biome);
                }
            }
        }

        public Heightmap.Biome GetCurrentBiome()
        {
            return this.m_currentBiome;
        }

        public virtual void RaiseSkill(Skills.SkillType skill, float value = 1f)
        {
            float num = 1f;
            m_humanoid.m_seman.ModifyRaiseSkill(skill, ref num);
            value *= num;
            this.m_skills.RaiseSkill(skill, value);
        }

        private void UpdateStats(float dt)
        {
            if (this.IsTeleporting())
            {
                return;
            }
            this.m_timeSinceDeath += dt;
            this.UpdateMovementModifier();
            this.UpdateFood(dt, false);
            bool flag = this.IsEncumbered();
            float maxStamina = this.GetMaxStamina();
            float num = 1f;
            if (m_humanoid.IsBlocking())
            {
                num *= 0.8f;
            }
            if ((m_humanoid.IsSwiming() && !m_humanoid.IsOnGround()) || this.InAttack() || this.InDodge() || m_humanoid.m_wallRunning || flag)
            {
                num = 0f;
            }
            float num2 = (this.m_staminaRegen + (1f - this.m_stamina / maxStamina) * this.m_staminaRegen * this.m_staminaRegenTimeMultiplier) * num;
            float num3 = 1f;
            m_humanoid.m_seman.ModifyStaminaRegen(ref num3);
            num2 *= num3;
            this.m_staminaRegenTimer -= dt;
            if (this.m_stamina < maxStamina && this.m_staminaRegenTimer <= 0f)
            {
                this.m_stamina = Mathf.Min(maxStamina, this.m_stamina + num2 * dt);
            }
            this.m_nview.GetZDO().Set("stamina", this.m_stamina);
            if (flag)
            {
                if (m_humanoid.m_moveDir.magnitude > 0.1f)
                {
                    this.UseStamina(this.m_encumberedStaminaDrain * dt);
                }
                m_humanoid.m_seman.AddStatusEffect("Encumbered", false);
            }
            else
            {
                m_humanoid.m_seman.RemoveStatusEffect("Encumbered", false);
            }
            this.UpdateEnvStatusEffects(dt);
        }

        private void UpdateEnvStatusEffects(float dt)
        {
            this.m_nearFireTimer += dt;
            HitData.DamageModifiers damageModifiers = m_humanoid.GetDamageModifiers();
            bool flag = this.m_nearFireTimer < 0.25f;
            bool flag2 = m_humanoid.m_seman.HaveStatusEffect("Burning");
            bool flag3 = this.InShelter();
            HitData.DamageModifier modifier = damageModifiers.GetModifier(HitData.DamageType.Frost);
            bool flag4 = EnvMan.instance.IsFreezing();
            bool flag5 = EnvMan.instance.IsCold();
            bool flag6 = EnvMan.instance.IsWet();
            bool flag7 = this.IsSensed();
            bool flag8 = m_humanoid.m_seman.HaveStatusEffect("Wet");
            bool flag9 = m_humanoid.IsSitting();
            bool flag10 = flag4 && !flag && !flag3;
            bool flag11 = (flag5 && !flag) || (flag4 && flag && !flag3) || (flag4 && !flag && flag3);
            if (modifier == HitData.DamageModifier.Resistant || modifier == HitData.DamageModifier.VeryResistant)
            {
                flag10 = false;
                flag11 = false;
            }
            if (flag6 && !this.m_underRoof)
            {
                m_humanoid.m_seman.AddStatusEffect("Wet", true);
            }
            if (flag3)
            {
                m_humanoid.m_seman.AddStatusEffect("Shelter", false);
            }
            else
            {
                m_humanoid.m_seman.RemoveStatusEffect("Shelter", false);
            }
            if (flag)
            {
                m_humanoid.m_seman.AddStatusEffect("CampFire", false);
            }
            else
            {
                m_humanoid.m_seman.RemoveStatusEffect("CampFire", false);
            }
            bool flag12 = !flag7 && (flag9 || flag3) && (!flag11 & !flag10) && !flag8 && !flag2 && flag;
            if (flag12)
            {
                m_humanoid.m_seman.AddStatusEffect("Resting", false);
            }
            else
            {
                m_humanoid.m_seman.RemoveStatusEffect("Resting", false);
            }
            this.m_safeInHome = (flag12 && flag3);
            if (flag10)
            {
                if (!m_humanoid.m_seman.RemoveStatusEffect("Cold", true))
                {
                    m_humanoid.m_seman.AddStatusEffect("Freezing", false);
                    return;
                }
            }
            else if (flag11)
            {
                if (!m_humanoid.m_seman.RemoveStatusEffect("Freezing", true) && m_humanoid.m_seman.AddStatusEffect("Cold", false))
                {
                    return;
                }
            }
            else
            {
                m_humanoid.m_seman.RemoveStatusEffect("Cold", false);
                m_humanoid.m_seman.RemoveStatusEffect("Freezing", false);
            }
        }

        public bool CanEat(ItemDrop.ItemData item)
        {
            foreach (Player.Food food in this.m_foods)
            {
                if (food.m_item.m_shared.m_name == item.m_shared.m_name)
                {
                    if (food.CanEatAgain())
                    {
                        return true;
                    }
                    //this.Message(MessageHud.MessageType.Center, Localization.instance.Localize("$msg_nomore", new string[]
                    //{
                    //item.m_shared.m_name
                    //}), 0, null);
                    return false;
                }
            }
            using (List<Player.Food>.Enumerator enumerator = this.m_foods.GetEnumerator())
            {
                while (enumerator.MoveNext())
                {
                    if (enumerator.Current.CanEatAgain())
                    {
                        return true;
                    }
                }
            }
            if (this.m_foods.Count >= 3)
            {
                //this.Message(MessageHud.MessageType.Center, "$msg_isfull", 0, null);
                return false;
            }
            return true;
        }

        private Player.Food GetMostDepletedFood()
        {
            Player.Food food = null;
            foreach (Player.Food food2 in this.m_foods)
            {
                if (food2.CanEatAgain() && (food == null || food2.m_health < food.m_health))
                {
                    food = food2;
                }
            }
            return food;
        }

        public void ClearFood()
        {
            this.m_foods.Clear();
        }

        private bool EatFood(ItemDrop.ItemData item)
        {
            if (!this.CanEat(item))
            {
                return false;
            }
            foreach (Player.Food food in this.m_foods)
            {
                if (food.m_item.m_shared.m_name == item.m_shared.m_name)
                {
                    if (food.CanEatAgain())
                    {
                        food.m_health = item.m_shared.m_food;
                        food.m_stamina = item.m_shared.m_foodStamina;
                        this.UpdateFood(0f, true);
                        return true;
                    }
                    return false;
                }
            }
            if (this.m_foods.Count < 3)
            {
                Player.Food food2 = new Player.Food();
                food2.m_name = item.m_dropPrefab.name;
                food2.m_item = item;
                food2.m_health = item.m_shared.m_food;
                food2.m_stamina = item.m_shared.m_foodStamina;
                this.m_foods.Add(food2);
                this.UpdateFood(0f, true);
                return true;
            }
            Player.Food mostDepletedFood = this.GetMostDepletedFood();
            if (mostDepletedFood != null)
            {
                mostDepletedFood.m_name = item.m_dropPrefab.name;
                mostDepletedFood.m_item = item;
                mostDepletedFood.m_health = item.m_shared.m_food;
                mostDepletedFood.m_stamina = item.m_shared.m_foodStamina;
                return true;
            }
            return false;
        }

        private void UpdateFood(float dt, bool forceUpdate)
        {
            this.m_foodUpdateTimer += dt;
            if (this.m_foodUpdateTimer >= 1f || forceUpdate)
            {
                this.m_foodUpdateTimer = 0f;
                foreach (Player.Food food in this.m_foods)
                {
                    food.m_health -= food.m_item.m_shared.m_food / food.m_item.m_shared.m_foodBurnTime;
                    food.m_stamina -= food.m_item.m_shared.m_foodStamina / food.m_item.m_shared.m_foodBurnTime;
                    if (food.m_health < 0f)
                    {
                        food.m_health = 0f;
                    }
                    if (food.m_stamina < 0f)
                    {
                        food.m_stamina = 0f;
                    }
                    if (food.m_health <= 0f)
                    {
                        //this.Message(MessageHud.MessageType.Center, "$msg_food_done", 0, null);
                        this.m_foods.Remove(food);
                        break;
                    }
                }
                float health;
                float stamina;
                this.GetTotalFoodValue(out health, out stamina);
                this.SetMaxHealth(health);
                this.SetMaxStamina(stamina);
            }
            if (!forceUpdate)
            {
                this.m_foodRegenTimer += dt;
                if (this.m_foodRegenTimer >= 10f)
                {
                    this.m_foodRegenTimer = 0f;
                    float num = 0f;
                    foreach (Player.Food food2 in this.m_foods)
                    {
                        num += food2.m_item.m_shared.m_foodRegen;
                    }
                    if (num > 0f)
                    {
                        float num2 = 1f;
                        m_humanoid.m_seman.ModifyHealthRegen(ref num2);
                        num *= num2;
                        m_humanoid.Heal(num, true);
                    }
                }
            }
        }

        private void GetTotalFoodValue(out float hp, out float stamina)
        {
            hp = 25f;
            stamina = 75f;
            foreach (Player.Food food in this.m_foods)
            {
                hp += food.m_health;
                stamina += food.m_stamina;
            }
        }

        public float GetBaseFoodHP()
        {
            return 25f;
        }

        public List<Player.Food> GetFoods()
        {
            return this.m_foods;
        }

        protected virtual bool CheckRun(Vector3 moveDir, float dt)
        {
            if (!m_humanoid.CheckRun(moveDir, dt))
            {
                return false;
            }
            bool flag = this.HaveStamina(0f);
            float skillFactor = this.m_skills.GetSkillFactor(Skills.SkillType.Run);
            float num = Mathf.Lerp(1f, 0.5f, skillFactor);
            float num2 = this.m_runStaminaDrain * num;
            m_humanoid.m_seman.ModifyRunStaminaDrain(num2, ref num2);
            this.UseStamina(dt * num2);
            if (this.HaveStamina(0f))
            {
                this.m_runSkillImproveTimer += dt;
                if (this.m_runSkillImproveTimer > 1f)
                {
                    this.m_runSkillImproveTimer = 0f;
                    this.RaiseSkill(Skills.SkillType.Run, 1f);
                }
                this.AbortEquipQueue();
                return true;
            }
            if (flag)
            {
                Hud.instance.StaminaBarNoStaminaFlash();
            }
            return false;
        }

        private void UpdateMovementModifier()
        {
            this.m_equipmentMovementModifier = 0f;
            if (m_humanoid.m_rightItem != null)
            {
                this.m_equipmentMovementModifier += m_humanoid.m_rightItem.m_shared.m_movementModifier;
            }
            if (m_humanoid.m_leftItem != null)
            {
                this.m_equipmentMovementModifier += m_humanoid.m_leftItem.m_shared.m_movementModifier;
            }
            if (m_humanoid.m_chestItem != null)
            {
                this.m_equipmentMovementModifier += m_humanoid.m_chestItem.m_shared.m_movementModifier;
            }
            if (m_humanoid.m_legItem != null)
            {
                this.m_equipmentMovementModifier += m_humanoid.m_legItem.m_shared.m_movementModifier;
            }
            if (m_humanoid.m_helmetItem != null)
            {
                this.m_equipmentMovementModifier += m_humanoid.m_helmetItem.m_shared.m_movementModifier;
            }
            if (m_humanoid.m_shoulderItem != null)
            {
                this.m_equipmentMovementModifier += m_humanoid.m_shoulderItem.m_shared.m_movementModifier;
            }
            if (m_humanoid.m_utilityItem != null)
            {
                this.m_equipmentMovementModifier += m_humanoid.m_utilityItem.m_shared.m_movementModifier;
            }
        }

        public void OnSkillLevelup(Skills.SkillType skill, float level)
        {
            this.m_skillLevelupEffects.Create(m_humanoid.m_head.position, m_humanoid.m_head.rotation, m_humanoid.m_head, 1f);
        }

        protected virtual void OnJump()
        {
            this.AbortEquipQueue();
            float num = m_humanoid.m_jumpStaminaUsage - m_humanoid.m_jumpStaminaUsage * this.m_equipmentMovementModifier;
            m_humanoid.m_seman.ModifyJumpStaminaUsage(num, ref num);
            this.UseStamina(num);
        }

        protected virtual void OnSwiming(Vector3 targetVel, float dt)
        {
            m_humanoid.OnSwiming(targetVel, dt);
            if (targetVel.magnitude > 0.1f)
            {
                float skillFactor = this.m_skills.GetSkillFactor(Skills.SkillType.Swim);
                float num = Mathf.Lerp(this.m_swimStaminaDrainMinSkill, this.m_swimStaminaDrainMaxSkill, skillFactor);
                this.UseStamina(dt * num);
                this.m_swimSkillImproveTimer += dt;
                if (this.m_swimSkillImproveTimer > 1f)
                {
                    this.m_swimSkillImproveTimer = 0f;
                    this.RaiseSkill(Skills.SkillType.Swim, 1f);
                }
            }
            if (!this.HaveStamina(0f))
            {
                this.m_drownDamageTimer += dt;
                if (this.m_drownDamageTimer > 1f)
                {
                    this.m_drownDamageTimer = 0f;
                    float damage = Mathf.Ceil(m_humanoid.GetMaxHealth() / 20f);
                    HitData hitData = new HitData();
                    hitData.m_damage.m_damage = damage;
                    hitData.m_point = m_humanoid.GetCenterPoint();
                    hitData.m_dir = Vector3.down;
                    hitData.m_pushForce = 10f;
                    m_humanoid.Damage(hitData);
                    Vector3 position = m_humanoid.transform.position;
                    position.y = m_humanoid.m_waterLevel;
                    this.m_drownEffects.Create(position, m_humanoid.transform.rotation, null, 1f);
                }
            }
        }

        public void UseIventoryItem(int index)
        {
            if (index < m_humanoid.m_inventory.m_inventory.Count)
                return;

            ItemDrop.ItemData itemAt = m_humanoid.m_inventory.m_inventory[index];
            if (itemAt != null)
            {
                m_humanoid.UseItem(null, itemAt, false);
            }
        }

        public bool RequiredCraftingStation(Recipe recipe, int qualityLevel, bool checkLevel)
        {
            CraftingStation requiredStation = recipe.GetRequiredStation(qualityLevel);
            if (requiredStation != null)
            {
                if (this.m_currentStation == null)
                {
                    return false;
                }
                if (requiredStation.m_name != this.m_currentStation.m_name)
                {
                    return false;
                }
                if (checkLevel)
                {
                    int requiredStationLevel = recipe.GetRequiredStationLevel(qualityLevel);
                    if (this.m_currentStation.GetLevel() < requiredStationLevel)
                    {
                        return false;
                    }
                }
            }
            else if (this.m_currentStation != null && !this.m_currentStation.m_showBasicRecipies)
            {
                return false;
            }
            return true;
        }

        public bool HaveRequirements(Recipe recipe, bool discover, int qualityLevel)
        {
            if (discover)
            {
                if (recipe.m_craftingStation && !this.KnowStationLevel(recipe.m_craftingStation.m_name, recipe.m_minStationLevel))
                {
                    return false;
                }
            }
            else if (!this.RequiredCraftingStation(recipe, qualityLevel, true))
            {
                return false;
            }
            return (recipe.m_item.m_itemData.m_shared.m_dlc.Length <= 0 || DLCMan.instance.IsDLCInstalled(recipe.m_item.m_itemData.m_shared.m_dlc)) && this.HaveRequirements(recipe.m_resources, discover, qualityLevel);
        }

        private bool HaveRequirements(Piece.Requirement[] resources, bool discover, int qualityLevel)
        {
            foreach (Piece.Requirement requirement in resources)
            {
                if (requirement.m_resItem)
                {
                    if (discover)
                    {
                        if (requirement.m_amount > 0 && !this.m_knownMaterial.Contains(requirement.m_resItem.m_itemData.m_shared.m_name))
                        {
                            return false;
                        }
                    }
                    else
                    {
                        int amount = requirement.GetAmount(qualityLevel);
                        if (m_humanoid.m_inventory.CountItems(requirement.m_resItem.m_itemData.m_shared.m_name) < amount)
                        {
                            return false;
                        }
                    }
                }
            }
            return true;
        }

        public bool HaveRequirements(Piece piece, Player.RequirementMode mode)
        {
            if (piece.m_craftingStation)
            {
                if (mode == Player.RequirementMode.IsKnown || mode == Player.RequirementMode.CanAlmostBuild)
                {
                    if (!this.m_knownStations.ContainsKey(piece.m_craftingStation.m_name))
                    {
                        return false;
                    }
                }
                else if (!CraftingStation.HaveBuildStationInRange(piece.m_craftingStation.m_name, m_humanoid.transform.position))
                {
                    return false;
                }
            }
            if (piece.m_dlc.Length > 0 && !DLCMan.instance.IsDLCInstalled(piece.m_dlc))
            {
                return false;
            }
            foreach (Piece.Requirement requirement in piece.m_resources)
            {
                if (requirement.m_resItem && requirement.m_amount > 0)
                {
                    if (mode == Player.RequirementMode.IsKnown)
                    {
                        if (!this.m_knownMaterial.Contains(requirement.m_resItem.m_itemData.m_shared.m_name))
                        {
                            return false;
                        }
                    }
                    else if (mode == Player.RequirementMode.CanAlmostBuild)
                    {
                        if (!m_humanoid.m_inventory.HaveItem(requirement.m_resItem.m_itemData.m_shared.m_name))
                        {
                            return false;
                        }
                    }
                    else if (mode == Player.RequirementMode.CanBuild && m_humanoid.m_inventory.CountItems(requirement.m_resItem.m_itemData.m_shared.m_name) < requirement.m_amount)
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        //removed??? in 153.2
        //public void SetCraftingStation(CraftingStation station)
        //{
        //    if (this.m_currentStation == station)
        //    {
        //        return;
        //    }
        //    if (station)
        //    {
        //        this.AddKnownStation(station);
        //        station.PokeInUse();
        //    }
        //    this.m_currentStation = station;
        //    m_humanoid.HideHandItems();
        //    int value = this.m_currentStation ? this.m_currentStation.m_useAnimation : 0;
        //    m_humanoid.m_zanim.SetInt("crafting", value);
        //}

        //public CraftingStation GetCurrentCraftingStation()
        //{
        //    return this.m_currentStation;
        //}

        public void ConsumeResources(Piece.Requirement[] requirements, int qualityLevel)
        {
            foreach (Piece.Requirement requirement in requirements)
            {
                if (requirement.m_resItem)
                {
                    int amount = requirement.GetAmount(qualityLevel);
                    if (amount > 0)
                    {
                        m_humanoid.m_inventory.RemoveItem(requirement.m_resItem.m_itemData.m_shared.m_name, amount);
                    }
                }
            }
        }

        private bool CheckCanRemovePiece(Piece piece)
        {
            if (!this.m_noPlacementCost && piece.m_craftingStation != null && !CraftingStation.HaveBuildStationInRange(piece.m_craftingStation.m_name, m_humanoid.transform.position))
            {
                Say("I can't remove this piece. I lack the appropriate station nearby.");
                //this.Message(MessageHud.MessageType.Center, "$msg_missingstation", 0, null);
                return false;
            }
            return true;
        }

        private bool RemovePiece()
        {
            RaycastHit raycastHit;
            if (Physics.Raycast(GameCamera.instance.transform.position, GameCamera.instance.transform.forward, out raycastHit, 50f, NPC.m_removeRayMask) && Vector3.Distance(raycastHit.point, m_humanoid.m_eye.position) < this.m_maxPlaceDistance)
            {
                Piece piece = raycastHit.collider.GetComponentInParent<Piece>();
                if (piece == null && raycastHit.collider.GetComponent<Heightmap>())
                {
                    piece = TerrainModifier.FindClosestModifierPieceInRange(raycastHit.point, 2.5f);
                }
                if (piece)
                {
                    if (!piece.m_canBeRemoved)
                    {
                        return false;
                    }
                    if (Location.IsInsideNoBuildLocation(piece.transform.position))
                    {
                        Say("I cannot remove this piece because I am not in a build zone.");
                        //this.Message(MessageHud.MessageType.Center, "$msg_nobuildzone", 0, null);
                        return false;
                    }
                    if (!PrivateArea.CheckAccess(piece.transform.position, 0f, true, false))
                    {
                        Say("I cannot remove this piece because I this is a private zone.");
                        //this.Message(MessageHud.MessageType.Center, "$msg_privatezone", 0, null);
                        return false;
                    }
                    if (!this.CheckCanRemovePiece(piece))
                    {
                        return false;
                    }
                    ZNetView component = piece.GetComponent<ZNetView>();
                    if (component == null)
                    {
                        return false;
                    }
                    if (!piece.CanBeRemoved())
                    {
                        Say("I cannot remove this piece because it cannot be removed.");
                        //this.Message(MessageHud.MessageType.Center, "$msg_cantremovenow", 0, null);
                        return false;
                    }
                    WearNTear component2 = piece.GetComponent<WearNTear>();
                    if (component2)
                    {
                        component2.Remove();
                    }
                    else
                    {
                        ZLog.Log("Removing non WNT object with hammer " + piece.name);
                        component.ClaimOwnership();
                        piece.DropResources();
                        piece.m_placeEffect.Create(piece.transform.position, piece.transform.rotation, piece.gameObject.transform, 1f);
                        this.m_removeEffects.Create(piece.transform.position, Quaternion.identity, null, 1f);
                        ZNetScene.instance.Destroy(piece.gameObject);
                    }
                    ItemDrop.ItemData rightItem = m_humanoid.GetRightItem();
                    if (rightItem != null)
                    {
                        this.FaceLookDirection();
                        m_humanoid.m_zanim.SetTrigger(rightItem.m_shared.m_attack.m_attackAnimation);
                    }
                    return true;
                }
            }
            return false;
        }

        public void FaceLookDirection()
        {
            m_humanoid.transform.rotation = m_humanoid.GetLookYaw();
        }

        private bool PlacePiece(Piece piece)
        {
            this.UpdatePlacementGhost(true);
            Vector3 position = this.m_placementGhost.transform.position;
            Quaternion rotation = this.m_placementGhost.transform.rotation;
            GameObject gameObject = piece.gameObject;
            switch (this.m_placementStatus)
            {
                case Player.PlacementStatus.Invalid:
                    //this.Message(MessageHud.MessageType.Center, "$msg_invalidplacement", 0, null);
                    return false;
                case Player.PlacementStatus.BlockedbyPlayer:
                    //this.Message(MessageHud.MessageType.Center, "$msg_blocked", 0, null);
                    return false;
                case Player.PlacementStatus.NoBuildZone:
                    //this.Message(MessageHud.MessageType.Center, "$msg_nobuildzone", 0, null);
                    return false;
                case Player.PlacementStatus.PrivateZone:
                    //this.Message(MessageHud.MessageType.Center, "$msg_privatezone", 0, null);
                    return false;
                case Player.PlacementStatus.MoreSpace:
                    //this.Message(MessageHud.MessageType.Center, "$msg_needspace", 0, null);
                    return false;
                case Player.PlacementStatus.NoTeleportArea:
                    //this.Message(MessageHud.MessageType.Center, "$msg_noteleportarea", 0, null);
                    return false;
                case Player.PlacementStatus.ExtensionMissingStation:
                    //this.Message(MessageHud.MessageType.Center, "$msg_extensionmissingstation", 0, null);
                    return false;
                case Player.PlacementStatus.WrongBiome:
                    //this.Message(MessageHud.MessageType.Center, "$msg_wrongbiome", 0, null);
                    return false;
                case Player.PlacementStatus.NeedCultivated:
                    //this.Message(MessageHud.MessageType.Center, "$msg_needcultivated", 0, null);
                    return false;
                case Player.PlacementStatus.NotInDungeon:
                    //this.Message(MessageHud.MessageType.Center, "$msg_notindungeon", 0, null);
                    return false;
                default:
                    {
                        TerrainModifier.SetTriggerOnPlaced(true);
                        GameObject gameObject2 = UnityEngine.Object.Instantiate<GameObject>(gameObject, position, rotation);
                        TerrainModifier.SetTriggerOnPlaced(false);
                        CraftingStation componentInChildren = gameObject2.GetComponentInChildren<CraftingStation>();
                        if (componentInChildren)
                        {
                            this.AddKnownStation(componentInChildren);
                        }
                        Piece component = gameObject2.GetComponent<Piece>();
                        if (component)
                        {
                            component.SetCreator(this.GetNPCID());
                        }
                        PrivateArea component2 = gameObject2.GetComponent<PrivateArea>();
                        if (component2)
                        {
                            component2.Setup(Game.instance.GetPlayerProfile().GetName());
                        }
                        WearNTear component3 = gameObject2.GetComponent<WearNTear>();
                        if (component3)
                        {
                            component3.OnPlaced();
                        }
                        ItemDrop.ItemData rightItem = m_humanoid.GetRightItem();
                        if (rightItem != null)
                        {
                            this.FaceLookDirection();
                            m_humanoid.m_zanim.SetTrigger(rightItem.m_shared.m_attack.m_attackAnimation);
                        }
                        piece.m_placeEffect.Create(position, rotation, gameObject2.transform, 1f);
                        m_humanoid.AddNoise(50f);
                        Game.instance.GetPlayerProfile().m_playerStats.m_builds++;
                        ZLog.Log("Placed " + gameObject.name);
                        Gogan.LogEvent("Game", "PlacedPiece", gameObject.name, 0L);
                        return true;
                    }
            }
        }

        public void GetBuildSelection(out Piece go, out Vector2Int id, out int total, out Piece.PieceCategory category, out bool useCategory)
        {
            category = this.m_buildPieces.m_selectedCategory;
            useCategory = this.m_buildPieces.m_useCategories;
            if (this.m_buildPieces.GetAvailablePiecesInSelectedCategory() == 0)
            {
                go = null;
                id = Vector2Int.zero;
                total = 0;
                return;
            }
            GameObject selectedPrefab = this.m_buildPieces.GetSelectedPrefab();
            go = (selectedPrefab ? selectedPrefab.GetComponent<Piece>() : null);
            id = this.m_buildPieces.GetSelectedIndex();
            total = this.m_buildPieces.GetAvailablePiecesInSelectedCategory();
        }

        public List<Piece> GetBuildPieces()
        {
            if (this.m_buildPieces)
            {
                return this.m_buildPieces.GetPiecesInSelectedCategory();
            }
            return null;
        }

        public int GetAvailableBuildPiecesInCategory(Piece.PieceCategory cat)
        {
            if (this.m_buildPieces)
            {
                return this.m_buildPieces.GetAvailablePiecesInCategory(cat);
            }
            return 0;
        }

        private void CreateDeathEffects()
        {
            GameObject[] array = m_humanoid.m_deathEffects.Create(m_humanoid.transform.position, m_humanoid.transform.rotation, m_humanoid.transform, 1f);
            for (int i = 0; i < array.Length; i++)
            {
                Ragdoll component = array[i].GetComponent<Ragdoll>();
                if (component)
                {
                    Vector3 velocity = this.m_body.velocity;
                    if (m_humanoid.m_pushForce.magnitude * 0.5f > velocity.magnitude)
                    {
                        velocity = m_humanoid.m_pushForce * 0.5f;
                    }
                    component.Setup(velocity, 0f, 0f, 0f, null);
                    m_humanoid.OnRagdollCreated(component);
                    this.m_ragdoll = component;
                }
            }
        }

        public void UnequipDeathDropItems()
        {
            if (m_humanoid.m_rightItem != null)
            {
                m_humanoid.UnequipItem(m_humanoid.m_rightItem, false);
            }
            if (m_humanoid.m_leftItem != null)
            {
                m_humanoid.UnequipItem(m_humanoid.m_leftItem, false);
            }
            if (m_humanoid.m_ammoItem != null)
            {
                m_humanoid.UnequipItem(m_humanoid.m_ammoItem, false);
            }
            if (m_humanoid.m_utilityItem != null)
            {
                m_humanoid.UnequipItem(m_humanoid.m_utilityItem, false);
            }
        }

        private void CreateTombStone()
        {
            if (m_humanoid.m_inventory.NrOfItems() == 0)
            {
                return;
            }
            m_humanoid.UnequipAllItems();
            GameObject gameObject = UnityEngine.Object.Instantiate<GameObject>(this.m_tombstone, m_humanoid.GetCenterPoint(), m_humanoid.transform.rotation);
            gameObject.GetComponent<Container>().GetInventory().MoveInventoryToGrave(m_humanoid.m_inventory);
            TombStone component = gameObject.GetComponent<TombStone>();
            PlayerProfile playerProfile = Game.instance.GetPlayerProfile();
            component.Setup(playerProfile.GetName(), playerProfile.GetPlayerID());
        }

        protected virtual void OnDeath()
        {
            this.m_nview.GetZDO().Set("dead", true);
            this.m_nview.InvokeRPC(ZNetView.Everybody, "OnDeath", Array.Empty<object>());
            Game.instance.GetPlayerProfile().m_playerStats.m_deaths++;
            Game.instance.GetPlayerProfile().SetDeathPoint(m_humanoid.transform.position);
            this.CreateDeathEffects();
            this.CreateTombStone();
            this.m_foods.Clear();
            //if (flag)
            {
                this.m_skills.OnDeath();
            }
            Game.instance.RequestRespawn(10f);
            this.m_timeSinceDeath = 0f;
            string eventLabel = "biome:" + this.GetCurrentBiome().ToString();
            Gogan.LogEvent("Game", "Death", eventLabel, 0L);
        }

        public void OnRespawn()
        {
            this.m_nview.GetZDO().Set("dead", false);
            m_humanoid.SetHealth(m_humanoid.GetMaxHealth());
        }

        private void SetupPlacementGhost()
        {
            if (this.m_placementGhost)
            {
                UnityEngine.Object.Destroy(this.m_placementGhost);
                this.m_placementGhost = null;
            }
            if (this.m_buildPieces == null)
            {
                return;
            }
            GameObject selectedPrefab = this.m_buildPieces.GetSelectedPrefab();
            if (selectedPrefab == null)
            {
                return;
            }
            if (selectedPrefab.GetComponent<Piece>().m_repairPiece)
            {
                return;
            }
            bool enabled = false;
            TerrainModifier componentInChildren = selectedPrefab.GetComponentInChildren<TerrainModifier>();
            if (componentInChildren)
            {
                enabled = componentInChildren.enabled;
                componentInChildren.enabled = false;
            }
            TerrainOp.m_forceDisableTerrainOps = true;
            ZNetView.m_forceDisableInit = true;
            this.m_placementGhost = UnityEngine.Object.Instantiate<GameObject>(selectedPrefab);
            ZNetView.m_forceDisableInit = false;
            TerrainOp.m_forceDisableTerrainOps = false;
            this.m_placementGhost.name = selectedPrefab.name;
            if (componentInChildren)
            {
                componentInChildren.enabled = enabled;
            }
            Joint[] componentsInChildren = this.m_placementGhost.GetComponentsInChildren<Joint>();
            for (int i = 0; i < componentsInChildren.Length; i++)
            {
                UnityEngine.Object.Destroy(componentsInChildren[i]);
            }
            Rigidbody[] componentsInChildren2 = this.m_placementGhost.GetComponentsInChildren<Rigidbody>();
            for (int i = 0; i < componentsInChildren2.Length; i++)
            {
                UnityEngine.Object.Destroy(componentsInChildren2[i]);
            }
            foreach (Collider collider in this.m_placementGhost.GetComponentsInChildren<Collider>())
            {
                if ((1 << collider.gameObject.layer & NPC.m_placeRayMask) == 0)
                {
                    ZLog.Log("Disabling " + collider.gameObject.name + "  " + LayerMask.LayerToName(collider.gameObject.layer));
                    collider.enabled = false;
                }
            }
            Transform[] componentsInChildren4 = this.m_placementGhost.GetComponentsInChildren<Transform>();
            int layer = LayerMask.NameToLayer("ghost");
            Transform[] array = componentsInChildren4;
            for (int i = 0; i < array.Length; i++)
            {
                array[i].gameObject.layer = layer;
            }
            TerrainModifier[] componentsInChildren5 = this.m_placementGhost.GetComponentsInChildren<TerrainModifier>();
            for (int i = 0; i < componentsInChildren5.Length; i++)
            {
                UnityEngine.Object.Destroy(componentsInChildren5[i]);
            }
            GuidePoint[] componentsInChildren6 = this.m_placementGhost.GetComponentsInChildren<GuidePoint>();
            for (int i = 0; i < componentsInChildren6.Length; i++)
            {
                UnityEngine.Object.Destroy(componentsInChildren6[i]);
            }
            Light[] componentsInChildren7 = this.m_placementGhost.GetComponentsInChildren<Light>();
            for (int i = 0; i < componentsInChildren7.Length; i++)
            {
                UnityEngine.Object.Destroy(componentsInChildren7[i]);
            }
            AudioSource[] componentsInChildren8 = this.m_placementGhost.GetComponentsInChildren<AudioSource>();
            for (int i = 0; i < componentsInChildren8.Length; i++)
            {
                componentsInChildren8[i].enabled = false;
            }
            ZSFX[] componentsInChildren9 = this.m_placementGhost.GetComponentsInChildren<ZSFX>();
            for (int i = 0; i < componentsInChildren9.Length; i++)
            {
                componentsInChildren9[i].enabled = false;
            }
            Windmill componentInChildren2 = this.m_placementGhost.GetComponentInChildren<Windmill>();
            if (componentInChildren2)
            {
                componentInChildren2.enabled = false;
            }
            ParticleSystem[] componentsInChildren10 = this.m_placementGhost.GetComponentsInChildren<ParticleSystem>();
            for (int i = 0; i < componentsInChildren10.Length; i++)
            {
                componentsInChildren10[i].gameObject.SetActive(false);
            }
            Transform transform = this.m_placementGhost.transform.Find("_GhostOnly");
            if (transform)
            {
                transform.gameObject.SetActive(true);
            }
            this.m_placementGhost.transform.position = m_humanoid.transform.position;
            this.m_placementGhost.transform.localScale = selectedPrefab.transform.localScale;
            foreach (MeshRenderer meshRenderer in this.m_placementGhost.GetComponentsInChildren<MeshRenderer>())
            {
                if (!(meshRenderer.sharedMaterial == null))
                {
                    Material[] sharedMaterials = meshRenderer.sharedMaterials;
                    for (int j = 0; j < sharedMaterials.Length; j++)
                    {
                        Material material = new Material(sharedMaterials[j]);
                        material.SetFloat("_RippleDistance", 0f);
                        material.SetFloat("_ValueNoise", 0f);
                        sharedMaterials[j] = material;
                    }
                    meshRenderer.sharedMaterials = sharedMaterials;
                    meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                }
            }
        }

        private void SetPlacementGhostValid(bool valid)
        {
            this.m_placementGhost.GetComponent<Piece>().SetInvalidPlacementHeightlight(!valid);
        }

        protected virtual void SetPlaceMode(PieceTable buildPieces)
        {
            m_humanoid.SetPlaceMode(buildPieces);
            this.m_buildPieces = buildPieces;
            this.UpdateAvailablePiecesList();
        }

        public void SetBuildCategory(int index)
        {
            if (this.m_buildPieces != null)
            {
                this.m_buildPieces.SetCategory(index);
                this.UpdateAvailablePiecesList();
            }
        }

        public virtual bool InPlaceMode()
        {
            return this.m_buildPieces != null;
        }

        private void Repair(ItemDrop.ItemData toolItem, Piece repairPiece)
        {
            if (!this.InPlaceMode())
            {
                return;
            }
            Piece hoveringPiece = this.GetHoveringPiece();
            if (hoveringPiece)
            {
                if (!this.CheckCanRemovePiece(hoveringPiece))
                {
                    return;
                }
                if (!PrivateArea.CheckAccess(hoveringPiece.transform.position, 0f, true, false))
                {
                    return;
                }
                bool flag = false;
                WearNTear component = hoveringPiece.GetComponent<WearNTear>();
                if (component && component.Repair())
                {
                    flag = true;
                }
                if (flag)
                {
                    this.FaceLookDirection();
                    m_humanoid.m_zanim.SetTrigger(toolItem.m_shared.m_attack.m_attackAnimation);
                    hoveringPiece.m_placeEffect.Create(hoveringPiece.transform.position, hoveringPiece.transform.rotation, null, 1f);
                    //this.Message(MessageHud.MessageType.TopLeft, Localization.instance.Localize("$msg_repaired", new string[]
                    //{
                    //hoveringPiece.m_name
                    //}), 0, null);
                    this.UseStamina(toolItem.m_shared.m_attack.m_attackStamina);
                    if (toolItem.m_shared.m_useDurability)
                    {
                        toolItem.m_durability -= toolItem.m_shared.m_useDurabilityDrain;
                        return;
                    }
                }
                else
                {
                    //this.Message(MessageHud.MessageType.TopLeft, hoveringPiece.m_name + " $msg_doesnotneedrepair", 0, null);
                }
            }
        }

        public Piece GetHoveringPiece()
        {
            if (this.InPlaceMode())
            {
                return this.m_hoveringPiece;
            }
            return null;
        }

        private void UpdatePlacementGhost(bool flashGuardStone)
        {
            if (this.m_placementGhost == null)
            {
                if (this.m_placementMarkerInstance)
                {
                    this.m_placementMarkerInstance.SetActive(false);
                }
                return;
            }
            bool flag = false; //TODO: replace this with an input command?
                //ZInput.GetButton("AltPlace") || ZInput.GetButton("JoyAltPlace");
            Piece component = this.m_placementGhost.GetComponent<Piece>();
            bool water = component.m_waterPiece || component.m_noInWater;
            Vector3 vector;
            Vector3 up;
            Piece piece;
            Heightmap heightmap;
            Collider x;
            if (this.PieceRayTest(out vector, out up, out piece, out heightmap, out x, water))
            {
                this.m_placementStatus = Player.PlacementStatus.Valid;
                if (this.m_placementMarkerInstance == null)
                {
                    this.m_placementMarkerInstance = UnityEngine.Object.Instantiate<GameObject>(this.m_placeMarker, vector, Quaternion.identity);
                }
                this.m_placementMarkerInstance.SetActive(true);
                this.m_placementMarkerInstance.transform.position = vector;
                this.m_placementMarkerInstance.transform.rotation = Quaternion.LookRotation(up);
                if (component.m_groundOnly || component.m_groundPiece || component.m_cultivatedGroundOnly)
                {
                    this.m_placementMarkerInstance.SetActive(false);
                }
                WearNTear wearNTear = (piece != null) ? piece.GetComponent<WearNTear>() : null;
                StationExtension component2 = component.GetComponent<StationExtension>();
                if (component2 != null)
                {
                    CraftingStation craftingStation = component2.FindClosestStationInRange(vector);
                    if (craftingStation)
                    {
                        component2.StartConnectionEffect(craftingStation);
                    }
                    else
                    {
                        component2.StopConnectionEffect();
                        this.m_placementStatus = Player.PlacementStatus.ExtensionMissingStation;
                    }
                    if (component2.OtherExtensionInRange(component.m_spaceRequirement))
                    {
                        this.m_placementStatus = Player.PlacementStatus.MoreSpace;
                    }
                }
                if (wearNTear && !wearNTear.m_supports)
                {
                    this.m_placementStatus = Player.PlacementStatus.Invalid;
                }
                if (component.m_waterPiece && x == null && !flag)
                {
                    this.m_placementStatus = Player.PlacementStatus.Invalid;
                }
                if (component.m_noInWater && x != null)
                {
                    this.m_placementStatus = Player.PlacementStatus.Invalid;
                }
                if (component.m_groundPiece && heightmap == null)
                {
                    this.m_placementGhost.SetActive(false);
                    this.m_placementStatus = Player.PlacementStatus.Invalid;
                    return;
                }
                if (component.m_groundOnly && heightmap == null)
                {
                    this.m_placementStatus = Player.PlacementStatus.Invalid;
                }
                if (component.m_cultivatedGroundOnly && (heightmap == null || !heightmap.IsCultivated(vector)))
                {
                    this.m_placementStatus = Player.PlacementStatus.NeedCultivated;
                }
                if (component.m_notOnWood && piece && wearNTear && (wearNTear.m_materialType == WearNTear.MaterialType.Wood || wearNTear.m_materialType == WearNTear.MaterialType.HardWood))
                {
                    this.m_placementStatus = Player.PlacementStatus.Invalid;
                }
                if (component.m_notOnTiltingSurface && up.y < 0.8f)
                {
                    this.m_placementStatus = Player.PlacementStatus.Invalid;
                }
                if (component.m_inCeilingOnly && up.y > -0.5f)
                {
                    this.m_placementStatus = Player.PlacementStatus.Invalid;
                }
                if (component.m_notOnFloor && up.y > 0.1f)
                {
                    this.m_placementStatus = Player.PlacementStatus.Invalid;
                }
                if (component.m_onlyInTeleportArea && !EffectArea.IsPointInsideArea(vector, EffectArea.Type.Teleport, 0f))
                {
                    this.m_placementStatus = Player.PlacementStatus.NoTeleportArea;
                }
                if (!component.m_allowedInDungeons && m_humanoid.InInterior())
                {
                    this.m_placementStatus = Player.PlacementStatus.NotInDungeon;
                }
                if (heightmap)
                {
                    up = Vector3.up;
                }
                this.m_placementGhost.SetActive(true);
                Quaternion rotation = Quaternion.identity;//TODO: the piece's desired rotation... Quaternion.Euler(0f, 22.5f * (float)this.m_placeRotation, 0f);
                if (((component.m_groundPiece || component.m_clipGround) && heightmap) || component.m_clipEverything)
                {
                    GameObject selectedPrefab = this.m_buildPieces.GetSelectedPrefab();
                    TerrainModifier component5 = selectedPrefab.GetComponent<TerrainModifier>();
                    TerrainOp component4 = selectedPrefab.GetComponent<TerrainOp>();
                    if ((component5 || component4) && component.m_allowAltGroundPlacement && component.m_groundPiece && !ZInput.GetButton("AltPlace") && !ZInput.GetButton("JoyAltPlace"))
                    {
                        float groundHeight = ZoneSystem.instance.GetGroundHeight(m_humanoid.transform.position);
                        vector.y = groundHeight;
                    }
                    this.m_placementGhost.transform.position = vector;
                    this.m_placementGhost.transform.rotation = rotation;
                }
                else
                {
                    Collider[] componentsInChildren = this.m_placementGhost.GetComponentsInChildren<Collider>();
                    if (componentsInChildren.Length != 0)
                    {
                        this.m_placementGhost.transform.position = vector + up * 50f;
                        this.m_placementGhost.transform.rotation = rotation;
                        Vector3 b = Vector3.zero;
                        float num = 999999f;
                        foreach (Collider collider in componentsInChildren)
                        {
                            if (!collider.isTrigger && collider.enabled)
                            {
                                MeshCollider meshCollider = collider as MeshCollider;
                                if (!(meshCollider != null) || meshCollider.convex)
                                {
                                    Vector3 vector2 = collider.ClosestPoint(vector);
                                    float num2 = Vector3.Distance(vector2, vector);
                                    if (num2 < num)
                                    {
                                        b = vector2;
                                        num = num2;
                                    }
                                }
                            }
                        }
                        Vector3 b2 = this.m_placementGhost.transform.position - b;
                        if (component.m_waterPiece)
                        {
                            b2.y = 3f;
                        }
                        this.m_placementGhost.transform.position = vector + b2;
                        this.m_placementGhost.transform.rotation = rotation;
                    }
                }
                if (!flag)
                {
                    this.m_tempPieces.Clear();
                    Transform transform;
                    Transform transform2;
                    if (this.FindClosestSnapPoints(this.m_placementGhost.transform, 0.5f, out transform, out transform2, this.m_tempPieces))
                    {
                        Vector3 position = transform2.parent.position;
                        Vector3 vector3 = transform2.position - (transform.position - this.m_placementGhost.transform.position);
                        if (!this.IsOverlapingOtherPiece(vector3, this.m_placementGhost.name, this.m_tempPieces))
                        {
                            this.m_placementGhost.transform.position = vector3;
                        }
                    }
                }
                if (Location.IsInsideNoBuildLocation(this.m_placementGhost.transform.position))
                {
                    this.m_placementStatus = Player.PlacementStatus.NoBuildZone;
                }
                PrivateArea component30 = component.GetComponent<PrivateArea>();
                float radius = component30 ? component30.m_radius : 0f;
                bool wardCheck = component30 != null;
                if (!PrivateArea.CheckAccess(this.m_placementGhost.transform.position, radius, flashGuardStone, wardCheck))
                {
                    this.m_placementStatus = Player.PlacementStatus.PrivateZone;
                }
                if (this.CheckPlacementGhostVSPlayers())
                {
                    this.m_placementStatus = Player.PlacementStatus.BlockedbyPlayer;
                }
                if (component.m_onlyInBiome != Heightmap.Biome.None && (Heightmap.FindBiome(this.m_placementGhost.transform.position) & component.m_onlyInBiome) == Heightmap.Biome.None)
                {
                    this.m_placementStatus = Player.PlacementStatus.WrongBiome;
                }
                if (component.m_noClipping && this.TestGhostClipping(this.m_placementGhost, 0.2f))
                {
                    this.m_placementStatus = Player.PlacementStatus.Invalid;
                }
            }
            else
            {
                if (this.m_placementMarkerInstance)
                {
                    this.m_placementMarkerInstance.SetActive(false);
                }
                this.m_placementGhost.SetActive(false);
                this.m_placementStatus = Player.PlacementStatus.Invalid;
            }
            this.SetPlacementGhostValid(this.m_placementStatus == Player.PlacementStatus.Valid);
        }


        private bool IsOverlapingOtherPiece(Vector3 p, string pieceName, List<Piece> pieces)
        {
            foreach (Piece piece in this.m_tempPieces)
            {
                if (Vector3.Distance(p, piece.transform.position) < 0.05f && piece.gameObject.name.StartsWith(pieceName))
                {
                    return true;
                }
            }
            return false;
        }

        private bool FindClosestSnapPoints(Transform ghost, float maxSnapDistance, out Transform a, out Transform b, List<Piece> pieces)
        {
            this.m_tempSnapPoints1.Clear();
            ghost.GetComponent<Piece>().GetSnapPoints(this.m_tempSnapPoints1);
            this.m_tempSnapPoints2.Clear();
            this.m_tempPieces.Clear();
            Piece.GetSnapPoints(ghost.transform.position, 10f, this.m_tempSnapPoints2, this.m_tempPieces);
            float num = 9999999f;
            a = null;
            b = null;
            foreach (Transform transform in this.m_tempSnapPoints1)
            {
                Transform transform2;
                float num2;
                if (this.FindClosestSnappoint(transform.position, this.m_tempSnapPoints2, maxSnapDistance, out transform2, out num2) && num2 < num)
                {
                    num = num2;
                    a = transform;
                    b = transform2;
                }
            }
            return a != null;
        }

        private bool FindClosestSnappoint(Vector3 p, List<Transform> snapPoints, float maxDistance, out Transform closest, out float distance)
        {
            closest = null;
            distance = 999999f;
            foreach (Transform transform in snapPoints)
            {
                float num = Vector3.Distance(transform.position, p);
                if (num <= maxDistance && num < distance)
                {
                    closest = transform;
                    distance = num;
                }
            }
            return closest != null;
        }

        private bool TestGhostClipping(GameObject ghost, float maxPenetration)
        {
            Collider[] componentsInChildren = ghost.GetComponentsInChildren<Collider>();
            Collider[] array = Physics.OverlapSphere(ghost.transform.position, 10f, NPC.m_placeRayMask);
            foreach (Collider collider in componentsInChildren)
            {
                foreach (Collider collider2 in array)
                {
                    Vector3 vector;
                    float num;
                    if (Physics.ComputePenetration(collider, collider.transform.position, collider.transform.rotation, collider2, collider2.transform.position, collider2.transform.rotation, out vector, out num) && num > maxPenetration)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        private bool CheckPlacementGhostVSPlayers()
        {
            if (this.m_placementGhost == null)
            {
                return false;
            }
            List<Character> list = new List<Character>();
            Character.GetCharactersInRange(m_humanoid.transform.position, 30f, list);
            foreach (Collider collider in this.m_placementGhost.GetComponentsInChildren<Collider>())
            {
                if (!collider.isTrigger && collider.enabled)
                {
                    MeshCollider meshCollider = collider as MeshCollider;
                    if (!(meshCollider != null) || meshCollider.convex)
                    {
                        foreach (Character character in list)
                        {
                            CapsuleCollider collider2 = character.GetCollider();
                            Vector3 vector;
                            float num;
                            if (Physics.ComputePenetration(collider, collider.transform.position, collider.transform.rotation, collider2, collider2.transform.position, collider2.transform.rotation, out vector, out num))
                            {
                                return true;
                            }
                        }
                    }
                }
            }
            return false;
        }

        private bool PieceRayTest(out Vector3 point, out Vector3 normal, out Piece piece, out Heightmap heightmap, out Collider waterSurface, bool water)
        {
            int layerMask = NPC.m_placeRayMask;
            if (water)
            {
                layerMask = NPC.m_placeWaterRayMask;
            }
            RaycastHit raycastHit;
            if (Physics.Raycast(GameCamera.instance.transform.position, GameCamera.instance.transform.forward, out raycastHit, 50f, layerMask) && raycastHit.collider && !raycastHit.collider.attachedRigidbody && Vector3.Distance(m_humanoid.m_eye.position, raycastHit.point) < this.m_maxPlaceDistance)
            {
                point = raycastHit.point;
                normal = raycastHit.normal;
                piece = raycastHit.collider.GetComponentInParent<Piece>();
                heightmap = raycastHit.collider.GetComponent<Heightmap>();
                if (raycastHit.collider.gameObject.layer == LayerMask.NameToLayer("Water"))
                {
                    waterSurface = raycastHit.collider;
                }
                else
                {
                    waterSurface = null;
                }
                return true;
            }
            point = Vector3.zero;
            normal = Vector3.zero;
            piece = null;
            heightmap = null;
            waterSurface = null;
            return false;
        }

        private void FindHoverObject(out GameObject hover, out Character hoverCreature)
        {
            hover = null;
            hoverCreature = null;
            RaycastHit[] array = Physics.RaycastAll(GameCamera.instance.transform.position, GameCamera.instance.transform.forward, 50f, NPC.m_interactMask);
            Array.Sort<RaycastHit>(array, (RaycastHit x, RaycastHit y) => x.distance.CompareTo(y.distance));
            RaycastHit[] array2 = array;
            int i = 0;
            while (i < array2.Length)
            {
                RaycastHit raycastHit = array2[i];
                if (!raycastHit.collider.attachedRigidbody || !(raycastHit.collider.attachedRigidbody.gameObject == m_humanoid.gameObject))
                {
                    if (hoverCreature == null)
                    {
                        Character character = raycastHit.collider.attachedRigidbody ? raycastHit.collider.attachedRigidbody.GetComponent<Character>() : raycastHit.collider.GetComponent<Character>();
                        if (character != null)
                        {
                            hoverCreature = character;
                        }
                    }
                    if (Vector3.Distance(m_humanoid.m_eye.position, raycastHit.point) >= this.m_maxInteractDistance)
                    {
                        break;
                    }
                    if (raycastHit.collider.GetComponent<Hoverable>() != null)
                    {
                        hover = raycastHit.collider.gameObject;
                        return;
                    }
                    if (raycastHit.collider.attachedRigidbody)
                    {
                        hover = raycastHit.collider.attachedRigidbody.gameObject;
                        return;
                    }
                    hover = raycastHit.collider.gameObject;
                    return;
                }
                else
                {
                    i++;
                }
            }
        }

        private void Interact(GameObject go, bool hold)
        {
            if (this.InAttack() || this.InDodge())
            {
                return;
            }
            if (hold && Time.time - this.m_lastHoverInteractTime < 0.2f)
            {
                return;
            }
            Interactable componentInParent = go.GetComponentInParent<Interactable>();
            if (componentInParent != null)
            {
                this.m_lastHoverInteractTime = Time.time;
                if (componentInParent.Interact(m_humanoid, hold))
                {
                    Vector3 forward = go.transform.position - m_humanoid.transform.position;
                    forward.y = 0f;
                    forward.Normalize();
                    m_humanoid.transform.rotation = Quaternion.LookRotation(forward);
                    m_humanoid.m_zanim.SetTrigger("interact");
                }
            }
        }

        public static void CraftingStation_UpdateKnownStationsInRange(NPC npc)
        {
            Vector3 position = npc.transform.position;
            foreach (CraftingStation craftingStation in CraftingStation.m_allStations)
            {
                if (Vector3.Distance(craftingStation.transform.position, position) < craftingStation.m_discoverRange)
                {
                    npc.AddKnownStation(craftingStation);
                }
            }
        }

        private void UpdateStations(float dt)
        {
            this.m_stationDiscoverTimer += dt;
            if (this.m_stationDiscoverTimer > 1f)
            {
                this.m_stationDiscoverTimer = 0f;
                CraftingStation_UpdateKnownStationsInRange(this);
            }

            if (!(this.m_currentStation != null))
            {
                if (this.m_inCraftingStation)
                {
                    this.m_humanoid.m_zanim.SetInt("crafting", 0);
                    this.m_inCraftingStation = false;
                }
                return;
            }
            if (!this.m_currentStation.InUseDistance(m_humanoid))
            {
                this.SetCraftingStation(null);
                return;
            }
            //TODO: some kind of bool is needed to allow npcs to "use" stations
            //if (!InventoryGui.IsVisible())
            {
                this.SetCraftingStation(null);
                return;
            }
            this.m_currentStation.PokeInUse();
            //if (!this.AlwaysRotateCamera())
            {
                Vector3 normalized = (this.m_currentStation.transform.position - base.transform.position).normalized;
                normalized.y = 0f;
                normalized.Normalize();
                Quaternion to = Quaternion.LookRotation(normalized);
                base.transform.rotation = Quaternion.RotateTowards(base.transform.rotation, to, this.m_humanoid.m_turnSpeed * dt);
            }
            this.m_humanoid.m_zanim.SetInt("crafting", this.m_currentStation.m_useAnimation);
            this.m_inCraftingStation = true;
        }

        public void SetCraftingStation(CraftingStation station)
        {
            if (this.m_currentStation == station)
            {
                return;
            }
            if (station)
            {
                this.AddKnownStation(station);
                station.PokeInUse();
                m_humanoid.HideHandItems();
            }
            this.m_currentStation = station;
        }

        public CraftingStation GetCurrentCraftingStation()
        {
            return this.m_currentStation;
        }

        private void UpdateCover(float dt)
        {
            this.m_updateCoverTimer += dt;
            if (this.m_updateCoverTimer > 1f)
            {
                this.m_updateCoverTimer = 0f;
                Cover.GetCoverForPoint(m_humanoid.GetCenterPoint(), out this.m_coverPercentage, out this.m_underRoof);
            }
        }

        public Character GetHoverCreature()
        {
            return this.m_hoveringCreature;
        }

        public virtual GameObject GetHoverObject()
        {
            return this.m_hovering;
        }

        public virtual void OnNearFire(Vector3 point)
        {
            this.m_nearFireTimer = 0f;
        }

        public bool InShelter()
        {
            return this.m_coverPercentage >= 0.8f && this.m_underRoof;
        }

        public float GetStamina()
        {
            return this.m_stamina;
        }

        public virtual float GetMaxStamina()
        {
            return this.m_maxStamina;
        }

        public virtual float GetStaminaPercentage()
        {
            return this.m_stamina / this.m_maxStamina;
        }

        public void SetGodMode(bool godMode)
        {
            this.m_godMode = godMode;
        }

        public virtual bool InGodMode()
        {
            return this.m_godMode;
        }

        public void SetGhostMode(bool ghostmode)
        {
            this.m_ghostMode = ghostmode;
        }

        public virtual bool InGhostMode()
        {
            return this.m_ghostMode;
        }

        public virtual void AddStamina(float v)
        {
            this.m_stamina += v;
            if (this.m_stamina > this.m_maxStamina)
            {
                this.m_stamina = this.m_maxStamina;
            }
        }

        public virtual void UseStamina(float v)
        {
            if (v == 0f)
            {
                return;
            }
            if (!this.m_nview.IsValid())
            {
                return;
            }
            if (this.m_nview.IsOwner())
            {
                this.RPC_UseStamina(0L, v);
                return;
            }
            this.m_nview.InvokeRPC("UseStamina", new object[]
            {
            v
            });
        }

        private void RPC_UseStamina(long sender, float v)
        {
            if (v == 0f)
            {
                return;
            }
            this.m_stamina -= v;
            if (this.m_stamina < 0f)
            {
                this.m_stamina = 0f;
            }
            this.m_staminaRegenTimer = this.m_staminaRegenDelay;
        }

        public virtual bool HaveStamina(float amount = 0f)
        {
            if (this.m_nview.IsValid() && !this.m_nview.IsOwner())
            {
                return this.m_nview.GetZDO().GetFloat("stamina", this.m_maxStamina) > amount;
            }
            return this.m_stamina > amount;
        }

        public void Save(ZPackage pkg)
        {
            pkg.Write(24);
            pkg.Write(m_humanoid.GetMaxHealth());
            pkg.Write(m_humanoid.GetHealth());
            pkg.Write(this.GetMaxStamina());
            //pkg.Write(this.m_firstSpawn);
            pkg.Write(this.m_timeSinceDeath);
            pkg.Write(this.m_guardianPower);
            pkg.Write(this.m_guardianPowerCooldown);
            m_humanoid.m_inventory.Save(pkg);
            pkg.Write(this.m_knownRecipes.Count);
            foreach (string data in this.m_knownRecipes)
            {
                pkg.Write(data);
            }
            pkg.Write(this.m_knownStations.Count);
            foreach (KeyValuePair<string, int> keyValuePair in this.m_knownStations)
            {
                pkg.Write(keyValuePair.Key);
                pkg.Write(keyValuePair.Value);
            }
            pkg.Write(this.m_knownMaterial.Count);
            foreach (string data2 in this.m_knownMaterial)
            {
                pkg.Write(data2);
            }
            //pkg.Write(this.m_shownTutorials.Count);
            //foreach (string data3 in this.m_shownTutorials)
            //{
            //    pkg.Write(data3);
            //}
            pkg.Write(this.m_uniques.Count);
            foreach (string data4 in this.m_uniques)
            {
                pkg.Write(data4);
            }
            pkg.Write(this.m_trophies.Count);
            foreach (string data5 in this.m_trophies)
            {
                pkg.Write(data5);
            }
            pkg.Write(this.m_knownBiome.Count);
            foreach (Heightmap.Biome data6 in this.m_knownBiome)
            {
                pkg.Write((int)data6);
            }
            pkg.Write(this.m_knownTexts.Count);
            foreach (KeyValuePair<string, string> keyValuePair2 in this.m_knownTexts)
            {
                pkg.Write(keyValuePair2.Key);
                pkg.Write(keyValuePair2.Value);
            }
            pkg.Write(m_humanoid.m_beardItem);
            pkg.Write(m_humanoid.m_hairItem);
            pkg.Write(this.m_skinColor);
            pkg.Write(this.m_hairColor);
            pkg.Write(this.m_modelIndex);
            pkg.Write(this.m_foods.Count);
            foreach (Player.Food food in this.m_foods)
            {
                pkg.Write(food.m_name);
                pkg.Write(food.m_health);
                pkg.Write(food.m_stamina);
            }
            this.m_skills.Save(pkg);
        }

        public void Load(ZPackage pkg)
        {
            this.m_isLoading = true;
            m_humanoid.UnequipAllItems();
            int num = pkg.ReadInt();
            if (num >= 7)
            {
                this.SetMaxHealth(pkg.ReadSingle());
            }
            float num2 = pkg.ReadSingle();
            float maxHealth = m_humanoid.GetMaxHealth();
            if (num2 <= 0f || num2 > maxHealth || float.IsNaN(num2))
            {
                num2 = maxHealth;
            }
            m_humanoid.SetHealth(num2);
            if (num >= 10)
            {
                float stamina = pkg.ReadSingle();
                this.SetMaxStamina(stamina);
                this.m_stamina = stamina;
            }
            if (num >= 20)
            {
                this.m_timeSinceDeath = pkg.ReadSingle();
            }
            if (num >= 23)
            {
                string guardianPower = pkg.ReadString();
                this.SetGuardianPower(guardianPower);
            }
            if (num >= 24)
            {
                this.m_guardianPowerCooldown = pkg.ReadSingle();
            }
            if (num == 2)
            {
                pkg.ReadZDOID();
            }
            m_humanoid.m_inventory.Load(pkg);
            int num3 = pkg.ReadInt();
            for (int i = 0; i < num3; i++)
            {
                string item = pkg.ReadString();
                this.m_knownRecipes.Add(item);
            }
            if (num < 15)
            {
                int num4 = pkg.ReadInt();
                for (int j = 0; j < num4; j++)
                {
                    pkg.ReadString();
                }
            }
            else
            {
                int num5 = pkg.ReadInt();
                for (int k = 0; k < num5; k++)
                {
                    string key = pkg.ReadString();
                    int value = pkg.ReadInt();
                    this.m_knownStations.Add(key, value);
                }
            }
            int num6 = pkg.ReadInt();
            for (int l = 0; l < num6; l++)
            {
                string item2 = pkg.ReadString();
                this.m_knownMaterial.Add(item2);
            }
            if (num < 19 || num >= 21)
            {
                int num7 = pkg.ReadInt();
                for (int m = 0; m < num7; m++)
                {
                    string item3 = pkg.ReadString();
                    //this.m_shownTutorials.Add(item3);
                }
            }
            if (num >= 6)
            {
                int num8 = pkg.ReadInt();
                for (int n = 0; n < num8; n++)
                {
                    string item4 = pkg.ReadString();
                    this.m_uniques.Add(item4);
                }
            }
            if (num >= 9)
            {
                int num9 = pkg.ReadInt();
                for (int num10 = 0; num10 < num9; num10++)
                {
                    string item5 = pkg.ReadString();
                    this.m_trophies.Add(item5);
                }
            }
            if (num >= 18)
            {
                int num11 = pkg.ReadInt();
                for (int num12 = 0; num12 < num11; num12++)
                {
                    Heightmap.Biome item6 = (Heightmap.Biome)pkg.ReadInt();
                    this.m_knownBiome.Add(item6);
                }
            }
            if (num >= 22)
            {
                int num13 = pkg.ReadInt();
                for (int num14 = 0; num14 < num13; num14++)
                {
                    string key2 = pkg.ReadString();
                    string value2 = pkg.ReadString();
                    this.m_knownTexts.Add(key2, value2);
                }
            }
            if (num >= 4)
            {
                string beard = pkg.ReadString();
                string hair = pkg.ReadString();
                m_humanoid.SetBeard(beard);
                m_humanoid.SetHair(hair);
            }
            if (num >= 5)
            {
                Vector3 skinColor = pkg.ReadVector3();
                Vector3 hairColor = pkg.ReadVector3();
                this.SetSkinColor(skinColor);
                this.SetHairColor(hairColor);
            }
            if (num >= 11)
            {
                int playerModel = pkg.ReadInt();
                this.SetPlayerModel(playerModel);
            }
            if (num >= 12)
            {
                this.m_foods.Clear();
                int num15 = pkg.ReadInt();
                for (int num16 = 0; num16 < num15; num16++)
                {
                    if (num >= 14)
                    {
                        Player.Food food = new Player.Food();
                        food.m_name = pkg.ReadString();
                        food.m_health = pkg.ReadSingle();
                        if (num >= 16)
                        {
                            food.m_stamina = pkg.ReadSingle();
                        }
                        GameObject itemPrefab = ObjectDB.instance.GetItemPrefab(food.m_name);
                        if (itemPrefab == null)
                        {
                            ZLog.LogWarning("FAiled to find food item " + food.m_name);
                        }
                        else
                        {
                            food.m_item = itemPrefab.GetComponent<ItemDrop>().m_itemData;
                            this.m_foods.Add(food);
                        }
                    }
                    else
                    {
                        pkg.ReadString();
                        pkg.ReadSingle();
                        pkg.ReadSingle();
                        pkg.ReadSingle();
                        pkg.ReadSingle();
                        pkg.ReadSingle();
                        pkg.ReadSingle();
                        if (num >= 13)
                        {
                            pkg.ReadSingle();
                        }
                    }
                }
            }
            if (num >= 17)
            {
                this.m_skills.Load(pkg);
            }
            this.m_isLoading = false;
            this.UpdateAvailablePiecesList();
            this.EquipIventoryItems();
        }

        private void EquipIventoryItems()
        {
            foreach (ItemDrop.ItemData itemData in m_humanoid.m_inventory.GetEquipedtems())
            {
                if (!m_humanoid.EquipItem(itemData, false))
                {
                    itemData.m_equiped = false;
                }
            }
        }

        //TODO: hook up to places...
        public virtual bool CanMove()
        {
            return !this.IsTeleporting() && !this.InCutscene() && (!this.IsEncumbered() || this.HaveStamina(0f)) && m_character.CanMove();
        }

        public virtual bool IsEncumbered()
        {
            return m_humanoid.m_inventory.GetTotalWeight() > this.GetMaxCarryWeight();
        }

        public float GetMaxCarryWeight()
        {
            float maxCarryWeight = this.m_maxCarryWeight;
            m_humanoid.m_seman.ModifyMaxCarryWeight(maxCarryWeight, ref maxCarryWeight);
            return maxCarryWeight;
        }

        public virtual bool HaveUniqueKey(string name)
        {
            return this.m_uniques.Contains(name);
        }

        public virtual void AddUniqueKey(string name)
        {
            if (!this.m_uniques.Contains(name))
            {
                this.m_uniques.Add(name);
            }
        }

        public bool IsBiomeKnown(Heightmap.Biome biome)
        {
            return this.m_knownBiome.Contains(biome);
        }

        public void AddKnownBiome(Heightmap.Biome biome)
        {
            if (!this.m_knownBiome.Contains(biome))
            {
                this.m_knownBiome.Add(biome);
                if (biome != Heightmap.Biome.Meadows && biome != Heightmap.Biome.None)
                {
                    //string text = "$biome_" + biome.ToString().ToLower();
                    //MessageHud.instance.ShowBiomeFoundMsg(text, true);
                }
                if (biome == Heightmap.Biome.BlackForest && !ZoneSystem.instance.GetGlobalKey("defeated_eikthyr"))
                {
                    //this.ShowTutorial("blackforest", false);
                }
                //Gogan.LogEvent("Game", "BiomeFound", biome.ToString(), 0L);
            }
        }

        public bool IsRecipeKnown(string name)
        {
            return this.m_knownRecipes.Contains(name);
        }

        public void AddKnownRecipe(Recipe recipe)
        {
            if (!this.m_knownRecipes.Contains(recipe.m_item.m_itemData.m_shared.m_name))
            {
                this.m_knownRecipes.Add(recipe.m_item.m_itemData.m_shared.m_name);
                //MessageHud.instance.QueueUnlockMsg(recipe.m_item.m_itemData.GetIcon(), "$msg_newrecipe", recipe.m_item.m_itemData.m_shared.m_name);
                //Gogan.LogEvent("Game", "RecipeFound", recipe.m_item.m_itemData.m_shared.m_name, 0L);
            }
        }

        public void AddKnownPiece(Piece piece)
        {
            if (!this.m_knownRecipes.Contains(piece.m_name))
            {
                this.m_knownRecipes.Add(piece.m_name);
                //MessageHud.instance.QueueUnlockMsg(piece.m_icon, "$msg_newpiece", piece.m_name);
                //Gogan.LogEvent("Game", "PieceFound", piece.m_name, 0L);
            }
        }

        public void AddKnownStation(CraftingStation station)
        {
            int level = station.GetLevel();
            int num;
            if (this.m_knownStations.TryGetValue(station.m_name, out num))
            {
                if (num < level)
                {
                    this.m_knownStations[station.m_name] = level;
                    //MessageHud.instance.QueueUnlockMsg(station.m_icon, "$msg_newstation_level", station.m_name + " $msg_level " + level);
                    this.UpdateKnownRecipesList();
                }
                return;
            }
            this.m_knownStations.Add(station.m_name, level);
            //MessageHud.instance.QueueUnlockMsg(station.m_icon, "$msg_newstation", station.m_name);
            //Gogan.LogEvent("Game", "StationFound", station.m_name, 0L);
            this.UpdateKnownRecipesList();
        }

        private bool KnowStationLevel(string name, int level)
        {
            int num;
            return this.m_knownStations.TryGetValue(name, out num) && num >= level;
        }

        public void AddKnownText(string label, string text)
        {
            if (label.Length == 0)
            {
                ZLog.LogWarning("Text " + text + " Is missing label");
                return;
            }
            if (!this.m_knownTexts.ContainsKey(label))
            {
                this.m_knownTexts.Add(label, text);
                //this.Message(MessageHud.MessageType.TopLeft, Localization.instance.Localize("$msg_newtext", new string[]
                //{
                //label
                //}), 0, this.m_textIcon);
            }
        }

        public List<KeyValuePair<string, string>> GetKnownTexts()
        {
            return this.m_knownTexts.ToList<KeyValuePair<string, string>>();
        }

        public void AddKnownItem(ItemDrop.ItemData item)
        {
            if (item.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Trophie)
            {
                this.AddTrophie(item);
            }
            if (!this.m_knownMaterial.Contains(item.m_shared.m_name))
            {
                this.m_knownMaterial.Add(item.m_shared.m_name);
                if (item.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Material)
                {
                    MessageHud.instance.QueueUnlockMsg(item.GetIcon(), "$msg_newmaterial", item.m_shared.m_name);
                }
                else if (item.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Trophie)
                {
                    MessageHud.instance.QueueUnlockMsg(item.GetIcon(), "$msg_newtrophy", item.m_shared.m_name);
                }
                else
                {
                    MessageHud.instance.QueueUnlockMsg(item.GetIcon(), "$msg_newitem", item.m_shared.m_name);
                }
                Gogan.LogEvent("Game", "ItemFound", item.m_shared.m_name, 0L);
                this.UpdateKnownRecipesList();
            }
        }

        private void AddTrophie(ItemDrop.ItemData item)
        {
            if (item.m_shared.m_itemType != ItemDrop.ItemData.ItemType.Trophie)
            {
                return;
            }
            if (!this.m_trophies.Contains(item.m_dropPrefab.name))
            {
                this.m_trophies.Add(item.m_dropPrefab.name);
            }
        }

        public List<string> GetTrophies()
        {
            List<string> list = new List<string>();
            list.AddRange(this.m_trophies);
            return list;
        }

        private void UpdateKnownRecipesList()
        {
            if (Game.instance == null)
            {
                return;
            }
            foreach (Recipe recipe in ObjectDB.instance.m_recipes)
            {
                if (recipe.m_enabled && !this.m_knownRecipes.Contains(recipe.m_item.m_itemData.m_shared.m_name) && this.HaveRequirements(recipe, true, 0))
                {
                    this.AddKnownRecipe(recipe);
                }
            }
            this.m_tempOwnedPieceTables.Clear();
            m_humanoid.m_inventory.GetAllPieceTables(this.m_tempOwnedPieceTables);
            bool flag = false;
            foreach (PieceTable pieceTable in this.m_tempOwnedPieceTables)
            {
                foreach (GameObject gameObject in pieceTable.m_pieces)
                {
                    Piece component = gameObject.GetComponent<Piece>();
                    if (component.m_enabled && !this.m_knownRecipes.Contains(component.m_name) && this.HaveRequirements(component, Player.RequirementMode.IsKnown))
                    {
                        this.AddKnownPiece(component);
                        flag = true;
                    }
                }
            }
            if (flag)
            {
                this.UpdateAvailablePiecesList();
            }
        }

        static public void PieceTable_UpdateAvailable(PieceTable self, HashSet<string> knownRecipies, NPC npc, bool hideUnavailable, bool noPlacementCost)
        {
            if (self.m_availablePieces.Count == 0)
            {
                for (int i = 0; i < 4; i++)
                {
                    self.m_availablePieces.Add(new List<Piece>());
                }
            }
            foreach (List<Piece> list in self.m_availablePieces)
            {
                list.Clear();
            }
            foreach (GameObject gameObject in self.m_pieces)
            {
                Piece component = gameObject.GetComponent<Piece>();
                if (noPlacementCost || (knownRecipies.Contains(component.m_name) && component.m_enabled && (!hideUnavailable || npc.HaveRequirements(component, Player.RequirementMode.CanAlmostBuild))))
                {
                    if (component.m_category == Piece.PieceCategory.All)
                    {
                        for (int j = 0; j < 4; j++)
                        {
                            self.m_availablePieces[j].Add(component);
                        }
                    }
                    else
                    {
                        self.m_availablePieces[(int)component.m_category].Add(component);
                    }
                }
            }
        }

        private void UpdateAvailablePiecesList()
        {
            if (this.m_buildPieces != null)
            {
                PieceTable_UpdateAvailable(m_buildPieces, this.m_knownRecipes, this, this.m_hideUnavailable, this.m_noPlacementCost);
            }
            this.SetupPlacementGhost();
        }

        public static NPC GetNPC(long npcID)
        {
            foreach (NPC npc in NPC.s_npcs)
            {
                if (npc.GetNPCID() == npcID)
                {
                    return npc;
                }
            }
            return null;
        }

        public static NPC GetClosestNPC(Vector3 point, float maxRange)
        {
            NPC result = null;
            float num = 999999f;
            foreach (NPC npc in NPC.s_npcs)
            {
                float num2 = Vector3.Distance(npc.transform.position, point);
                if (num2 < num && num2 < maxRange)
                {
                    num = num2;
                    result = npc;
                }
            }
            return result;
        }

        public static bool IsNPCInRange(Vector3 point, float range, long npcID)
        {
            foreach (NPC npc in NPC.s_npcs)
            {
                if (npc.GetNPCID() == npcID)
                {
                    return Utils.DistanceXZ(npc.transform.position, point) < range;
                }
            }
            return false;
        }

        public static NPC GetPlayerNoiseRange(Vector3 point, float noiseRangeScale = 1f)
        {
            foreach (NPC npc in NPC.s_npcs)
            {
                float num = Vector3.Distance(npc.transform.position, point);
                float noiseRange = npc.m_humanoid.GetNoiseRange();
                if (num < noiseRange * noiseRangeScale)
                {
                    return npc;
                }
            }
            return null;
        }

        public static NPC GetRandomPlayer()
        {
            if (NPC.s_npcs.Count == 0)
            {
                return null;
            }
            return NPC.s_npcs[UnityEngine.Random.Range(0, NPC.s_npcs.Count)];
        }

        public void GetAvailableRecipes(ref List<Recipe> available)
        {
            available.Clear();
            foreach (Recipe recipe in ObjectDB.instance.m_recipes)
            {
                if (recipe.m_enabled && (recipe.m_item.m_itemData.m_shared.m_dlc.Length <= 0 || DLCMan.instance.IsDLCInstalled(recipe.m_item.m_itemData.m_shared.m_dlc)) && (this.m_knownRecipes.Contains(recipe.m_item.m_itemData.m_shared.m_name) || this.m_noPlacementCost) && (this.RequiredCraftingStation(recipe, 1, false) || this.m_noPlacementCost))
                {
                    available.Add(recipe);
                }
            }
        }

        private void OnInventoryChanged()
        {
            if (this.m_isLoading)
            {
                return;
            }
            foreach (ItemDrop.ItemData itemData in m_humanoid.m_inventory.GetAllItems())
            {
                this.AddKnownItem(itemData);
                if (itemData.m_shared.m_name == "$item_hammer")
                {
                    //this.ShowTutorial("hammer", false);
                }
                else if (itemData.m_shared.m_name == "$item_hoe")
                {
                    //this.ShowTutorial("hoe", false);
                }
                else if (itemData.m_shared.m_name == "$item_pickaxe_antler")
                {
                    //this.ShowTutorial("pickaxe", false);
                }
                if (itemData.m_shared.m_name == "$item_trophy_eikthyr")
                {
                    //this.ShowTutorial("boss_trophy", false);
                }
                if (itemData.m_shared.m_name == "$item_wishbone")
                {
                    //this.ShowTutorial("wishbone", false);
                }
                else if (itemData.m_shared.m_name == "$item_copperore" || itemData.m_shared.m_name == "$item_tinore")
                {
                    //this.ShowTutorial("ore", false);
                }
                else if (itemData.m_shared.m_food > 0f)
                {
                    //this.ShowTutorial("food", false);
                }
            }
            this.UpdateKnownRecipesList();
            this.UpdateAvailablePiecesList();
        }

        public Ragdoll GetRagdoll()
        {
            return this.m_ragdoll;
        }

        public void OnDodgeMortal()
        {
            m_dodgeInvincible = false;
        }


        private void UpdateDodge(float dt)
        {
            this.m_queuedDodgeTimer -= dt;
            if (this.m_queuedDodgeTimer > 0f && m_humanoid.IsOnGround() && !m_humanoid.IsDead() && !m_humanoid.InAttack() && !this.IsEncumbered() && !this.InDodge())
            {
                float num = this.m_dodgeStaminaUsage - this.m_dodgeStaminaUsage * this.m_equipmentMovementModifier;
                if (this.HaveStamina(num))
                {
                    m_humanoid.AbortEquipQueue();
                    this.m_queuedDodgeTimer = 0f;
                    this.m_dodgeInvincible = true;
                    m_humanoid.transform.rotation = Quaternion.LookRotation(this.m_queuedDodgeDir);
                    this.m_body.rotation = m_humanoid.transform.rotation;
                    m_humanoid.m_zanim.SetTrigger("dodge");
                    m_humanoid.AddNoise(5f);
                    m_humanoid.UseStamina(num);
                    this.m_dodgeEffects.Create(m_humanoid.transform.position, Quaternion.identity, m_humanoid.transform, 1f);
                }
                else
                {
                    //TODO: if there's a nearby player tell them you're out of stamina somehow....
                    Say("I am too tired to dodge!");
                    //Hud.instance.StaminaBarNoStaminaFlash();
                }
            }
            AnimatorStateInfo currentAnimatorStateInfo = m_humanoid.m_animator.GetCurrentAnimatorStateInfo(0);
            AnimatorStateInfo nextAnimatorStateInfo = m_humanoid.m_animator.GetNextAnimatorStateInfo(0);
            bool flag = m_humanoid.m_animator.IsInTransition(0);
            bool flag2 = m_humanoid.m_animator.GetBool("dodge") || (currentAnimatorStateInfo.tagHash == Player.m_animatorTagDodge && !flag) || (flag && nextAnimatorStateInfo.tagHash == Player.m_animatorTagDodge);
            bool value = flag2 && this.m_dodgeInvincible;
            this.m_nview.GetZDO().Set("dodgeinv", value);
            this.m_inDodge = flag2;
        }

        public virtual bool IsDodgeInvincible()
        {
            return this.m_nview.IsValid() && this.m_nview.GetZDO().GetBool("dodgeinv", false);
        }

        public virtual bool InDodge()
        {
            return this.m_nview.IsValid() && this.m_nview.IsOwner() && this.m_inDodge;
        }

        public virtual bool IsDead()
        {
            ZDO zdo = this.m_nview.GetZDO();
            return zdo != null && zdo.GetBool("dead", false);
        }

        public void Dodge(Vector3 dodgeDir)
        {
            this.m_queuedDodgeTimer = 0.5f;
            this.m_queuedDodgeDir = dodgeDir;
        }

        public bool TeleportTo(Vector3 pos, Quaternion rot, bool distantTeleport)
        {
            if (this.IsTeleporting())
            {
                return false;
            }
            if (this.m_teleportCooldown < 2f)
            {
                return false;
            }
            this.m_teleporting = true;
            this.m_distantTeleport = distantTeleport;
            this.m_teleportTimer = 0f;
            this.m_teleportCooldown = 0f;
            this.m_teleportFromPos = m_humanoid.transform.position;
            this.m_teleportFromRot = m_humanoid.transform.rotation;
            this.m_teleportTargetPos = pos;
            this.m_teleportTargetRot = rot;
            return true;
        }

        private void UpdateTeleport(float dt)
        {
            if (!this.m_teleporting)
            {
                this.m_teleportCooldown += dt;
                return;
            }
            this.m_teleportCooldown = 0f;
            this.m_teleportTimer += dt;
            if (this.m_teleportTimer > 2f)
            {
                Vector3 lookDir = this.m_teleportTargetRot * Vector3.forward;
                m_humanoid.transform.position = this.m_teleportTargetPos;
                m_humanoid.transform.rotation = this.m_teleportTargetRot;
                this.m_body.velocity = Vector3.zero;
                m_character.m_maxAirAltitude = m_humanoid.transform.position.y;
                m_character.SetLookDir(lookDir);
                if ((this.m_teleportTimer > 8f || !this.m_distantTeleport) && OnTeleport_IsAreaReady(this.m_teleportTargetPos))
                {
                    float num = 0f;
                    if (ZoneSystem.instance.FindFloor(this.m_teleportTargetPos, out num))
                    {
                        this.m_teleportTimer = 0f;
                        this.m_teleporting = false;
                        m_character.ResetCloth();
                        return;
                    }
                    if (this.m_teleportTimer > 15f || !this.m_distantTeleport)
                    {
                        if (this.m_distantTeleport)
                        {
                            Vector3 position = m_humanoid.transform.position;
                            position.y = ZoneSystem.instance.GetSolidHeight(this.m_teleportTargetPos) + 0.5f;
                            m_humanoid.transform.position = position;
                        }
                        else
                        {
                            m_humanoid.transform.rotation = this.m_teleportFromRot;
                            m_humanoid.transform.position = this.m_teleportFromPos;
                            m_character.m_maxAirAltitude = m_humanoid.transform.position.y;
                            //Debug.Log(MessageHud.MessageType.Center, " $msg_portal_blocked", 0, null);
                        }
                        this.m_teleportTimer = 0f;
                        this.m_teleporting = false;
                        m_character.ResetCloth();
                    }
                }
            }
        }

        public virtual bool IsTeleporting()
        {
            return this.m_teleporting;
        }

        public void SetPlayerModel(int index)
        {
            if (this.m_modelIndex == index)
            {
                return;
            }
            this.m_modelIndex = index;
            this.m_visEquipment.SetModel(index);
        }

        public int GetPlayerModel()
        {
            return this.m_modelIndex;
        }

        public void SetSkinColor(Vector3 color)
        {
            if (color == this.m_skinColor)
            {
                return;
            }
            this.m_skinColor = color;
            this.m_visEquipment.SetSkinColor(this.m_skinColor);
        }

        public void SetHairColor(Vector3 color)
        {
            if (this.m_hairColor == color)
            {
                return;
            }
            this.m_hairColor = color;
            this.m_visEquipment.SetHairColor(this.m_hairColor);
        }

        public void SetPlayerFaction()
        {
            if (m_character != null)
            {
                m_character.m_faction = Character.Faction.Players;
            }
        }

        public bool OnTeleport_IsAreaReady(Vector3 point)
        {
            Vector2i zone = ZoneSystem.instance.GetZone(point);
            if (!ZoneSystem.instance.IsZoneLoaded(zone))
            {
                return false;
            }
            ZNetScene.instance.m_tempCurrentObjects.Clear();
            ZDOMan.instance.FindSectorObjects(zone, npz.m_activeAreaRadius, 0, ZNetScene.instance.m_tempCurrentObjects, null);
            foreach (ZDO zdo in ZNetScene.instance.m_tempCurrentObjects)
            {
                if (ZNetScene.instance.IsPrefabZDOValid(zdo) && !ZNetScene.instance.FindInstance(zdo))
                {
                    return false;
                }
            }
            return true;
        }

        public void HearTalking(string words)
        {
            //TODO: process hearing words
            TEMP_lastHeardString = words;
            Say(words);
        }

        public void Say(string words)
        {
            //say words...
            Chat.instance.SetNpcText(m_humanoid.gameObject, Vector3.up * 1.5f, 20f, this.m_hideDialogDelay, "", words, false);

            //TODO: look up the animator triggers for different conversation animations...
            if (sayTrigger.Length > 0)
            {
                m_character.m_animator.SetTrigger(sayTrigger);
            }
        }

        //public void OnInventoryChanged()
        //{
        //    //TODO:
        //    //if (this.m_isLoading)
        //    //{
        //    //    return;
        //    //}
        //    //foreach (ItemDrop.ItemData itemData in m_humanoid.m_inventory.GetAllItems())
        //    //{
        //    //    this.AddKnownItem(itemData);
        //    //    if (itemData.m_shared.m_name == "$item_hammer")
        //    //    {
        //    //        this.ShowTutorial("hammer", false);
        //    //    }
        //    //    else if (itemData.m_shared.m_name == "$item_hoe")
        //    //    {
        //    //        this.ShowTutorial("hoe", false);
        //    //    }
        //    //    else if (itemData.m_shared.m_name == "$item_pickaxe_antler")
        //    //    {
        //    //        this.ShowTutorial("pickaxe", false);
        //    //    }
        //    //    if (itemData.m_shared.m_name == "$item_trophy_eikthyr")
        //    //    {
        //    //        this.ShowTutorial("boss_trophy", false);
        //    //    }
        //    //    if (itemData.m_shared.m_name == "$item_wishbone")
        //    //    {
        //    //        this.ShowTutorial("wishbone", false);
        //    //    }
        //    //    else if (itemData.m_shared.m_name == "$item_copperore" || itemData.m_shared.m_name == "$item_tinore")
        //    //    {
        //    //        this.ShowTutorial("ore", false);
        //    //    }
        //    //    else if (itemData.m_shared.m_food > 0f)
        //    //    {
        //    //        this.ShowTutorial("food", false);
        //    //    }
        //    //}
        //    //this.UpdateKnownRecipesList();
        //    //this.UpdateAvailablePiecesList();
        //}


        //public void SetPlayerModel(int index)
        //{
        //    if (this.m_modelIndex == index)
        //    {
        //        return;
        //    }
        //    this.m_modelIndex = index;
        //    m_humanoid.m_visEquipment.SetModel(index);
        //}

        //public int GetPlayerModel()
        //{
        //    return this.m_modelIndex;
        //}

        //public void SetSkinColor(Vector3 color)
        //{
        //    if (color == this.m_skinColor)
        //    {
        //        return;
        //    }
        //    this.m_skinColor = color;
        //    m_humanoid.m_visEquipment.SetSkinColor(this.m_skinColor);
        //}

        //public void SetHairColor(Vector3 color)
        //{
        //    if (this.m_hairColor == color)
        //    {
        //        return;
        //    }
        //    this.m_hairColor = color;
        //    m_humanoid.m_visEquipment.SetHairColor(this.m_hairColor);
        //}

        public void SetupVisEquipment(VisEquipment visEq, bool isRagdoll)
        {
            m_humanoid.SetupVisEquipment(visEq, isRagdoll);
            visEq.SetModel(this.m_modelIndex);
            visEq.SetSkinColor(this.m_skinColor);
            visEq.SetHairColor(this.m_hairColor);
        }

        public bool CanConsumeItem(ItemDrop.ItemData item)
        {
            if (item.m_shared.m_itemType != ItemDrop.ItemData.ItemType.Consumable)
            {
                return false;
            }
            if (item.m_shared.m_food > 0f && !this.CanEat(item))
            {
                return false;
            }
            if (item.m_shared.m_consumeStatusEffect)
            {
                StatusEffect consumeStatusEffect = item.m_shared.m_consumeStatusEffect;
                if (m_humanoid.m_seman.HaveStatusEffect(item.m_shared.m_consumeStatusEffect.name) || m_humanoid.m_seman.HaveStatusEffectCategory(consumeStatusEffect.m_category))
                {
                    //this.Message(MessageHud.MessageType.Center, "$msg_cantconsume", 0, null);
                    return false;
                }
            }
            return true;
        }

        public bool ConsumeItem(Inventory inventory, ItemDrop.ItemData item)
        {
            if (!this.CanConsumeItem(item))
            {
                return false;
            }
            if (item.m_shared.m_consumeStatusEffect)
            {
                StatusEffect consumeStatusEffect = item.m_shared.m_consumeStatusEffect;
                m_humanoid.m_seman.AddStatusEffect(item.m_shared.m_consumeStatusEffect, true);
            }
            if (item.m_shared.m_food > 0f)
            {
                EatFood(item);
            }
            inventory.RemoveOneItem(item);
            return true;
        }

        public virtual bool InCutscene()
        {
            return Player.m_localPlayer != null && Player.m_localPlayer.InCutscene();
        }

        public void SetMaxStamina(float stamina)
        {
            this.m_maxStamina = stamina;
            this.m_stamina = Mathf.Clamp(this.m_stamina, 0f, this.m_maxStamina);
        }

        public void SetMaxHealth(float health)
        {
            m_humanoid.SetMaxHealth(health);
        }

        public void StartEmote(string emote, bool oneshot = true)
        {
            if (!this.CanMove() || this.InAttack() || m_humanoid.IsHoldingAttack())
            {
                return;
            }
            this.SetCrouch(false);
            int @int = this.m_nview.GetZDO().GetInt("emoteID", 0);
            this.m_nview.GetZDO().Set("emoteID", @int + 1);
            this.m_nview.GetZDO().Set("emote", emote);
            this.m_nview.GetZDO().Set("emote_oneshot", oneshot);
        }

        protected virtual void StopEmote()
        {
            if (this.m_nview.GetZDO().GetString("emote", "") != "")
            {
                int @int = this.m_nview.GetZDO().GetInt("emoteID", 0);
                this.m_nview.GetZDO().Set("emoteID", @int + 1);
                this.m_nview.GetZDO().Set("emote", "");
            }
        }

        private void UpdateEmote()
        {
            if (this.m_nview.IsOwner() && this.InEmote() && m_humanoid.m_moveDir != Vector3.zero)
            {
                this.StopEmote();
            }
            int @int = this.m_nview.GetZDO().GetInt("emoteID", 0);
            if (@int != this.m_emoteID)
            {
                this.m_emoteID = @int;
                if (!string.IsNullOrEmpty(this.m_emoteState))
                {
                    this.m_animator.SetBool("emote_" + this.m_emoteState, false);
                }
                this.m_emoteState = "";
                this.m_animator.SetTrigger("emote_stop");
                string @string = this.m_nview.GetZDO().GetString("emote", "");
                if (!string.IsNullOrEmpty(@string))
                {
                    bool @bool = this.m_nview.GetZDO().GetBool("emote_oneshot", false);
                    this.m_animator.ResetTrigger("emote_stop");
                    if (@bool)
                    {
                        this.m_animator.SetTrigger("emote_" + @string);
                        return;
                    }
                    this.m_emoteState = @string;
                    this.m_animator.SetBool("emote_" + @string, true);
                }
            }
        }

        public virtual bool InEmote()
        {
            return !string.IsNullOrEmpty(this.m_emoteState) || this.m_animator.GetCurrentAnimatorStateInfo(0).tagHash == Player.m_animatorTagEmote;
        }

        public virtual bool IsCrouching()
        {
            return this.m_animator.GetCurrentAnimatorStateInfo(0).tagHash == Player.m_animatorTagCrouch;
        }

        private void UpdateCrouch(float dt)
        {
            if (this.m_crouchToggled)
            {
                if (!this.HaveStamina(0f) || m_humanoid.IsSwiming() || this.InBed() || this.InPlaceMode() || m_humanoid.m_run || m_humanoid.IsBlocking() || m_humanoid.IsFlying())
                {
                    this.SetCrouch(false);
                }
                bool flag = this.InAttack() || m_humanoid.IsHoldingAttack();
                m_humanoid.m_zanim.SetBool(Player.crouching, this.m_crouchToggled && !flag);
                return;
            }
            m_humanoid.m_zanim.SetBool(Player.crouching, false);
        }

        protected virtual void SetCrouch(bool crouch)
        {
            if (this.m_crouchToggled == crouch)
            {
                return;
            }
            this.m_crouchToggled = crouch;
        }


        public void SetGuardianPower(string name)
        {
            this.m_guardianPower = name;
            this.m_guardianSE = ObjectDB.instance.GetStatusEffect(this.m_guardianPower);
        }

        public string GetGuardianPowerName()
        {
            return this.m_guardianPower;
        }

        public void GetGuardianPowerHUD(out StatusEffect se, out float cooldown)
        {
            se = this.m_guardianSE;
            cooldown = this.m_guardianPowerCooldown;
        }

        //TODO: hook this up
        public bool StartGuardianPower()
        {
            if (this.m_guardianSE == null)
            {
                return false;
            }
            if ((m_humanoid.InAttack() && !m_humanoid.HaveQueuedChain()) || m_humanoid.InDodge() || !this.CanMove() || m_humanoid.IsKnockedBack() || m_humanoid.IsStaggering() || m_humanoid.InMinorAction())
            {
                return false;
            }
            if (this.m_guardianPowerCooldown > 0f)
            {
                //this.Message(MessageHud.MessageType.Center, "$hud_powernotready", 0, null);
                return false;
            }
            m_humanoid.m_zanim.SetTrigger("gpower");
            return true;
        }

        public bool ActivateGuardianPower()
        {
            if (m_guardianPowerCooldown > 0f)
            {
                return false;
            }
            if (m_guardianSE == null)
            {
                return false;
            }
            {
                List<Player> list = new List<Player>();
                Player.GetPlayersInRange(m_humanoid.transform.position, guardianPowerRange, list);
                foreach (Player player in list)
                {
                    player.GetSEMan().AddStatusEffect(m_guardianSE.name, true);
                }
            }
            {
                List<NPC> list = new List<NPC>();
                NPC.GetFriendlyNPCsInRange(m_humanoid.transform.position, guardianPowerRange, list);
                foreach (NPC npc in list)
                {
                    npc.m_humanoid.GetSEMan().AddStatusEffect(m_guardianSE.name, true);
                }
            }
            m_guardianPowerCooldown = m_guardianSE.m_cooldown;
            return false;
        }

        private void UpdateGuardianPower(float dt)
        {
            this.m_guardianPowerCooldown -= dt;
            if (this.m_guardianPowerCooldown < 0f)
            {
                this.m_guardianPowerCooldown = 0f;
            }
        }

        public virtual void AttachStart(Transform attachPoint, bool hideWeapons, bool isBed, string attachAnimation, Vector3 detachOffset)
        {
            if (this.m_attached)
            {
                return;
            }
            this.m_attached = true;
            this.m_attachPoint = attachPoint;
            this.m_detachOffset = detachOffset;
            this.m_attachAnimation = attachAnimation;
            m_humanoid.m_zanim.SetBool(attachAnimation, true);
            this.m_nview.GetZDO().Set("inBed", isBed);
            if (hideWeapons)
            {
                m_humanoid.HideHandItems();
            }
            m_humanoid.ResetCloth();
        }

        private void UpdateAttach()
        {
            if (this.m_attached)
            {
                if (this.m_attachPoint != null)
                {
                    m_humanoid.transform.position = this.m_attachPoint.position;
                    m_humanoid.transform.rotation = this.m_attachPoint.rotation;
                    Rigidbody componentInParent = this.m_attachPoint.GetComponentInParent<Rigidbody>();
                    this.m_body.useGravity = false;
                    this.m_body.velocity = (componentInParent ? componentInParent.GetPointVelocity(m_humanoid.transform.position) : Vector3.zero);
                    this.m_body.angularVelocity = Vector3.zero;
                    m_humanoid.m_maxAirAltitude = m_humanoid.transform.position.y;
                    return;
                }
                this.AttachStop();
            }
        }

        public virtual bool IsAttached()
        {
            return this.m_attached;
        }

        public virtual bool InBed()
        {
            return this.m_nview.IsValid() && this.m_nview.GetZDO().GetBool("inBed", false);
        }

        public virtual void AttachStop()
        {
            if (this.m_sleeping)
            {
                return;
            }
            if (this.m_attached)
            {
                if (this.m_attachPoint != null)
                {
                    m_humanoid.transform.position = this.m_attachPoint.TransformPoint(this.m_detachOffset);
                }
                this.m_body.useGravity = true;
                this.m_attached = false;
                this.m_attachPoint = null;
                m_humanoid.m_zanim.SetBool(this.m_attachAnimation, false);
                this.m_nview.GetZDO().Set("inBed", false);
                m_humanoid.ResetCloth();
            }
        }

        public void StartShipControl(ShipControlls shipControl)
        {
            this.m_shipControl = shipControl;
            ZLog.Log("ship controlls set " + shipControl.GetShip().gameObject.name);
        }

        public static void ShipControlls_OnUseStop(ShipControlls self, NPC npc)
        {
            if (!self.m_nview.IsValid())
            {
                return;
            }
            self.m_nview.InvokeRPC("ReleaseControl", new object[]
            {
            npc.m_humanoid.GetZDOID()
            });
            if (self.m_attachPoint != null)
            {
                npc.AttachStop();
            }
        }

        public void StopShipControl()
        {
            if (this.m_shipControl != null)
            {
                if (this.m_shipControl)
                {
                    ShipControlls_OnUseStop(m_shipControl, this);
                }
                ZLog.Log("Stop ship controlls");
                this.m_shipControl = null;
            }
        }

        private void SetShipControl(ref Vector3 moveDir)
        {
            this.m_shipControl.GetShip().ApplyMovementControlls(moveDir);
            moveDir = Vector3.zero;
        }

        public Ship GetControlledShip()
        {
            if (this.m_shipControl)
            {
                return this.m_shipControl.GetShip();
            }
            return null;
        }

        public ShipControlls GetShipControl()
        {
            return this.m_shipControl;
        }

        private void UpdateShipControl(float dt)
        {
            if (!this.m_shipControl)
            {
                return;
            }
            Vector3 forward = this.m_shipControl.GetShip().transform.forward;
            forward.y = 0f;
            forward.Normalize();
            Quaternion to = Quaternion.LookRotation(forward);
            m_humanoid.transform.rotation = Quaternion.RotateTowards(m_humanoid.transform.rotation, to, 100f * dt);
            if (Vector3.Distance(this.m_shipControl.transform.position, m_humanoid.transform.position) > this.m_maxInteractDistance)
            {
                this.StopShipControl();
            }
        }

        public bool IsSleeping()
        {
            return this.m_sleeping;
        }

        public void SetSleeping(bool sleep)
        {
            if (this.m_sleeping == sleep)
            {
                return;
            }
            this.m_sleeping = sleep;
            if (!sleep)
            {
                //this.Message(MessageHud.MessageType.Center, "$msg_goodmorning", 0, null);
                m_humanoid.m_seman.AddStatusEffect("Rested", true);
            }
        }

        //public void SetControls(Vector3 movedir, bool attack, bool attackHold, bool secondaryAttack, bool block, bool blockHold, bool jump, bool crouch, bool run, bool autoRun)
        //{
        //    if ((movedir != Vector3.zero || attack || secondaryAttack || block || blockHold || jump || crouch) && this.GetControlledShip() == null)
        //    {
        //        this.StopEmote();
        //        this.AttachStop();
        //    }
        //    if (this.m_shipControl)
        //    {
        //        this.SetShipControl(ref movedir);
        //        if (jump)
        //        {
        //            this.StopShipControl();
        //        }
        //    }
        //    if (run)
        //    {
        //        m_humanoid.m_walk = false;
        //    }
        //    //if (!this.m_autoRun)
        //    //{
        //    //    Vector3 lookDir = this.m_lookDir;
        //    //    lookDir.y = 0f;
        //    //    lookDir.Normalize();
        //    //    m_humanoid.m_moveDir = movedir.z * lookDir + movedir.x * Vector3.Cross(Vector3.up, lookDir);
        //    //}
        //    //if (!this.m_autoRun && autoRun && !this.InPlaceMode())
        //    //{
        //    //    this.m_autoRun = true;
        //    //    this.SetCrouch(false);
        //    //    m_humanoid.m_moveDir = this.m_lookDir;
        //    //    m_humanoid.m_moveDir.y = 0f;
        //    //    m_humanoid.m_moveDir.Normalize();
        //    //}
        //    //else if (this.m_autoRun)
        //    //{
        //    //    if (attack || jump || crouch || movedir != Vector3.zero || this.InPlaceMode() || attackHold)
        //    //    {
        //    //        this.m_autoRun = false;
        //    //    }
        //    //    else if (autoRun || blockHold)
        //    //    {
        //    //        m_humanoid.m_moveDir = this.m_lookDir;
        //    //        m_humanoid.m_moveDir.y = 0f;
        //    //        m_humanoid.m_moveDir.Normalize();
        //    //        blockHold = false;
        //    //        block = false;
        //    //    }
        //    //}
        //    this.m_attack = attack;
        //    this.m_attackDraw = attackHold;
        //    this.m_secondaryAttack = secondaryAttack;
        //    this.m_blocking = blockHold;
        //    this.m_run = run;
        //    if (crouch)
        //    {
        //        this.SetCrouch(!this.m_crouchToggled);
        //    }
        //    if (jump)
        //    {
        //        if (this.m_blocking)
        //        {
        //            Vector3 dodgeDir = m_humanoid.m_moveDir;
        //            if (dodgeDir.magnitude < 0.1f)
        //            {
        //                dodgeDir = -this.m_lookDir;
        //                dodgeDir.y = 0f;
        //                dodgeDir.Normalize();
        //            }
        //            this.Dodge(dodgeDir);
        //            return;
        //        }
        //        if (this.IsCrouching() || this.m_crouchToggled)
        //        {
        //            Vector3 dodgeDir2 = m_humanoid.m_moveDir;
        //            if (dodgeDir2.magnitude < 0.1f)
        //            {
        //                dodgeDir2 = this.m_lookDir;
        //                dodgeDir2.y = 0f;
        //                dodgeDir2.Normalize();
        //            }
        //            this.Dodge(dodgeDir2);
        //            return;
        //        }
        //        m_humanoid.Jump();
        //    }
        //}

        private void UpdateTargeted(float dt)
        {
            this.m_timeSinceTargeted += dt;
            this.m_timeSinceSensed += dt;
        }
        public virtual void OnTargeted(bool sensed, bool alerted)
        {
            if (sensed)
            {
                if (this.m_timeSinceSensed > 0.5f)
                {
                    this.m_timeSinceSensed = 0f;
                    this.m_nview.InvokeRPC("OnTargeted", new object[]
                    {
                    sensed,
                    alerted
                    });
                    return;
                }
            }
            else if (this.m_timeSinceTargeted > 0.5f)
            {
                this.m_timeSinceTargeted = 0f;
                this.m_nview.InvokeRPC("OnTargeted", new object[]
                {
                sensed,
                alerted
                });
            }
        }

        private void RPC_OnTargeted(long sender, bool sensed, bool alerted)
        {
            this.m_timeSinceTargeted = 0f;
            if (sensed)
            {
                this.m_timeSinceSensed = 0f;
            }
            if (alerted)
            {
                MusicMan.instance.ResetCombatTimer();
            }
        }

        protected virtual void OnDamaged(HitData hit)
        {
            m_humanoid.OnDamaged(hit);
            Hud.instance.DamageFlash();
        }

        public bool IsTargeted()
        {
            return this.m_timeSinceTargeted < 1f;
        }

        public bool IsSensed()
        {
            return this.m_timeSinceSensed < 1f;
        }
        protected virtual void ApplyArmorDamageMods(ref HitData.DamageModifiers mods)
        {
            if (m_humanoid.m_chestItem != null)
            {
                mods.Apply(m_humanoid.m_chestItem.m_shared.m_damageModifiers);
            }
            if (m_humanoid.m_legItem != null)
            {
                mods.Apply(m_humanoid.m_legItem.m_shared.m_damageModifiers);
            }
            if (m_humanoid.m_helmetItem != null)
            {
                mods.Apply(m_humanoid.m_helmetItem.m_shared.m_damageModifiers);
            }
            if (m_humanoid.m_shoulderItem != null)
            {
                mods.Apply(m_humanoid.m_shoulderItem.m_shared.m_damageModifiers);
            }
        }

        public virtual float GetBodyArmor()
        {
            float num = 0f;
            if (m_humanoid.m_chestItem != null)
            {
                num += m_humanoid.m_chestItem.GetArmor();
            }
            if (m_humanoid.m_legItem != null)
            {
                num += m_humanoid.m_legItem.GetArmor();
            }
            if (m_humanoid.m_helmetItem != null)
            {
                num += m_humanoid.m_helmetItem.GetArmor();
            }
            if (m_humanoid.m_shoulderItem != null)
            {
                num += m_humanoid.m_shoulderItem.GetArmor();
            }
            return num;
        }

        protected virtual void OnSneaking(float dt)
        {
            float t = Mathf.Pow(this.m_skills.GetSkillFactor(Skills.SkillType.Sneak), 0.5f);
            float num = Mathf.Lerp(1f, 0.25f, t);
            this.UseStamina(dt * this.m_sneakStaminaDrain * num);
            if (!this.HaveStamina(0f))
            {
                Hud.instance.StaminaBarNoStaminaFlash();
            }
            this.m_sneakSkillImproveTimer += dt;
            if (this.m_sneakSkillImproveTimer > 1f)
            {
                this.m_sneakSkillImproveTimer = 0f;
                if (BaseAI.InStealthRange(m_humanoid))
                {
                    this.RaiseSkill(Skills.SkillType.Sneak, 1f);
                    return;
                }
                this.RaiseSkill(Skills.SkillType.Sneak, 0.1f);
            }
        }

        private void UpdateStealth(float dt)
        {
            this.m_stealthFactorUpdateTimer += dt;
            if (this.m_stealthFactorUpdateTimer > 0.5f)
            {
                this.m_stealthFactorUpdateTimer = 0f;
                this.m_stealthFactorTarget = 0f;
                if (this.IsCrouching())
                {
                    this.m_lastStealthPosition = m_humanoid.transform.position;
                    float skillFactor = this.m_skills.GetSkillFactor(Skills.SkillType.Sneak);
                    float lightFactor = StealthSystem.instance.GetLightFactor(m_humanoid.GetCenterPoint());
                    this.m_stealthFactorTarget = Mathf.Lerp(0.5f + lightFactor * 0.5f, 0.2f + lightFactor * 0.4f, skillFactor);
                    this.m_stealthFactorTarget = Mathf.Clamp01(this.m_stealthFactorTarget);
                    m_humanoid.m_seman.ModifyStealth(this.m_stealthFactorTarget, ref this.m_stealthFactorTarget);
                    this.m_stealthFactorTarget = Mathf.Clamp01(this.m_stealthFactorTarget);
                }
                else
                {
                    this.m_stealthFactorTarget = 1f;
                }
            }
            this.m_stealthFactor = Mathf.MoveTowards(this.m_stealthFactor, this.m_stealthFactorTarget, dt / 4f);
            this.m_nview.GetZDO().Set("Stealth", this.m_stealthFactor);
        }

        public virtual float GetStealthFactor()
        {
            if (!this.m_nview.IsValid())
            {
                return 0f;
            }
            if (this.m_nview.IsOwner())
            {
                return this.m_stealthFactor;
            }
            return this.m_nview.GetZDO().GetFloat("Stealth", 0f);
        }

        public virtual bool InAttack()
        {
            if (this.m_animator.IsInTransition(0))
            {
                return this.m_animator.GetNextAnimatorStateInfo(0).tagHash == Humanoid.m_animatorTagAttack || this.m_animator.GetNextAnimatorStateInfo(1).tagHash == Humanoid.m_animatorTagAttack;
            }
            return this.m_animator.GetCurrentAnimatorStateInfo(0).tagHash == Humanoid.m_animatorTagAttack || this.m_animator.GetCurrentAnimatorStateInfo(1).tagHash == Humanoid.m_animatorTagAttack;
        }

        public virtual float GetEquipmentMovementModifier()
        {
            return this.m_equipmentMovementModifier;
        }

        protected virtual float GetJogSpeedFactor()
        {
            return 1f + this.m_equipmentMovementModifier;
        }

        protected virtual float GetRunSpeedFactor()
        {
            float num = 1f;
            float skillFactor = this.m_skills.GetSkillFactor(Skills.SkillType.Run);
            return (num + skillFactor * 0.25f) * (1f + this.m_equipmentMovementModifier * 1.5f);
        }

        public virtual bool InMinorAction()
        {
            return (this.m_animator.IsInTransition(1) ? this.m_animator.GetNextAnimatorStateInfo(1) : this.m_animator.GetCurrentAnimatorStateInfo(1)).tagHash == Player.m_animatorTagMinorAction;
        }
        public virtual bool GetRelativePosition(out ZDOID parent, out Vector3 relativePos, out Vector3 relativeVel)
        {
            if (this.m_attached && this.m_attachPoint)
            {
                ZNetView componentInParent = this.m_attachPoint.GetComponentInParent<ZNetView>();
                if (componentInParent && componentInParent.IsValid())
                {
                    parent = componentInParent.GetZDO().m_uid;
                    relativePos = componentInParent.transform.InverseTransformPoint(m_humanoid.transform.position);
                    relativeVel = Vector3.zero;
                    return true;
                }
            }
            return m_humanoid.GetRelativePosition(out parent, out relativePos, out relativeVel);
        }

        public virtual Skills GetSkills()
        {
            return this.m_skills;
        }

        public virtual float GetRandomSkillFactor(Skills.SkillType skill)
        {
            return this.m_skills.GetRandomSkillFactor(skill);
        }

        public virtual float GetSkillFactor(Skills.SkillType skill)
        {
            return this.m_skills.GetSkillFactor(skill);
        }

        protected virtual void DoDamageCameraShake(HitData hit)
        {
            if (GameCamera.instance && hit.GetTotalPhysicalDamage() > 0f)
            {
                float num = Mathf.Clamp01(hit.GetTotalPhysicalDamage() / m_humanoid.GetMaxHealth());
                GameCamera.instance.AddShake(m_humanoid.transform.position, 50f, this.m_baseCameraShake * num, false);
            }
        }

        protected virtual bool ToggleEquiped(ItemDrop.ItemData item)
        {
            if (!item.IsEquipable())
            {
                return false;
            }
            if (this.InAttack())
            {
                return true;
            }
            if (item.m_shared.m_equipDuration <= 0f)
            {
                if (m_humanoid.IsItemEquiped(item))
                {
                    m_humanoid.UnequipItem(item, true);
                }
                else
                {
                    m_humanoid.EquipItem(item, true);
                }
            }
            else if (m_humanoid.IsItemEquiped(item))
            {
                this.QueueUnequipItem(item);
            }
            else
            {
                this.QueueEquipItem(item);
            }
            return true;
        }

        public void GetActionProgress(out string name, out float progress)
        {
            if (this.m_equipQueue.Count > 0)
            {
                Player.EquipQueueData equipQueueData = this.m_equipQueue[0];
                if (equipQueueData.m_duration > 0.5f)
                {
                    if (equipQueueData.m_equip)
                    {
                        name = "$hud_equipping " + equipQueueData.m_item.m_shared.m_name;
                    }
                    else
                    {
                        name = "$hud_unequipping " + equipQueueData.m_item.m_shared.m_name;
                    }
                    progress = Mathf.Clamp01(equipQueueData.m_time / equipQueueData.m_duration);
                    return;
                }
            }
            name = null;
            progress = 0f;
        }

        private void UpdateEquipQueue(float dt)
        {
            if (this.m_equipQueuePause > 0f)
            {
                this.m_equipQueuePause -= dt;
                m_humanoid.m_zanim.SetBool("equipping", false);
                return;
            }
            m_humanoid.m_zanim.SetBool("equipping", this.m_equipQueue.Count > 0);
            if (this.m_equipQueue.Count == 0)
            {
                return;
            }
            Player.EquipQueueData equipQueueData = this.m_equipQueue[0];
            if (equipQueueData.m_time == 0f && equipQueueData.m_duration >= 1f)
            {
                this.m_equipStartEffects.Create(m_humanoid.transform.position, Quaternion.identity, null, 1f);
            }
            equipQueueData.m_time += dt;
            if (equipQueueData.m_time > equipQueueData.m_duration)
            {
                this.m_equipQueue.RemoveAt(0);
                if (equipQueueData.m_equip)
                {
                    m_humanoid.EquipItem(equipQueueData.m_item, true);
                }
                else
                {
                    m_humanoid.UnequipItem(equipQueueData.m_item, true);
                }
                this.m_equipQueuePause = 0.3f;
            }
        }

        private void QueueEquipItem(ItemDrop.ItemData item)
        {
            if (item == null)
            {
                return;
            }
            if (this.IsItemQueued(item))
            {
                this.RemoveFromEquipQueue(item);
                return;
            }
            Player.EquipQueueData equipQueueData = new Player.EquipQueueData();
            equipQueueData.m_item = item;
            equipQueueData.m_equip = true;
            equipQueueData.m_duration = item.m_shared.m_equipDuration;
            this.m_equipQueue.Add(equipQueueData);
        }

        private void QueueUnequipItem(ItemDrop.ItemData item)
        {
            if (item == null)
            {
                return;
            }
            if (this.IsItemQueued(item))
            {
                this.RemoveFromEquipQueue(item);
                return;
            }
            Player.EquipQueueData equipQueueData = new Player.EquipQueueData();
            equipQueueData.m_item = item;
            equipQueueData.m_equip = false;
            equipQueueData.m_duration = item.m_shared.m_equipDuration;
            this.m_equipQueue.Add(equipQueueData);
        }

        public virtual void AbortEquipQueue()
        {
            this.m_equipQueue.Clear();
        }

        public virtual void RemoveFromEquipQueue(ItemDrop.ItemData item)
        {
            if (item == null)
            {
                return;
            }
            foreach (Player.EquipQueueData equipQueueData in this.m_equipQueue)
            {
                if (equipQueueData.m_item == item)
                {
                    this.m_equipQueue.Remove(equipQueueData);
                    break;
                }
            }
        }

        public bool IsItemQueued(ItemDrop.ItemData item)
        {
            if (item == null)
            {
                return false;
            }
            using (List<Player.EquipQueueData>.Enumerator enumerator = this.m_equipQueue.GetEnumerator())
            {
                while (enumerator.MoveNext())
                {
                    if (enumerator.Current.m_item == item)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        public void ResetCharacter()
        {
            this.m_guardianPowerCooldown = 0f;
            Player.ResetSeenTutorials();
            this.m_knownRecipes.Clear();
            this.m_knownStations.Clear();
            this.m_knownMaterial.Clear();
            this.m_uniques.Clear();
            this.m_trophies.Clear();
            this.m_skills.Clear();
            this.m_knownBiome.Clear();
            this.m_knownTexts.Clear();
        }



        public float GetMaxSenseRange(MonsterAI self)
        {
            float rangeCheck = Mathf.Max(self.m_viewRange, self.m_hearRange);
            return rangeCheck;
        }
        
        public float GetNearbyUpdateTargetTime()
        {
            return 3f;
        }

        public float GetDefaultUpdateTargetTime()
        {
            return 10f;
        }

        public void CheckForNearbyEnemies(MonsterAI self)
        {
            if (self.m_updateTargetTimer <= 0f && !self.m_character.InAttack())
            {
                float aggroRange = GetMaxSenseRange(self);
                self.m_updateTargetTimer = (Character.IsCharacterInRange(self.transform.position, aggroRange) ? GetNearbyUpdateTargetTime() : GetDefaultUpdateTargetTime());
                Character character = self.FindEnemy();
                if (character)
                {
                    self.m_targetCreature = character;
                    self.m_targetStatic = null;
                }

                if (self.m_targetCreature != null)
                {
                    self.m_havePathToTarget = self.HavePath(self.m_targetCreature.transform.position);
                }

                if (self.m_targetCreature && !self.m_havePathToTarget)
                {
                    //TODO: think about what to do in this case
                    self.m_targetCreature = null;
                    //StaticTarget staticTarget = self.FindClosestStaticPriorityTarget(99999f);
                    //if (staticTarget)
                    //{
                    //    self.m_targetStatic = staticTarget;
                    //    self.m_targetCreature = null;
                    //}
                    //if (self.m_targetStatic != null)
                    //{
                    //    self.m_havePathToTarget = self.HavePath(self.m_targetStatic.transform.position);
                    //}
                    //if ((!staticTarget || (self.m_targetStatic && !self.m_havePathToTarget)) && self.IsAlerted())
                    //{
                    //    StaticTarget staticTarget2 = self.FindRandomStaticTarget(10f, false);
                    //    if (staticTarget2)
                    //    {
                    //        self.m_targetStatic = staticTarget2;
                    //        self.m_targetCreature = null;
                    //    }
                    //}
                }
            }
        }

        public float loseAggroTime = 15f;

        public void CheckIfShouldLoseAggro(MonsterAI self)
        {
            if (self.m_targetCreature != null)
            {
                bool shouldLoseAggroTime = self.m_timeSinceSensedTargetCreature > loseAggroTime;
                float loseAggroDist = Vector3.Distance(self.m_targetCreature.transform.position, self.transform.position);
                bool shouldLoseAggroDist = self.m_timeSinceSensedTargetCreature > 1f && self.m_maxChaseDistance > 0f && loseAggroDist > self.m_maxChaseDistance;

                if (shouldLoseAggroTime || shouldLoseAggroDist)
                {
                    self.SetAlerted(false);
                    self.m_targetCreature = null;
                    self.m_targetStatic = null;
                    self.m_timeSinceAttacking = 0f;
                    self.m_updateTargetTimer = 5f;
                }
            }
        }

        public float repeatedAttackFleeTime = 20f;

        public bool CheckIfShouldFleeFromLowHealth(MonsterAI self)
        {
            if (self.m_fleeIfLowHealth > 0f && self.m_character.GetHealthPercentage() < self.m_fleeIfLowHealth && self.m_timeSinceHurt < repeatedAttackFleeTime && self.m_targetCreature != null)
            {
                return true;
            }

            return false;
        }

        public bool CheckIfShouldFleeFromUnreachable(MonsterAI self)
        {
            if (self.m_fleeIfHurtWhenTargetCantBeReached && self.m_targetCreature != null && !self.m_havePathToTarget && self.m_timeSinceHurt < repeatedAttackFleeTime)
            {
                return true;
            }

            return false;
        }

        public bool CheckIfShouldFlee(MonsterAI self)
        {
            //TODO: this flee condition
            //if (self.m_fleeIfNotAlerted && !self.HuntPlayer() && self.m_targetCreature && !self.IsAlerted() && Vector3.Distance(self.m_targetCreature.transform.position, self.transform.position) - self.m_targetCreature.GetRadius() > self.m_alertRange)
            //{
            //    self.Flee(dt, self.m_targetCreature.transform.position);
            //    self.m_aiStatus = "Avoiding conflict";
            //    return false;
            //}
            //if (self.m_targetCreature != null)
            //{
            //    if (EffectArea.IsPointInsideArea(self.m_targetCreature.transform.position, EffectArea.Type.NoMonsters, 0f))
            //    {
            //        self.Flee(dt, self.m_targetCreature.transform.position);
            //        self.m_aiStatus = "Avoid no-monster area";
            //        return false;
            //    }
            //}
            //else
            //{
            //    EffectArea effectArea = EffectArea.IsPointInsideArea(self.transform.position, EffectArea.Type.NoMonsters, 15f);
            //    if (effectArea != null)
            //    {
            //        self.Flee(dt, effectArea.transform.position);
            //        self.m_aiStatus = "Avoid no-monster area";
            //        return false;
            //    }
            //}
            //if ((self.m_afraidOfFire || self.m_avoidFire) && self.AvoidFire(dt, self.m_targetCreature, self.m_afraidOfFire))
            //{
            //    if (self.m_afraidOfFire)
            //    {
            //        self.m_targetStatic = null;
            //        self.m_targetCreature = null;
            //    }
            //    self.m_aiStatus = "Avoiding fire";
            //    return false;
            //}

            bool shouldFlee = CheckIfShouldFleeFromLowHealth(self);
            if (shouldFlee)
            {
                self.m_aiStatus = "Low health, flee";
            }
            else
            {
                shouldFlee = CheckIfShouldFleeFromUnreachable(self);
                if (shouldFlee)
                    self.m_aiStatus = "Hide from unreachable target";
            }

            return shouldFlee;
        }

        public bool CheckIfShouldEat(MonsterAI self)
        {
            //if ((!self.IsAlerted() || (self.m_targetStatic == null && self.m_targetCreature == null)) && self.UpdateConsumeItem(humanoid, dt))
            //{
            //    self.m_aiStatus = "Consume item";
            //    return false;
            //}
            return false;
        }

        public ItemDrop.ItemData currentWeapon;

        public bool TryAndUpdateAttack(MonsterAI self, float dt, bool canSeeTarget, bool canHearTarget)
        {
            ItemDrop.ItemData weaponItem = self.SelectBestAttack(m_humanoid, dt);
            currentWeapon = weaponItem;
            bool canAttack = weaponItem != null && Time.time - weaponItem.m_lastAttackTime > weaponItem.m_shared.m_aiAttackInterval && Time.time - self.m_lastAttackTime > self.m_minAttackInterval && !self.IsTakingOff();

            if ((!(self.m_targetStatic == null) || !(self.m_targetCreature == null)) && weaponItem != null)
            {
                if (weaponItem.m_shared.m_aiTargetType == ItemDrop.ItemData.AiTarget.Enemy)
                {
                    if (self.m_targetStatic)
                    {
                        Vector3 vector = self.m_targetStatic.FindClosestPoint(self.transform.position);
                        if (Vector3.Distance(vector, self.transform.position) >= weaponItem.m_shared.m_aiAttackRange || !self.CanSeeTarget(self.m_targetStatic))
                        {
                            self.m_aiStatus = "Move to static target";
                            self.MoveTo(dt, vector, 0f, self.IsAlerted());
                            return true;
                        }
                        self.LookAt(self.m_targetStatic.GetCenter());
                        if (self.IsLookingAt(self.m_targetStatic.GetCenter(), weaponItem.m_shared.m_aiAttackMaxAngle) && canAttack)
                        {
                            self.m_aiStatus = "Attacking piece";
                            self.DoAttack(null, false);
                            return true;
                        }
                        self.StopMoving();
                        return true;
                    }
                    else if (self.m_targetCreature)
                    {
                        if (canHearTarget || canSeeTarget || (self.HuntPlayer() && self.m_targetCreature.IsPlayer()))
                        {
                            self.m_beenAtLastPos = false;
                            self.m_lastKnownTargetPos = self.m_targetCreature.transform.position;
                            float num = Vector3.Distance(self.m_lastKnownTargetPos, self.transform.position) - self.m_targetCreature.GetRadius();
                            float num2 = self.m_alertRange * self.m_targetCreature.GetStealthFactor();
                            if ((canSeeTarget && num < num2) || self.HuntPlayer())
                            {
                                self.SetAlerted(true);
                            }
                            bool flag4 = num < weaponItem.m_shared.m_aiAttackRange;
                            if (!flag4 || !canSeeTarget || weaponItem.m_shared.m_aiAttackRangeMin < 0f || !self.IsAlerted())
                            {
                                self.m_aiStatus = "Move closer";
                                Vector3 velocity = self.m_targetCreature.GetVelocity();
                                Vector3 vector2 = velocity * self.m_interceptTime;
                                Vector3 vector3 = self.m_lastKnownTargetPos;
                                if (num > vector2.magnitude / 4f)
                                {
                                    vector3 += velocity * self.m_interceptTime;
                                }
                                if (self.MoveTo(dt, vector3, 0f, self.IsAlerted()))
                                {
                                    flag4 = true;
                                }
                            }
                            else
                            {
                                self.StopMoving();
                            }
                            if (flag4 && canSeeTarget && self.IsAlerted())
                            {
                                self.m_aiStatus = "In attack range";
                                self.LookAt(self.m_targetCreature.GetTopPoint());
                                if (canAttack && self.IsLookingAt(self.m_lastKnownTargetPos, weaponItem.m_shared.m_aiAttackMaxAngle))
                                {
                                    self.m_aiStatus = "Attacking creature";
                                    self.DoAttack(self.m_targetCreature, false);
                                    return true;
                                }
                            }
                        }
                        else
                        {
                            self.m_aiStatus = "Searching for target";
                            if (self.m_beenAtLastPos)
                            {
                                self.RandomMovement(dt, self.m_lastKnownTargetPos);
                                return true;
                            }
                            if (self.MoveTo(dt, self.m_lastKnownTargetPos, 0f, self.IsAlerted()))
                            {
                                self.m_beenAtLastPos = true;
                                return true;
                            }
                        }
                    }
                }
                else if (weaponItem.m_shared.m_aiTargetType == ItemDrop.ItemData.AiTarget.FriendHurt || weaponItem.m_shared.m_aiTargetType == ItemDrop.ItemData.AiTarget.Friend)
                {
                    self.m_aiStatus = "Helping friend";
                    Character character = (weaponItem.m_shared.m_aiTargetType == ItemDrop.ItemData.AiTarget.FriendHurt) ? self.HaveHurtFriendInRange(self.m_viewRange) : self.HaveFriendInRange(self.m_viewRange);
                    if (character)
                    {
                        if (Vector3.Distance(character.transform.position, self.transform.position) >= weaponItem.m_shared.m_aiAttackRange)
                        {
                            self.MoveTo(dt, character.transform.position, 0f, self.IsAlerted());
                            return true;
                        }
                        if (canAttack)
                        {
                            self.StopMoving();
                            self.LookAt(character.transform.position);
                            self.DoAttack(character, true);
                            return true;
                        }
                        self.RandomMovement(dt, character.transform.position);
                        return true;
                    }
                    else
                    {
                        self.RandomMovement(dt, self.transform.position);
                    }
                }
                return true;
            }

            return false;
        }


        public ItemDrop currentPickupTarget;

        public static string woodItemName = "Wood";
        public static string stoneItemName = "Stone";
        public static float nearbyItemsCheck = 32f;
        public Stack<Goal> goals = new Stack<Goal>();
        public Goal CurrentGoal {
            get {
                return goals.Peek();
            }
        }

        public bool HasGoal()
        {
            return goals.Count > 0;
        }

        public Goal CompleteGoal()
        {
            return goals.Pop();
        }

        public void AddGoal(Piece goalPiece)
        {
            goals.Push(new Goal(goalPiece));
        }

        public void AddGoal(string pieceName)
        {
            goals.Push(new Goal(pieceName));
        }

        public class Goal
        {
            public Piece piece;
            public List<Piece.Requirement> requiredItems;
            public Dictionary<Piece.Requirement, float> sortedRequiredItems;

            public int requiredWoodCount;
            public int requiredStoneCount;

            public Goal(Piece goalPiece)
            {
                piece = goalPiece;
                requiredWoodCount = GetRequiredWoodCount();
                requiredStoneCount = GetRequiredStoneCount();
            }

            public Goal(string pieceName)
            {
                piece = GetPiecePrefab(pieceName);
                requiredWoodCount = GetRequiredWoodCount();
                requiredStoneCount = GetRequiredStoneCount();
            }

            public List<Piece.Requirement> GetRequiredItems()
            {
                if (requiredItems == null)
                    requiredItems = new List<Piece.Requirement>();
                else
                    return requiredItems;

                if (sortedRequiredItems == null)
                    sortedRequiredItems = new Dictionary<Piece.Requirement, float>();

                if (piece != null)
                {
                    requiredItems.Clear();
                    requiredItems.AddRange(piece.m_resources);

                    sortedRequiredItems.Clear();

                    foreach (var v in requiredItems)
                    {
                        sortedRequiredItems.Add(v, float.MaxValue);
                    }
                }

                return requiredItems;
            }

            public Piece.Requirement GetCurrentGoalItem()
            {
                foreach (var req in currentRequiredItems)
                {
                    //TODO: check if npc has enough item already and set to max range

                    //TODO: think about max range search
                    var nearbyItem = FindClosestItem(req.m_resItem, 64f);

                    sortedRequiredItems[req] = nearbyItem.Value;
                }

                return sortedRequiredItems.OrderByDescending(x => x.Value).FirstOrDefault().Key;
            }

            public int GetRequiredCount(string itemID)
            {
                if (requiredItems == null)
                    return 0;

                var itemRequirement = GetRequiredItems().FirstOrDefault(x => x.m_resItem.m_itemData.m_shared.m_name == itemID);

                if (itemRequirement == null)
                    return 0;

                return itemRequirement.m_amount;
            }

            public Piece GetPiecePrefab(string pieceName)
            {
                return ZNetScene.instance.m_prefabs.FirstOrDefault(x => x.GetComponent<Piece>() != null && x.GetComponent<Piece>().m_name == pieceName).GetComponent<Piece>();
            }

            public int GetRequiredWoodCount()
            {
                return GetRequiredCount(woodItemName);
            }

            public int GetRequiredStoneCount()
            {
                return GetRequiredCount(stoneItemName);
            }
        }

        public class Memory
        {
            public Vector3 currentBaseLocation;

            public HashSet<Container> knownContainers = new HashSet<Container>();
            public HashSet<Container> ownedContainers = new HashSet<Container>();
        }

        public Memory memory = new Memory();

        public bool HasItemInInventory(string itemID)
        {
            return m_humanoid.m_inventory.HaveItem(itemID);
        }

        public int GetCountOfItemInInventory(string itemID)
        {
            return m_humanoid.m_inventory.CountItems(itemID);
        }

        public void SearchForWood()
        {
            int neededWood = CurrentGoal.requiredWoodCount - GetCountOfItemInInventory(woodItemName);

            if (neededWood > 0f)
            {
                //get nearby wood and pick it up, see if it's enough
                var nearbyWood = FindItemsInRange(woodItemName, nearbyItemsCheck);
                int woodCount = nearbyWood.Sum(x => x.m_itemData.m_stack);
                if(woodCount >= neededWood)
                {
                    currentPickupTarget = FindClosestItem(woodItemName, nearbyItemsCheck);
                }
            }

            if (currentPickupTarget != null)
                return;

            if (neededWood > 10f)
            {
                bool equippedAxe = false;
                //do we have an axe?
                var axes = GetWeaponsOfTypeInInventory(inventory, ItemDrop.ItemData.ItemType.OneHandedWeapon, 0, new HitData.DamageTypes() { m_chop = 1 });

                if(axes.Count > 0)
                {
                    m_humanoid.EquipItem(axes[0], false);
                    equippedAxe = true;
                }

                if (!equippedAxe)
                {
                    //search for an axe in known containers
                    var containersWithAxes = GetWeaponsOfTypeInContainers()

                    //search for an axe nearby on the ground

                    //add a goal to make an axe if there's not one already
                }

                if(equippedAxe)
                {
                    //chop tree
                }
            }
            else if (neededWood > 2f)
            {
                //find a micro tree or shrub and beat it up

                //if none available, change to chop a tree
            }
            else if (neededWood > 0f)
            {
                //search for loose pickable wood -- if none nearby, change to beatup a shrub
            }
            else
            {
                //begin to search far away for wood
            }
        }

        public void SearchForStone()
        {
            int neededStone = currentGoalStoneCount - GetCountOfItemInInventory("Stone");

            if (neededStone > 0f)
            {
                //get nearby stone and see if it's enough
            }

            if (neededStone > 10f)
            {
                //do we have a pickaxe?
                //make a pickaxe or get a pickaxe (if possible)

                //mine stone
            }
            else if (neededStone > 0f)
            {
                //search for loose pickable stone -- if none nearby, change to beatup a shrub
            }
            else
            {
                //begin to search far away for stone
            }
        }

        public int GetCountRemainingForGoalPiece(Piece.Requirement req)
        {
            int count = req.m_amount - GetCountOfItemsInInventory(req.m_resItem.m_itemData.m_shared.m_name);
            return count;
        }

        public int GetCountOfItemsInInventory(string itemName)
        {
            return inventory.CountItems(itemName);
        }

        public static int GetCountOfItemsInInventory(Inventory inv, string itemName)
        {
            return inv.CountItems(itemName);
        }

        public static int GetCountOfItemsInContainers(IEnumerable<Container> containers, string itemName)
        {
            return containers.Sum(x => x.m_inventory.CountItems(itemName));
        }

        public static IEnumerable<ItemDrop.ItemData> GetItemsOfTypeInInventory(Inventory inv, ItemDrop.ItemData.ItemType itemType, int minToolTier)
        {
            return inv.m_inventory.Where(x => x.m_shared.m_toolTier >= minToolTier);
        }

        public static IEnumerable<ItemDrop.ItemData> GetWeaponsOfTypeInInventory(Inventory inv, ItemDrop.ItemData.ItemType itemType, int minToolTier, HitData.DamageTypes damageTypes)
        {
            return GetItemsOfTypeInInventory(inv,itemType,minToolTier).Where(x => x.IsWeapon() && AreSimilar(x.GetDamage(), damageTypes, false));
        }

        public static IEnumerable<ItemDrop.ItemData> GetFoodItemsInInventory(Inventory inv)
        {
            return GetItemsOfTypeInInventory(inv, ItemDrop.ItemData.ItemType.Consumable, 0).Where(x => x.m_shared.m_food > 0f);
        }

        //have return kvp with the container ref that holds it
        public static IEnumerable<KeyValuePair<IEnumerable<ItemDrop.ItemData>, Container>> GetItemsOfTypeInContainers(IEnumerable<Container> containers, ItemDrop.ItemData.ItemType itemType, int minToolTier)
        {
            return containers.Select(x => new KeyValuePair<IEnumerable<ItemDrop.ItemData>, Container>(GetItemsOfTypeInInventory(x.m_inventory, itemType, minToolTier), x));
        }

        public static IEnumerable<KeyValuePair<IEnumerable<ItemDrop.ItemData>, Container>> GetWeaponsOfTypeInContainers(IEnumerable<Container> containers, ItemDrop.ItemData.ItemType itemType, int minToolTier, HitData.DamageTypes damageTypes)
        {
            return containers.Select(x => new KeyValuePair<IEnumerable<ItemDrop.ItemData>, Container>(GetItemsOfTypeInInventory(x.m_inventory, itemType, minToolTier).Where(y => y.IsWeapon() && AreSimilar(y.GetDamage(), damageTypes, false)), x));
            //return GetItemsOfTypeInContainers(containers, itemType, minToolTier).Where(x => x.m_shared.m_attack != null && AreSimilar(x.m_shared.m_damages, damageTypes, false)).ToList();
        }

        public static IEnumerable<KeyValuePair<IEnumerable<ItemDrop.ItemData>, Container>> GetFoodItemsInContainers(IEnumerable<Container> containers)
        {
            return containers.Select(x => new KeyValuePair<IEnumerable<ItemDrop.ItemData>, Container>(GetItemsOfTypeInInventory(x.m_inventory, ItemDrop.ItemData.ItemType.Consumable, 0).Where(y => y.m_shared.m_food > 0), x));
        }

        public static bool AreSimilar(HitData.DamageTypes a, HitData.DamageTypes b, bool exact)
        {
            bool b00 = (b.m_blunt > 0)     ;
            bool b01 = (b.m_chop > 0)      ;
          //bool b02 = (b.m_damage > 0)    ;
            bool b03 = (b.m_fire > 0)      ;
            bool b04 = (b.m_frost > 0)     ;
            bool b05 = (b.m_lightning > 0) ;
            bool b06 = (b.m_pickaxe > 0)   ;
            bool b07 = (b.m_pierce > 0)    ;
            bool b08 = (b.m_poison > 0)    ;
            bool b09 = (b.m_slash > 0)     ;
            bool b10 = (b.m_spirit > 0)    ;

            bool a00 = (a.m_blunt > 0     );
            bool a01 = (a.m_chop > 0      );
          //bool a02 = (a.m_damage > 0    );
            bool a03 = (a.m_fire > 0      );
            bool a04 = (a.m_frost > 0     );
            bool a05 = (a.m_lightning > 0 );
            bool a06 = (a.m_pickaxe > 0   );
            bool a07 = (a.m_pierce > 0    );
            bool a08 = (a.m_poison > 0    );
            bool a09 = (a.m_slash > 0     );
            bool a10 = (a.m_spirit > 0    );

            if(exact)
            {
                return (a00 && b00)
                    && (a01 && b01)
                  //&& (a02 && b02)
                    && (a03 && b03)
                    && (a04 && b04)
                    && (a05 && b05)
                    && (a06 && b06)
                    && (a07 && b07)
                    && (a08 && b08)
                    && (a09 && b09)
                    && (a10 && b10);
            }
            else
            {
                return (a00 && b00)
                    || (a01 && b01)
                  //|| (a02 && b02)
                    || (a03 && b03)
                    || (a04 && b04)
                    || (a05 && b05)
                    || (a06 && b06)
                    || (a07 && b07)
                    || (a08 && b08)
                    || (a09 && b09)
                    || (a10 && b10);
            }
        }

        public Container FindClosestContainer(float maxRange)
        {
            Collider[] array = Physics.OverlapSphere(base.transform.position, maxRange, NPC.m_interactMask);
            Container container = null;
            float num = 999999f;
            foreach (Collider collider in array)
            {
                if (collider.attachedRigidbody)
                {
                    Container component = collider.attachedRigidbody.GetComponent<Container>();
                    if (!(component == null) && component.GetComponent<ZNetView>().IsValid() && container.m_name == component.m_name)
                    {
                        float num2 = Vector3.Distance(component.transform.position, base.transform.position);
                        if (container == null || num2 < num)
                        {
                            container = component;
                            num = num2;
                        }
                    }
                }
            }

            //TODO: revisit this because the npc could potentially get stuck trying to access the same container
            if (container && m_monster.HavePath(container.transform.position))
            {
                return container;
            }
            return null;
        }

        public Container FindClosestContainerWithItem(string itemName, float maxRange)
        {
            Collider[] array = Physics.OverlapSphere(base.transform.position, maxRange, NPC.m_interactMask);
            Container container = null;
            float num = 999999f;
            foreach (Collider collider in array)
            {
                if (collider.attachedRigidbody)
                {
                    Container component = collider.attachedRigidbody.GetComponent<Container>();
                    if (!(component == null) && component.GetComponent<ZNetView>().IsValid() && container.m_name == component.m_name)
                    {
                        if (!component.m_inventory.HaveItem(itemName))
                            continue;

                        float num2 = Vector3.Distance(component.transform.position, base.transform.position);
                        if (container == null || num2 < num)
                        {
                            container = component;
                            num = num2;
                        }
                    }
                }
            }

            //TODO: revisit this because the npc could potentially get stuck trying to access the same container
            if (container && m_monster.HavePath(container.transform.position))
            {
                return container;
            }
            return null;
        }

        public Container FindClosestContainerWithItems(string itemName, int count, float maxRange)
        {
            Collider[] array = Physics.OverlapSphere(base.transform.position, maxRange, NPC.m_interactMask);
            Container container = null;
            float num = 999999f;
            foreach (Collider collider in array)
            {
                if (collider.attachedRigidbody)
                {
                    Container component = collider.attachedRigidbody.GetComponent<Container>();
                    if (!(component == null) && component.GetComponent<ZNetView>().IsValid() && container.m_name == component.m_name)
                    {
                        if (component.m_inventory.CountItems(itemName) < count)
                            continue;

                        float num2 = Vector3.Distance(component.transform.position, base.transform.position);
                        if (container == null || num2 < num)
                        {
                            container = component;
                            num = num2;
                        }
                    }
                }
            }

            //TODO: revisit this because the npc could potentially get stuck trying to access the same container
            if (container && m_monster.HavePath(container.transform.position))
            {
                return container;
            }
            return null;
        }

        public Container FindClosestContainerWithReq(Piece.Requirement req, float maxRange)
        {
            Collider[] array = Physics.OverlapSphere(base.transform.position, maxRange, NPC.m_interactMask);
            Container container = null;
            float num = 999999f;
            foreach (Collider collider in array)
            {
                if (collider.attachedRigidbody)
                {
                    Container component = collider.attachedRigidbody.GetComponent<Container>();
                    if (!(component == null) && component.GetComponent<ZNetView>().IsValid() && container.m_name == component.m_name)
                    {
                        if (component.m_inventory.CountItems(req.m_resItem.m_itemData.m_shared.m_name) < req.m_amount)
                            continue;

                        float num2 = Vector3.Distance(component.transform.position, base.transform.position);
                        if (container == null || num2 < num)
                        {
                            container = component;
                            num = num2;
                        }
                    }
                }
            }

            //TODO: revisit this because the npc could potentially get stuck trying to access the same container
            if (container && m_monster.HavePath(container.transform.position))
            {
                return container;
            }
            return null;
        }

        public Container FindClosestContainerWithAFreeSlot(float maxRange)
        {
            Collider[] array = Physics.OverlapSphere(base.transform.position, maxRange, NPC.m_interactMask);
            Container container = null;
            float num = 999999f;
            foreach (Collider collider in array)
            {
                if (collider.attachedRigidbody)
                {
                    Container component = collider.attachedRigidbody.GetComponent<Container>();
                    if (!(component == null) && component.GetComponent<ZNetView>().IsValid() && container.m_name == component.m_name)
                    {
                        if (component.m_inventory.SlotsUsedPercentage() >= 1f)
                            continue;

                        float num2 = Vector3.Distance(component.transform.position, base.transform.position);
                        if (container == null || num2 < num)
                        {
                            container = component;
                            num = num2;
                        }
                    }
                }
            }

            //TODO: revisit this because the npc could potentially get stuck trying to access the same container
            if (container && m_monster.HavePath(container.transform.position))
            {
                return container;
            }
            return null;
        }

        public Container FindClosestContainerWithStackRoom(ItemDrop itemSet, float maxRange)
        {
            Collider[] array = Physics.OverlapSphere(base.transform.position, maxRange, NPC.m_interactMask);
            Container container = null;
            float num = 999999f;
            foreach (Collider collider in array)
            {
                if (collider.attachedRigidbody)
                {
                    Container component = collider.attachedRigidbody.GetComponent<Container>();
                    if (!(component == null) && component.GetComponent<ZNetView>().IsValid() && container.m_name == component.m_name)
                    {
                        if (component.m_inventory.FindFreeStackSpace(itemSet.m_itemData.m_shared.m_name) < itemSet.m_itemData.m_stack)
                            continue;

                        float num2 = Vector3.Distance(component.transform.position, base.transform.position);
                        if (container == null || num2 < num)
                        {
                            container = component;
                            num = num2;
                        }
                    }
                }
            }

            //TODO: revisit this because the npc could potentially get stuck trying to access the same container
            if (container && m_monster.HavePath(container.transform.position))
            {
                return container;
            }
            return null;
        }

        public bool CanAccessContainer(Container box)
        {
            return box.CheckAccess(GetNPCID()) && box.m_nview.IsOwner() && !box.IsInUse();
        }

        public bool CanPlaceItemInContainer(Container box, ItemDrop itemSet)
        {
            if (CanAccessContainer(box))
            {
                return (box.m_inventory.FindFreeStackSpace(itemSet.m_itemData.m_shared.m_name) >= itemSet.m_itemData.m_stack);
            }

            return false;
        }

        public bool CanTakeItemFromContainer(Container box, string item)
        {
            if (CanAccessContainer(box) && ContainerHasItem(box,item))
            {
                return (inventory.FindFreeStackSpace(item) > 0);
            }

            return false;
        }

        public bool CanTakeItemsFromContainer(Container box, string item, int count)
        {
            if (CanAccessContainer(box) && ContainerHasItems(box, item, count))
            {
                return (inventory.FindFreeStackSpace(item) >= count);
            }

            return false;
        }

        public bool ContainerHasItem(Container box, string itemName)
        {
            //int itemCount = box.m_inventory.GetItem(itemSet.m_itemData.m_stack)

            return box.m_inventory.HaveItem(itemName);// && itemSet.m_itemData.m_stack);
        }

        public bool ContainerHasItems(Container box, string itemTypeName, int minCount)
        {
            int itemCount = box.m_inventory.CountItems(itemTypeName);

            return itemCount >= minCount;// && itemSet.m_itemData.m_stack);
        }

        public bool TakeItemFromContainer(Container box, string itemName)
        {
            if (CanAccessContainer(box) && CanTakeItemFromContainer(box,itemName))
            {
                inventory.MoveItemToThis(box.m_inventory, box.m_inventory.GetItem(itemName));
                return true;
            }

            return false;
        }

        public bool PlaceItemInContainer(Container box, ItemDrop item)
        {
            if (CanAccessContainer(box) && CanPlaceItemInContainer(box, item))
            {
                box.m_inventory.MoveItemToThis(inventory, item.m_itemData);
                return true;
            }

            return false;
        }

        public ItemDrop FindClosestItem(string itemName, float maxRange)
        {
            if (MonsterAI.m_itemMask == 0)
            {
                MonsterAI.m_itemMask = LayerMask.GetMask(new string[]
                {
                "item"
                });
            }
            Collider[] array = Physics.OverlapSphere(base.transform.position, maxRange, MonsterAI.m_itemMask);
            ItemDrop itemDrop = null;
            float num = 999999f;
            foreach (Collider collider in array)
            {
                if (collider.attachedRigidbody)
                {
                    ItemDrop component = collider.attachedRigidbody.GetComponent<ItemDrop>();
                    if (!(component == null) && component.GetComponent<ZNetView>().IsValid() && itemName == component.m_itemData.m_shared.m_name)
                    {
                        float num2 = Vector3.Distance(component.transform.position, base.transform.position);
                        if (itemDrop == null || num2 < num)
                        {
                            itemDrop = component;
                            num = num2;
                        }
                    }
                }
            }
            if (itemDrop && m_monster.HavePath(itemDrop.transform.position))
            {
                return itemDrop;
            }
            return null;
        }

        public KeyValuePair<ItemDrop,float> FindClosestItem(ItemDrop itemToSearch, float maxRange)
        {
            if (MonsterAI.m_itemMask == 0)
            {
                MonsterAI.m_itemMask = LayerMask.GetMask(new string[]
                {
                "item"
                });
            }
            Collider[] array = Physics.OverlapSphere(base.transform.position, maxRange, MonsterAI.m_itemMask);
            ItemDrop itemDrop = null;
            float num = 999999f;
            foreach (Collider collider in array)
            {
                if (collider.attachedRigidbody)
                {
                    ItemDrop component = collider.attachedRigidbody.GetComponent<ItemDrop>();
                    if (!(component == null) && component.GetComponent<ZNetView>().IsValid() && itemToSearch.m_itemData.m_shared.m_name == component.m_itemData.m_shared.m_name)
                    {
                        float num2 = Vector3.Distance(component.transform.position, base.transform.position);
                        if (itemDrop == null || num2 < num)
                        {
                            itemDrop = component;
                            num = num2;
                        }
                    }
                }
            }
            if (itemDrop && m_monster.HavePath(itemDrop.transform.position))
            {
                return new KeyValuePair<ItemDrop, float>( itemDrop, num );
            }
            return new KeyValuePair<ItemDrop, float>(null, float.MaxValue);
        }

        public List<ItemDrop> FindItemsInRange(string itemName, float maxRange)
        {
            if (MonsterAI.m_itemMask == 0)
            {
                MonsterAI.m_itemMask = LayerMask.GetMask(new string[]
                {
                "item"
                });
            }

            Collider[] array = Physics.OverlapSphere(base.transform.position, maxRange, MonsterAI.m_itemMask);
            if (array.Length <= 0)
                return null;

            float num = 999999f;
            List<ItemDrop> itemsInRange = new List<ItemDrop>();
            foreach (Collider collider in array)
            {
                if (collider.attachedRigidbody)
                {
                    ItemDrop component = collider.attachedRigidbody.GetComponent<ItemDrop>();
                    if (!(component == null) && component.GetComponent<ZNetView>().IsValid() && itemName == component.m_itemData.m_shared.m_name)
                    {
                        itemsInRange.Add(component);
                    }
                }
            }
            return itemsInRange;
        }

        public void UpdateSurvivalGoals(float dt)
        {
            //TODO: logic for how the goal is selected
            if (currentGoalPiece == null)
                SetGoalPiece("fire_pit");
        }

        public bool UpdateSearchForSurvivalItems(float dt)
        {
            if (this.m_consumeItems == null || this.m_consumeItems.Count == 0)
            {
                return false;
            }
            this.m_consumeSearchTimer += dt;
            if (this.m_consumeSearchTimer > this.m_consumeSearchInterval)
            {
                this.m_consumeSearchTimer = 0f;
                if (this.m_tamable && !this.m_tamable.IsHungry())
                {
                    return false;
                }
                this.m_consumeTarget = this.FindClosestConsumableItem(this.m_consumeSearchRange);
            }
            if (this.m_consumeTarget)
            {
                if (base.MoveTo(dt, this.m_consumeTarget.transform.position, this.m_consumeRange, false))
                {
                    base.LookAt(this.m_consumeTarget.transform.position);
                    if (base.IsLookingAt(this.m_consumeTarget.transform.position, 20f) && this.m_consumeTarget.RemoveOne())
                    {
                        if (this.m_onConsumedItem != null)
                        {
                            this.m_onConsumedItem(this.m_consumeTarget);
                        }
                        humanoid.m_consumeItemEffects.Create(base.transform.position, Quaternion.identity, null, 1f);
                        this.m_animator.SetTrigger("consume");
                        this.m_consumeTarget = null;
                        if (this.m_consumeHeal > 0f)
                        {
                            this.m_character.Heal(this.m_consumeHeal, true);
                        }
                    }
                }
                return true;
            }
            return false;
        }

































        private float m_rotatePieceTimer;

        private float m_baseValueUpdatetimer;

        private const int dataVersion = 24;

        private float m_equipQueuePause;

        public static bool m_debugMode = false;

        [Header("Player")]
        public float m_maxPlaceDistance = 5f;

        public float m_maxInteractDistance = 5f;

        public float m_scrollSens = 4f;

        public float m_hardDeathCooldown = 10f;

        public float m_baseCameraShake = 4f;

        public float m_placeDelay = 0.4f;

        public float m_removeDelay = 0.25f;

        public EffectList m_drownEffects = new EffectList();

        public EffectList m_spawnEffects = new EffectList();

        public EffectList m_removeEffects = new EffectList();

        public EffectList m_autopickupEffects = new EffectList();

        public EffectList m_skillLevelupEffects = new EffectList();

        public EffectList m_equipStartEffects = new EffectList();

        public GameObject m_placeMarker;

        public GameObject m_tombstone;

        public Sprite m_textIcon;

        private PieceTable m_buildPieces;

        private bool m_noPlacementCost;

        private bool m_hideUnavailable;

        private HashSet<string> m_knownRecipes = new HashSet<string>();

        private Dictionary<string, int> m_knownStations = new Dictionary<string, int>();

        private HashSet<string> m_knownMaterial = new HashSet<string>();

        private HashSet<string> m_uniques = new HashSet<string>();

        private HashSet<string> m_trophies = new HashSet<string>();

        private HashSet<Heightmap.Biome> m_knownBiome = new HashSet<Heightmap.Biome>();

        private Dictionary<string, string> m_knownTexts = new Dictionary<string, string>();

        private float m_stationDiscoverTimer;
        
        private bool m_godMode;

        private bool m_ghostMode;

        private GameObject m_placementMarkerInstance;

        private GameObject m_placementGhost;

        private Player.PlacementStatus m_placementStatus = Player.PlacementStatus.Invalid;
        
        private List<Player.EquipQueueData> m_equipQueue = new List<Player.EquipQueueData>();

        private Character m_hoveringCreature;

        private float m_lastHoverInteractTime;
        
        private float m_updateCoverTimer;

        private float m_coverPercentage;

        private bool m_underRoof = true;

        private float m_nearFireTimer;

        private bool m_isLoading;

        private float m_queuedAttackTimer;

        private float m_queuedSecondAttackTimer;

        private CraftingStation m_currentStation;

        private bool m_inCraftingStation;

        private Ragdoll m_ragdoll;

        private Piece m_hoveringPiece;

        private string m_emoteState = "";

        private int m_emoteID;
        
        private bool m_crouchToggled;
        
        private bool m_safeInHome;

        private ShipControlls m_shipControl;

        private bool m_attached;

        private string m_attachAnimation = "";

        private bool m_sleeping;

        private Transform m_attachPoint;

        private Vector3 m_detachOffset = Vector3.zero;

        private Heightmap.Biome m_currentBiome;

        private float m_biomeTimer;

        private int m_baseValue;

        private int m_comfortLevel;

        private float m_drownDamageTimer;

        private float m_timeSinceTargeted;

        private float m_timeSinceSensed;

        private float m_stealthFactorUpdateTimer;

        private float m_stealthFactor;

        private float m_stealthFactorTarget;

        private Vector3 m_lastStealthPosition = Vector3.zero;

        private float m_wakeupTimer = -1f;

        private float m_timeSinceDeath = 999999f;

        private float m_runSkillImproveTimer;

        private float m_swimSkillImproveTimer;

        private float m_sneakSkillImproveTimer;

        private static int crouching = 0;

        protected static int m_attackMask = 0;

        protected static int m_animatorTagDodge = Animator.StringToHash("dodge");

        protected static int m_animatorTagCutscene = Animator.StringToHash("cutscene");

        protected static int m_animatorTagCrouch = Animator.StringToHash("crouch");

        protected static int m_animatorTagMinorAction = Animator.StringToHash("minoraction");

        protected static int m_animatorTagEmote = Animator.StringToHash("emote");

        private List<PieceTable> m_tempOwnedPieceTables = new List<PieceTable>();

        private List<Transform> m_tempSnapPoints1 = new List<Transform>();

        private List<Transform> m_tempSnapPoints2 = new List<Transform>();

        private List<Piece> m_tempPieces = new List<Piece>();















        //TODO: continue paste

        public static List<NPC> s_npcs = new List<NPC>();

        public static void GetNPCsInRange(Vector3 point, float range, List<NPC> nearNPCs)
        {
            foreach (NPC npc in NPC.s_npcs)
            {
                if (Utils.DistanceXZ(npc.transform.position, point) < range)
                {
                    nearNPCs.Add(npc);
                }
            }
        }

        public static bool IsNPCInRange(Vector3 point, float range)
        {
            using (List<NPC>.Enumerator enumerator = NPC.s_npcs.GetEnumerator())
            {
                while (enumerator.MoveNext())
                {
                    if (Utils.DistanceXZ(enumerator.Current.transform.position, point) < range)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        public static void GetFriendlyNPCsInRange(Vector3 point, float range, List<NPC> nearNPCs)
        {
            foreach (NPC npc in NPC.s_npcs)
            {
                if (npc.m_character.m_faction != Character.Faction.Players)
                    continue;

                if (Utils.DistanceXZ(npc.transform.position, point) < range)
                {
                    nearNPCs.Add(npc);
                }
            }
        }

        public static bool IsFriendlyNPCInRange(Vector3 point, float range)
        {
            using (List<NPC>.Enumerator enumerator = NPC.s_npcs.GetEnumerator())
            {
                while (enumerator.MoveNext())
                {
                    if (enumerator.Current.m_character.m_faction != Character.Faction.Players)
                        continue;

                    if (Utils.DistanceXZ(enumerator.Current.transform.position, point) < range)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        public static bool IsNPCInRange(Vector3 point, float range, float minNoise)
        {
            using (List<NPC>.Enumerator enumerator = NPC.s_npcs.GetEnumerator())
            {
                while (enumerator.MoveNext())
                {
                    if (Utils.DistanceXZ(enumerator.Current.transform.position, point) < range)
                    {
                        float noiseRange = enumerator.Current.m_character.GetNoiseRange();
                        if (range <= noiseRange && noiseRange >= minNoise)
                        {
                            return true;
                        }
                        return true;
                    }
                }
            }
            return false;
        }


        public static bool IsNPCInRange(Vector3 point, float range, out NPC firstNPCInrange)
        {
            firstNPCInrange = null;
            using (List<NPC>.Enumerator enumerator = NPC.s_npcs.GetEnumerator())
            {
                while (enumerator.MoveNext())
                {
                    if (Utils.DistanceXZ(enumerator.Current.transform.position, point) < range)
                    {
                        firstNPCInrange = enumerator.Current;
                        return true;
                    }
                }
            }
            return false;
        }

        public static void GetNPCsInZone(List<NPC> nearNPCs, Vector3 zonePosition)
        {
            Vector2i zone = ZoneSystem.instance.GetZone(zonePosition);
            foreach (NPC npc in NPC.s_npcs)
            {
                if (IsNPCInActiveArea(zone, npc))
                {
                    nearNPCs.Add(npc);
                }
            }
        }

        public static void IsNPCsInZone(List<NPC> nearNPCs, Vector3 zonePosition)
        {
            Vector2i zone = ZoneSystem.instance.GetZone(zonePosition);
            foreach (NPC npc in NPC.s_npcs)
            {
                if (IsNPCInActiveArea(zone, npc))
                {
                    nearNPCs.Add(npc);
                }
            }
        }

        public static bool IsNPCInActiveArea(Vector2i zone)
        {
            foreach (NPC npc in NPC.s_npcs)
            {
                if (IsNPCInActiveArea(zone, npc))
                    return true;
            }

            return false;
        }

        public static bool IsNPCInActiveArea(Vector2i zone, NPC npc)
        {
            Vector2i refCenterZone = npc.npz.sector;
            int num = npc.npz.m_activeAreaRadius;
            return zone.x >= refCenterZone.x - num && zone.x <= refCenterZone.x + num && zone.y <= refCenterZone.y + num && zone.y >= refCenterZone.y - num;
        }

        public static bool IsNPCOutsideActiveArea(Vector3 point)
        {
            foreach (NPC npc in NPC.s_npcs)
            {
                if (!IsNPCOutsideActiveArea(point, npc))
                    return false;
            }

            return true;
        }

        public static bool IsNPCOutsideActiveArea(Vector3 point, NPC npc)
        {
            Vector2i zone = npc.npz.sector;// ZoneSystem.instance.GetZone(refPoint);
            Vector2i zone2 = ZoneSystem.instance.GetZone(point);
            return zone2.x <= zone.x - ZoneSystem.instance.m_activeArea || zone2.x >= zone.x + ZoneSystem.instance.m_activeArea || zone2.y >= zone.y + ZoneSystem.instance.m_activeArea || zone2.y <= zone.y - ZoneSystem.instance.m_activeArea;
        }

        //public static bool InsideZone(Vector3 point, Vector3 zonePosition)
        //{
        //    float zoneSize = ZoneSystem.instance.m_zoneSize * 0.5f;
        //    Vector3 position = zonePosition;
        //    return point.x >= position.x - zoneSize && point.x <= position.x + zoneSize && point.z >= position.z - zoneSize && point.z <= position.z + zoneSize;
        //}


        public static GameObject prefabRoot;
        public static GameObject playerPrefab;
        public static GameObject npcBase;

        public static GameObject skeletonPrefab;

        public static GameObject GetSkeletonPrefab()
        {
            if(skeletonPrefab == null)
                skeletonPrefab = ZNetScene.instance.m_prefabs.FirstOrDefault(x => x.name == "Skeleton_NoArcher");
            return skeletonPrefab;
        }

        public static Transform GetPrefabRoot()
        {
            if (prefabRoot != null)
                return prefabRoot.transform;

            prefabRoot = new GameObject("VEX_PREFAB_ROOT (leave inactive)");
            GameObject.DontDestroyOnLoad(prefabRoot);
            prefabRoot.SetActive(false);
            return prefabRoot.transform;
        }

        public static GameObject GetPlayerPrefab()
        {
            if (playerPrefab == null)
                playerPrefab = ZNetScene.instance.m_prefabs.FirstOrDefault(x => x.name == "Player");
            return playerPrefab;
        }

        public static NPC MakeNPCInstance()
        {
            return GameObject.Instantiate(GetNPCBase(), Player.m_localPlayer.transform.position + Vector3.forward * 5f, Quaternion.identity, ZNetScene.instance.m_netSceneRoot.transform).GetComponent<NPC>();
        }

        public static NPC MakeNPCInstance(Vector3 pos)
        {
            return GameObject.Instantiate(GetNPCBase(), pos, Quaternion.identity, ZNetScene.instance.m_netSceneRoot.transform).GetComponent<NPC>();
        }

        public static NPC GetNPCBase()
        {
            if (npcBase != null)
                return npcBase.GetComponent<NPC>();

            Debug.Log("making npc base");

            NPZ.MOD_ENABLED = true;

            Player p = GetPlayerPrefab().GetComponent<Player>();
            Transform visual = p.GetComponentsInChildren<Transform>().FirstOrDefault(x => x.name == "Visual");
            VisEquipment pv = p.GetComponent<VisEquipment>();
            ZSyncAnimation pvasync = p.GetComponent<ZSyncAnimation>();
            ZSyncTransform pvasynct = p.GetComponent<ZSyncTransform>();

            Debug.Log("making root base obj");
            npcBase = GameObject.Instantiate(GetSkeletonPrefab(), GetPrefabRoot(), true);
            
            Humanoid h = npcBase.GetComponent<Humanoid>();

            Transform npc_visual = h.GetComponentsInChildren<Transform>().FirstOrDefault(x => x.name == "Visual");
            //remove old
            GameObject.Destroy(npc_visual.gameObject);

            Debug.Log("making replacement visual");

            //clone player visual into this
            npc_visual = GameObject.Instantiate(visual.gameObject, npcBase.transform, false).transform;
            npc_visual.name = "Visual";
            npc_visual.SetAsFirstSibling();

            //get refs we'll use
            Animator npc_animator = npc_visual.GetComponent<Animator>();
            LODGroup npc_lod = npc_visual.GetComponent<LODGroup>();
            AnimationEffect npc_animeffect = npc_visual.GetComponent<AnimationEffect>();
            CharacterAnimEvent npc_animevent = npc_visual.GetComponent<CharacterAnimEvent>();
            ZSyncAnimation npc_animsync = npcBase.GetComponent<ZSyncAnimation>();
            ZSyncTransform npc_tsync = npcBase.GetComponent<ZSyncTransform>();

            npc_animsync.m_animator = npc_animator;

            
            if(pvasync.m_syncBools != null) npc_animsync.m_syncBools = new List<string> ( pvasync.m_syncBools );
            if(pvasync.m_syncFloats != null) npc_animsync.m_syncFloats = new List<string>(pvasync.m_syncFloats);
            if(pvasync.m_syncInts != null) npc_animsync.m_syncInts = new List<string>(pvasync.m_syncInts);
            if(pvasync.m_boolHashes != null) npc_animsync.m_boolHashes = new List<int>(pvasync.m_boolHashes).ToArray();
            if(pvasync.m_boolDefaults != null) npc_animsync.m_boolDefaults = new List<bool>(pvasync.m_boolDefaults).ToArray();
            if(pvasync.m_floatHashes != null) npc_animsync.m_floatHashes = new List<int>(pvasync.m_floatHashes).ToArray();
            if(pvasync.m_floatDefaults != null) npc_animsync.m_floatDefaults = new List<float>(pvasync.m_floatDefaults).ToArray();
            if(pvasync.m_intHashes != null) npc_animsync.m_intHashes = new List<int>(pvasync.m_intHashes).ToArray();
            if(pvasync.m_intDefaults != null) npc_animsync.m_intDefaults = new List<int>(pvasync.m_intDefaults).ToArray();

            npc_tsync.m_syncBodyVelocity = true;
            npc_tsync.m_characterParentSync = true;




            //now hook up components to the new visual
            h.m_defaultItems = new GameObject[0];
            h.m_randomWeapon = new GameObject[0];
            h.m_randomArmor = new GameObject[0];
            h.m_randomShield = new GameObject[0];
            //keep an eye on this and see if separate instances are needed....
            h.m_unarmedWeapon = p.m_unarmedWeapon;
            h.m_pickupEffects = p.m_pickupEffects;
            h.m_dropEffects = p.m_pickupEffects;
            h.m_consumeItemEffects = p.m_pickupEffects;
            h.m_equipEffects = p.m_pickupEffects;
            h.m_perfectBlockEffect = p.m_pickupEffects;

            NPC npc = npcBase.GetComponent<NPC>();
            if (npc == null)
                npc = npcBase.AddComponent<NPC>();

            h.m_inventory.m_inventory = new List<ItemDrop.ItemData>();
            h.m_inventory.m_onChanged = (Action)Delegate.Combine(h.m_inventory.m_onChanged, new Action(npc.OnInventoryChanged));

            //strip them
            h.m_rightItem = null;
            h.m_chestItem = null;
            h.m_legItem = null;
            h.m_ammoItem = null;
            h.m_helmetItem = null;
            h.m_shoulderItem = null;
            h.m_utilityItem = null;

            h.m_hiddenLeftItem = null;
            h.m_hiddenRightItem = null;

            VisEquipment v = h.GetComponent<VisEquipment>();
            v.m_bodyModel = h.GetComponentsInChildren<SkinnedMeshRenderer>(true).FirstOrDefault(x => x.name == "body");
            v.m_leftHand = h.GetComponentsInChildren<Transform>(true).FirstOrDefault(x => x.name == "LeftHand_Attach");
            v.m_rightHand = h.GetComponentsInChildren<Transform>(true).FirstOrDefault(x => x.name == "RightHand_Attach");
            v.m_helmet = h.GetComponentsInChildren<Transform>(true).FirstOrDefault(x => x.name == "Helmet_attach");
            v.m_backShield = h.GetComponentsInChildren<Transform>(true).FirstOrDefault(x => x.name == "BackShield_attach");
            v.m_backMelee = h.GetComponentsInChildren<Transform>(true).FirstOrDefault(x => x.name == "BackOneHanded_attach");
            v.m_backTwohandedMelee = h.GetComponentsInChildren<Transform>(true).FirstOrDefault(x => x.name == "BackTwohanded_attach");
            v.m_backBow = h.GetComponentsInChildren<Transform>(true).FirstOrDefault(x => x.name == "BackBow_attach");
            v.m_backTool = h.GetComponentsInChildren<Transform>(true).FirstOrDefault(x => x.name == "BackTool_attach");
            v.m_backAtgeir = h.GetComponentsInChildren<Transform>(true).FirstOrDefault(x => x.name == "BackAtgeir_attach");
            v.m_clothColliders = h.GetComponentsInChildren<CapsuleCollider>(true).Where(x => x.name.Contains("ClothCollider")).ToArray();
            v.m_models = pv.m_models;
            //used to enable the various equipment models
            v.m_isPlayer = true;
            v.m_emptyBodyTexture = pv.m_emptyBodyTexture;
            v.m_visual = npc_visual.gameObject;
            v.m_lodGroup = npc_lod;

            h.m_visEquipment = v;

            h.m_faction = Character.Faction.Players;
            h.m_crouchSpeed = 2f;
            h.m_walkSpeed = 1.6f;
            h.m_speed = 4f;
            h.m_turnSpeed = 300f;
            h.m_runSpeed = 7f;
            h.m_runTurnSpeed = 300f;
            h.m_flySlowSpeed = 5f;
            h.m_flyFastSpeed = 12f;
            h.m_flyTurnSpeed = 12f;
            h.m_acceleration = 0.8f;
            h.m_jumpForce = 8f;
            h.m_jumpForceForward = 2f;
            h.m_jumpForceTiredFactor = 0.6f;
            h.m_airControl = 0.1f;
            h.m_canSwim = true;
            h.m_swimDepth = 1.5f;
            h.m_swimSpeed = 3.9f;
            h.m_swimTurnSpeed = 100f;
            h.m_swimAcceleration = 0.05f;
            h.m_flying = false;
            h.m_jumpStaminaUsage = 10;
            h.m_level = 1;

            h.m_eye = h.GetComponentsInChildren<Transform>(true).FirstOrDefault(x => x.name == "EyePos");
            h.m_head = h.GetComponentsInChildren<Transform>(true).FirstOrDefault(x => x.name == "Head");

            h.m_hitEffects = p.m_hitEffects;
            h.m_critHitEffects = p.m_critHitEffects;
            h.m_backstabHitEffects = p.m_backstabHitEffects;
            h.m_deathEffects = p.m_deathEffects;
            h.m_waterEffects = p.m_waterEffects;
            h.m_slideEffects = p.m_slideEffects;
            h.m_jumpEffects = p.m_jumpEffects;

            h.m_tolerateWater = true;
            h.m_tolerateFire = false;
            h.m_tolerateSmoke = false;
            h.m_damageModifiers = p.m_damageModifiers.Clone();
            h.m_staggerWhenBlocked = p.m_staggerWhenBlocked;

            h.m_visual = npc_visual.gameObject;
            h.m_lodGroup = npc_lod;
            h.m_animator = npc_animator;
            h.m_animEvent = npc_animevent;

            //TODO: may need to hook up more components
            h.m_nview = h.GetComponent<ZNetView>();

            npc = MakeNPC(npcBase);
            return npc;
        }


        //for debugging the mod
        public static NPC MakeNPC(GameObject obj)
        {
            if (obj == null)
                return null;

            NPZ.MOD_ENABLED = true;

            Character c = obj.GetComponent<Character>();
            if (c == null)
                return null;

            NPC npc = obj.GetComponent<NPC>();
            if(npc == null)
                npc = obj.AddComponent<NPC>();

            //for debug
            c.SetMaxHealth(100000f);
            c.SetHealth(100000f);

            //for debug
            MonsterAI m = obj.GetComponent<MonsterAI>();
            if (m != null)
            {
                m.m_attackPlayerObjects = false;
                m.m_attackPlayerObjectsWhenAlerted = false;
                m.m_enableHuntPlayer = false;
                c.m_boss = false;

                if(m.m_randomFly)
                {
                    c.m_swimDepth = -10f;
                }
            }

            //for debug
            BaseAI b = obj.GetComponent<BaseAI>();
            if (b != null)
            { 
                b.m_huntPlayer = false;
                b.m_randomMoveRange = 100f;
            }

            c.m_groundTilt = Character.GroundTiltType.PitchRaycast;
            b.m_smoothMovement = true;

            //if (placeRandomly)
            //{
            //    for (int i = 0; i < 1000; ++i)
            //    {
            //        float x = UnityEngine.Random.Range(-5000, 5000);
            //        float z = UnityEngine.Random.Range(-5000, 5000);
            //        Vector3 telepos = new Vector3(x, 0f, z);
            //        Vector2i zone = ZoneSystem.instance.GetZone(telepos);

            //        bool moveHere = !ZNetScene.instance.IsAreaReady(telepos);

            //        if (moveHere)
            //        {
            //            npc.TeleportTo(telepos, npc.transform.rotation, true);
            //            break;
            //        }
            //    }
            //}

            return npc;
        }
    }


    //[HarmonyPatch(typeof(MonsterAI), "UpdateAI")]
    //public static class MonsterAI_UpdateAI_Patch
    //{
    //    private static void Postfix(ref MonsterAI __instance, float dt)
    //    {
    //        MonsterAI self = __instance;
    //        if (!self.m_nview.IsOwner())
    //        {
    //            return;
    //        }

    //        if (self.IsSleeping())
    //        {
    //            return;
    //        }


    //    }
    //}
}



/*
 stops for custom npc

    clone skeleton_noarcher:
    GameObject.Instantiate(geti(), Player.m_localPlayer.transform.position, Quaternion.identity)



    */
