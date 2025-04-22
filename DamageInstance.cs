using System;
using UnityEngine;

[Serializable]
public class DamageInstance
{
    public float damageVal;
    public AttackType attackType;
    public EnemyClass damageClass;

    public float critDMG, critChance;
}

public class DMGProcessor : MonoBehaviour
{
    public static DamageInstance SetupDamageInstance(float dmg, EnemyClass DMGClass, AttackType ATKType, float critChance, float critDMG)
    {
        DamageInstance damageInstance = new()
        {
            damageVal = dmg,
            critChance = critChance,
            critDMG = critDMG,
            damageClass = DMGClass,
            attackType = ATKType
        };

        return damageInstance;
    }

    public static DamageInstance SetupDamageInstance(float dmg, EnemyClass DMGClass, AttackType ATKType)
    {
        DamageInstance damageInstance = new()
        {
            damageVal = dmg,
            critChance = 0,
            critDMG = 0,
            damageClass = DMGClass,
            attackType = ATKType
        };

        return damageInstance;
    }

    public static DamageInstance SetupDamageInstance(float dmg, DamageInstance refInstance, bool replaceDMG = false, bool applyAsMult = false)//change the DMG instance DMG val
    {
        if(replaceDMG)
        {
            DamageInstance damageInstance = new()
            {
                damageVal = dmg,
                critChance = refInstance.critChance,
                critDMG = refInstance.critDMG,
                damageClass = refInstance.damageClass,
                attackType = refInstance.attackType
            };

            return damageInstance;
        }
        else if(!applyAsMult)
        {
            DamageInstance damageInstance = new()
            {
                damageVal = dmg + refInstance.damageVal,
                critChance = refInstance.critChance,
                critDMG = refInstance.critDMG,
                damageClass = refInstance.damageClass,
                attackType = refInstance.attackType
            };

            return damageInstance;
        }
        else
        {
            DamageInstance damageInstance = new()
            {
                damageVal = refInstance.damageVal * dmg,
                critChance = refInstance.critChance,
                critDMG = refInstance.critDMG,
                damageClass = refInstance.damageClass,
                attackType = refInstance.attackType
            };

            return damageInstance;
        }
    }

    public static DamageInstance SetupDamageInstance(DamageInstance refInstance, float critChance, float critDMG)
    {
        DamageInstance damageInstance = new()
        {
            damageVal = refInstance.damageVal,
            critChance = critChance + refInstance.critDMG,
            critDMG = critDMG + refInstance.critDMG,
            damageClass = refInstance.damageClass,
            attackType = refInstance.attackType
        };

        return damageInstance;
    }
}
