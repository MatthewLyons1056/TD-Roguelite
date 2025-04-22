using System.Collections.Generic;
using UnityEngine;

public class EnhancementController : MonoBehaviour
{
    public static EnhancementController Instance;

    public List<BuffEffect> activeEnhancementBuffs;
    public List<Enhancement> activeEnhancements;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
        }
    }

    public void AddEnhancement(string ID)
    {
        Enhancement enhancement = LibraryLink.Instance.dataLibrary.enhancementDataSO.GetEnhancement(ID);
        activeEnhancements.Add(enhancement);
    }
    public void EnableEnhancements()//enables enhancements, buff enhancements are added to a list while persistent effects are triggered once
    {
        activeEnhancementBuffs.Clear();

        foreach (Enhancement enhancement in activeEnhancements)
        {
            if(!enhancement.isEnabled)
            {
                BuffEffect buff = new();
                switch (enhancement.ID)
                {
                    case "EH_001"://Reduce deadzone while taking away velocity and firerate
                        buff = new()
                        {
                            icon = null,
                            Description = enhancement.description,
                            Duration = enhancement.duration,
                            BuffType = "Enhancement",
                            Cooldown = 0,
                            ID = enhancement.ID,
                            modifier = 1,
                            Name = enhancement.name,
                            TriggerCondition = "Permanent",
                            ValueX = enhancement.effectX,
                            ValueY = enhancement.effectY,
                            ValueZ = enhancement.effectZ,
                            UID = enhancement.effectID
                        };

                        activeEnhancementBuffs.Add(buff);
                        break;
                    case "EH_002"://combat encounters start with more gold
                        enhancement.isEnabled = true;
                        ResourceController.Instance.starterGoldVal += enhancement.effectX;
                        break;
                    case "EH_003"://gain a buff on all towers for firerate based on missing traits
                        enhancement.isEnabled = true;
                        TowerInventory.Instance.onTraitTypeCounterChange.Add(enhancement);
                        InventoryController.instance.globalFirerateBonus.Value += enhancement.effectX * LibraryLink.Instance.dataLibrary.rollableTraits.Count;
                        break;
                    case "EH_004"://gain a Kill buff that gives fire rate and dmg but reduces range
                        buff = new()
                        {
                            icon = null,
                            Description = enhancement.description,
                            Duration = enhancement.duration,
                            BuffType = "Enhancement",
                            Cooldown = 0,
                            ID = enhancement.ID,
                            modifier = 1,
                            Name = enhancement.name,
                            TriggerCondition = "OnKill",
                            ValueX = enhancement.effectX,
                            ValueY = enhancement.effectY,
                            ValueZ = enhancement.effectZ,
                            UID = enhancement.effectID
                        };

                        activeEnhancementBuffs.Add(buff);
                        break;
                    case "EH_005"://Gain penetration and velocity, each hit increases current projectile DMG
                        buff = new()
                        {
                            icon = null,
                            Description = enhancement.description,
                            Duration = enhancement.duration,
                            BuffType = "Enhancement",
                            Cooldown = 0,
                            ID = enhancement.ID,
                            modifier = 1,
                            Name = enhancement.name,
                            TriggerCondition = "Permanent",
                            ValueX = enhancement.effectX,
                            ValueY = enhancement.effectY,
                            ValueZ = enhancement.effectZ,
                            UID = enhancement.effectID
                        };

                        activeEnhancementBuffs.Add(buff);
                        break;
                    case "EH_006"://gain firerate at the start of waves which decays into a negative value
                        buff = new()
                        {
                            icon = null,
                            Description = enhancement.description,
                            Duration = enhancement.duration,
                            BuffType = "Enhancement",
                            Cooldown = 0,
                            ID = enhancement.ID,
                            modifier = 1,
                            Name = enhancement.name,
                            TriggerCondition = "OnWaveStart",
                            ValueX = enhancement.effectX,
                            ValueY = enhancement.effectY,
                            ValueZ = enhancement.effectZ,
                            UID = enhancement.effectID
                        };

                        InventoryController.instance.globalFirerateBonus.Value += enhancement.effectY;

                        activeEnhancementBuffs.Add(buff);
                        break;
                    case "EH_007"://towers with 1 component have boosted combat stats
                        buff = new()
                        {
                            icon = null,
                            Description = enhancement.description,
                            Duration = enhancement.duration,
                            BuffType = "Enhancement",
                            Cooldown = 0,
                            ID = enhancement.ID,
                            modifier = 1,
                            Name = enhancement.name,
                            TriggerCondition = "Permanent",
                            ValueX = enhancement.effectX,
                            ValueY = enhancement.effectY,
                            ValueZ = enhancement.effectZ,
                            UID = enhancement.effectID
                        };

                        activeEnhancementBuffs.Add(buff);
                        break;
                    case "EH_008"://all bleed effects are stronger
                        CombatController.Instance.bleed_DMGBonus += enhancement.effectX;
                        break;
                    case "EH_009"://increases the effect of Burn, Slow and Frozen. Burn cannot stack with Slow/Frozen
                        CombatController.Instance.burn_DMGBonus += enhancement.effectX;
                        CombatController.Instance.slow_DurationBonus += enhancement.effectY;
                        CombatController.Instance.slow_MaxDuration += enhancement.duration;
                        CombatController.Instance.frozen_DurationBonus += enhancement.effectZ;
                        CombatController.Instance.frozen_MaxDuration += enhancement.duration;

                        CombatController.Instance.burn_CanApplyToCold = false;
                        CombatController.Instance.slow_CanApplyToHot = false;
                        CombatController.Instance.frozen_CanApplyToHot = false;
                        break;
                    case "EH_010"://the TC heals every wave and causes an burst of DMG when taking DMG
                        buff = new()
                        {
                            icon = null,
                            Description = enhancement.description,
                            Duration = enhancement.duration,
                            BuffType = "Enhancement",
                            Cooldown = 0,
                            ID = enhancement.ID,
                            modifier = 1,
                            Name = enhancement.name,
                            TriggerCondition = "OnWaveStart",
                            ValueX = enhancement.effectX,
                            ValueY = enhancement.effectY,
                            ValueZ = enhancement.effectZ,
                            UID = enhancement.effectID
                        };

                        TownCenterController.Instance.OnWaveStartBuffs.Add(buff);

                        buff = new()
                        {
                            icon = null,
                            Description = enhancement.description,
                            Duration = enhancement.duration,
                            BuffType = "Enhancement",
                            Cooldown = 0,
                            ID = enhancement.ID,
                            modifier = 1,
                            Name = enhancement.name,
                            TriggerCondition = "OnDamaged",
                            ValueX = enhancement.effectX,
                            ValueY = enhancement.effectY,
                            ValueZ = enhancement.effectZ,
                            UID = enhancement.effectID
                        };

                        TownCenterController.Instance.OnDamagedBuffs.Add(buff);
                        break;
                }
            }
        }
    }
}
