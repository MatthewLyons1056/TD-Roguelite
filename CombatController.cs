using Sirenix.OdinInspector;
using System.Collections.Generic;
using UnityEngine;

public class CombatController : MonoBehaviour
{
    public static CombatController Instance { get; private set; }

    #region DMGMult
    [HorizontalGroup("row1")] public bool DMGMult = true;
    [ShowIfGroup("DMGMult")]

    [TabGroup("DMGMult/Combat Stats", "Fodder", false, 2)] public float[] fodderDMGMults;
    [TabGroup("DMGMult/Combat Stats", "Standard", false, 2)] public float[] standardDMGMults;
    [TabGroup("DMGMult/Combat Stats", "Large", false, 2)] public float[] largeDMGMults;
    [TabGroup("DMGMult/Combat Stats", "Collosal", false, 2)] public float[] collosalDMGMults;
    [TabGroup("DMGMult/Combat Stats", "Status", false, 2)] public float[] statusDMGMults;
    #endregion DMGMult

    #region Debuffs
    [HorizontalGroup("row1")] public bool Debuffs = true;
    [ShowIfGroup("Debuffs")]

    [TabGroup("Debuffs/Status stats", "Burn", false, 2)] public float burn_DMGMult, burn_DurationBonus, burn_DMGBonus, burn_MaxDuration, burn_TimeTillDMG;
    [TabGroup("Debuffs/Status stats", "Burn", false, 2)] public bool burn_CanApplyToCold;

    [TabGroup("Debuffs/Status stats", "Smite", false, 2)] public float smite_Size, smite_DMGBonus, smite_SizePerStack, smite_TimeTillDetonate;
    [TabGroup("Debuffs/Status stats", "Smite", false, 2)] public int smite_MaxStacks;

    [TabGroup("Debuffs/Status stats", "Stagger", false, 2)] public float stagger_Cooldown, stagger_Window, stagger_SlowDuration, stagger_StunDuration, stagger_SlowVal, stagger_DMGBonus;

    [TabGroup("Debuffs/Status stats", "Slow", false, 2)] public float slow_EffectVal, slow_DurationBonus, slow_MaxDuration;
    [TabGroup("Debuffs/Status stats", "Slow", false, 2)] public EnemyClass slow_TargetLimit;
    [TabGroup("Debuffs/Status stats", "Slow", false, 2)] public bool slow_CanApplyToHot;

    [TabGroup("Debuffs/Status stats", "Thorns", false, 2)] public float thorns_SizeBonus, thorns_DMGBonus, thorns_DurationBonus, thorns_MaxDuration;

    [TabGroup("Debuffs/Status stats", "Galvanized", false, 2)] public float galvanized_Cooldown, galvanized_ChainCooldown, galvanized_ChainDMGBonus, galvanized_FinishDMGBonus;
    [TabGroup("Debuffs/Status stats", "Galvanized", false, 2)] public int galvanized_ChainCount;

    [TabGroup("Debuffs/Status stats", "Bleed", false, 2)] public float bleed_DurationBonus, bleed_DMGBonus, bleed_MaxDuration;

    [TabGroup("Debuffs/Status stats", "Frozen", false, 2)] public EnemyClass frozen_TargetLimit;
    [TabGroup("Debuffs/Status stats", "Frozen", false, 2)] public float frozen_MaxDuration, frozen_DurationBonus;
    [TabGroup("Debuffs/Status stats", "Frozen", false, 2)] public bool frozen_CanApplyToHot;

    [TabGroup("Debuffs/Status stats", "Transmutation", false, 2)] public int transmutation_StackLimit;
    [TabGroup("Debuffs/Status stats", "Transmutation", false, 2)] public float transmutation_BonusPerStack;
    #endregion Debuffs

    #region Buffs
    [HorizontalGroup("row1")] public bool Buffs = true;
    [ShowIfGroup("Buffs")]

    [TabGroup("Buffs/Buff Stats", "Haste", false, 2)] public float haste_EffectVal;

    [TabGroup("Buffs/Buff Stats", "Regen", false, 2)] public float regen_EffectVal;

    [TabGroup("Buffs/Buff Stats", "Magic Shield", false, 2)] public float magicShield_EffectVal, magicShield_SearchRange;
    [TabGroup("Buffs/Buff Stats", "Magic Shield", false, 2)] public int magicShield_TargetLimit;
    #endregion Buffs

    #region Tower Effects
    [HorizontalGroup("row1")] public bool TowerEffects = true;
    [ShowIfGroup("TowerEffects")]

    [TabGroup("TowerEffects/Tower stats", "Rebound", false, 2)] public float reboundDMG;
    [TabGroup("TowerEffects/Tower stats", "Rebound", false, 2)] public int reboundBonusCount;

    [TabGroup("TowerEffects/Tower stats", "Deadzone", false, 2)] public float deadzoneSize;
    #endregion Tower Effects

    #region Tower Crit Stats
    [HorizontalGroup("row1")] public bool Crit = true;
    [ShowIfGroup("Crit")]

    [TabGroup("Crit/Tower crit", "Hunter", false, 2)] public float hunterCritRate, hunterCritDMG;
    [TabGroup("Crit/Tower crit", "concussive", false, 2)] public float concussiveCritRate, concussiveCritDMG;
    [TabGroup("Crit/Tower crit", "ignition", false, 2)] public float ignitionCritRate, ignitionCritDMG;
    [TabGroup("Crit/Tower crit", "cryo", false, 2)] public float cryoCritRate, cryoCritDMG;
    [TabGroup("Crit/Tower crit", "terra", false, 2)] public float terraCritRate, terraCritDMG;
    [TabGroup("Crit/Tower crit", "sanctified", false, 2)] public float sanctifiedCritRate, sanctifiedCritDMG;
    [TabGroup("Crit/Tower crit", "cursed", false, 2)] public float cursedCritRate, cursedCritDMG;
    [TabGroup("Crit/Tower crit", "explosive", false, 2)] public float explosiveCritRate, explosiveCritDMG;
    [TabGroup("Crit/Tower crit", "spark", false, 2)] public float sparkCritRate, sparkCritDMG;
    [TabGroup("Crit/Tower crit", "puncture", false, 2)] public float punctureCritRate, punctureCritDMG;
    #endregion Tower Crit Stats

    private List<string> activeBuffs = new();

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

    public void ApplyGlobalBuff(BuffEffect buff)
    {
        bool canApplyBuff = true;

        foreach(string ID in activeBuffs)
        {
            if(ID ==  buff.ID)
            {
                canApplyBuff = false;
                break;
            }
        }

        if (canApplyBuff)
        {
            activeBuffs.Add(buff.ID);
            switch (buff.ID)
            {
                case "TB_401"://improve burn
                    burn_DMGBonus += buff.ValueX;
                    burn_DurationBonus += buff.ValueY;
                    burn_MaxDuration += buff.ValueZ;
                    break;
                case "TB_801"://increase smite size and damage
                    smite_Size += buff.ValueX;
                    smite_DMGBonus += buff.ValueY;
                    break;
                case "TB_201"://increase slow amount
                    slow_EffectVal -= buff.ValueX;
                    break;
                case "TB_501"://increase thorn size
                    thorns_SizeBonus += buff.ValueX;
                    break;
                case "TB_601"://improves stagger
                    stagger_StunDuration += buff.ValueX;
                    stagger_SlowDuration += buff.ValueY;
                    stagger_Cooldown -= buff.ValueZ;
                    break;
                case "TB_301"://improve Rebound
                    reboundDMG += buff.ValueX;
                    reboundBonusCount += Mathf.FloorToInt(buff.ValueY);
                    break;
                case "TB_101"://increase damage taken by large+ enemies
                    fodderDMGMults[2] *= buff.ValueY;
                    fodderDMGMults[3] *= buff.ValueY;
                    standardDMGMults[2] *= buff.ValueY;
                    standardDMGMults[3] *= buff.ValueY;
                    largeDMGMults[2] *= buff.ValueY;
                    largeDMGMults[3] *= buff.ValueY;
                    collosalDMGMults[2] *= buff.ValueY;
                    collosalDMGMults[3] *= buff.ValueY;
                    statusDMGMults[2] *= buff.ValueY;
                    statusDMGMults[3] *= buff.ValueY;
                    break;
            }
        }
    }

    public void RemoveGlobalBuff(BuffEffect buff)
    {
        bool canRemoveBuff = false;

        foreach (string ID in activeBuffs)
        {
            if (ID == buff.ID)
            {
                canRemoveBuff = true;
                break;
            }
        }

        if (canRemoveBuff)
        {
            activeBuffs.Remove(buff.ID);
            switch (buff.ID)
            {
                case "TB_401"://improve burn
                    burn_DMGBonus -= buff.ValueX;
                    burn_DurationBonus -= buff.ValueY;
                    burn_MaxDuration -= buff.ValueZ;
                    break;
                case "TB_801"://increase smite size and damage
                    smite_Size -= buff.ValueX;
                    smite_DMGBonus -= buff.ValueY;
                    break;
                case "TB_201"://increase slow amount
                    slow_EffectVal += buff.ValueX;
                    break;
                case "TB_501"://increase thorn size
                    thorns_SizeBonus -= buff.ValueX;
                    break;
                case "TB_601"://improves stagger
                    stagger_StunDuration -= buff.ValueX;
                    stagger_SlowDuration -= buff.ValueY;
                    stagger_Cooldown += buff.ValueZ;
                    break;
                case "TB_301"://improve Rebound
                    reboundDMG -= buff.ValueX;
                    reboundBonusCount -= Mathf.FloorToInt(buff.ValueY);
                    break;
                case "TB_101"://increase damage taken by large+ enemies
                    fodderDMGMults[2] /= buff.ValueY;
                    fodderDMGMults[3] /= buff.ValueY;
                    standardDMGMults[2] /= buff.ValueY;
                    standardDMGMults[3] /= buff.ValueY;
                    largeDMGMults[2] /= buff.ValueY;
                    largeDMGMults[3] /= buff.ValueY;
                    collosalDMGMults[2] /= buff.ValueY;
                    collosalDMGMults[3] /= buff.ValueY;
                    statusDMGMults[2] /= buff.ValueY;
                    statusDMGMults[3] /= buff.ValueY;
                    break;
            }
        }
    }
}
