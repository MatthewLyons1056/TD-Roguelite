using UnityEngine;
using Sirenix.OdinInspector;
using DG.Tweening;
using System.Collections.Generic;
using System.Collections;

[System.Serializable]
public enum HealthType
{
    Health, Armor, MagicShield, Regenerator, Speed
}

[System.Serializable]
public enum EnemyClass
{
    Fodder, Standard, Large, Collosal, None
}

[System.Serializable]
public enum EnemyAbility
{
    None, Absorber, TowerShield
}

public class UnitHealth : MonoBehaviour
{
    public List<EnemyHealthPair> enemyHealthList = new();

    [FoldoutGroup("Health Data"), SerializeField] private float damageTaken;
    [FoldoutGroup("Health Data"), SerializeField, Range(0,999999)] internal float currentHealth;
    [FoldoutGroup("Health Data"), SerializeField] internal float maxHealth;
    [FoldoutGroup("Health Data")] public int priorityLevel;
    [FoldoutGroup("Health Data")] public EnemyClass enemyClass;
    [FoldoutGroup("Health Data")] public EnemyAbility enemyAbility;

    [FoldoutGroup("item drops"), SerializeField] private float xpValue;
    [FoldoutGroup("item drops"), SerializeField] private float goldValue;

    [FoldoutGroup("Refrences", 25)] public EnemyHealthUI healthUI;
    [FoldoutGroup("Refrences", 25)] public EnemyBuffData enemyBuffData;
    [FoldoutGroup("Refrences", 25)] public EnemyDataOBJ enemyDataOBJ;
    [FoldoutGroup("Refrences", 25)] public EnemyFloatingTextFeedBack setDamageText;

    internal bool isDead;

    public void Initalize(Enemy data, List<EnemyHealthPair> unitHealthList, float healthModifier, float goldAmount)
    {
        WaveSystemController.instance.activeEnemies.Add(this);
        healthUI.HideUI();
        enemyHealthList.Clear();
        isDead = false;
        damageTaken = 0;
        for (int i = 0; i != unitHealthList.Count; i++)
        {
            EnemyHealthPair pair = new()
            {
                healthVal = unitHealthList[i].healthVal,
                healthType = unitHealthList[i].healthType,
                maxHealth = unitHealthList[i].healthVal
            };
            enemyHealthList.Add(pair);
            maxHealth += enemyHealthList[i].healthVal;
            switch (enemyHealthList[i].healthType)
            {//apply buff regardless of position
                case HealthType.Speed:
                    CycleHealthEffect(enemyHealthList[i].healthType);
                    break;
            }
            if(i == 0)
            {//apply buff if in first slot
                switch (enemyHealthList[i].healthType)
                {
                    case HealthType.Regenerator:
                        CycleHealthEffect(enemyHealthList[i].healthType);
                        break;
                }
            }
        }
        currentHealth = maxHealth;

        priorityLevel = data.PriorityLevel;
        switch(data.EnemyClass)
        {
            case "Fodder":
                enemyClass = EnemyClass.Fodder;
                break;
            case "Standard":
                enemyClass = EnemyClass.Standard;
                break;
            case "Large":
                enemyClass = EnemyClass.Large;
                break;
            case "Collosal":
                enemyClass = EnemyClass.Collosal;
                break;
        }
        xpValue = data.XP;
        goldValue = goldAmount;

        enemyBuffData.GenerateSupportAI();

        healthUI.UIRectTrans.anchoredPosition = enemyDataOBJ.visData.healthUIPos.anchoredPosition;
        healthUI.SetupHealthBars(enemyHealthList, enemyClass, true);

        StartCoroutine(HealthVisualLoop());
    }


    //effects related to taking damage
    public void TakeDamage(DamageInstance DMGInstance, Tower_BuffData dmgSource, float dmgMult = 1)//take damage from a tower, takes in the tower that damaged them
    {
        if (!isDead)
        {
            float dmgAmount = DMGInstance.damageVal;
            float multiplier = 1;
            dmgAmount *= dmgMult;

            KeyValuePair<bool, float> critResult = CritCheck(dmgAmount, DMGInstance.critChance, DMGInstance.critDMG);
            dmgAmount = critResult.Value;
            multiplier = DamageClassCheck(multiplier, DMGInstance.damageClass, DMGInstance.attackType);
            foreach (EnemyHealthPair enemyHealthType in enemyHealthList)
            {
                if (enemyHealthType.healthVal > 0)
                {
                    multiplier = HealthTypeCheck(multiplier, enemyHealthType.healthType, DMGInstance.attackType);
                    break;
                }
            }

            dmgAmount *= multiplier;

            float overflowDMG = ApplyDMGToHealth(dmgAmount);
            while (overflowDMG < 0)
            {
                overflowDMG = ApplyDMGToHealth(Mathf.Abs(overflowDMG));
            }

            float healthRemaining = 0;
            foreach (EnemyHealthPair enemyHealthType in enemyHealthList)
            {
                if (enemyHealthType.healthVal > 0)
                {
                    healthRemaining += enemyHealthType.healthVal;
                }
            }
            currentHealth = healthRemaining;
            damageTaken += dmgAmount;


            if (dmgSource != null)
            {
                switch (DMGInstance.attackType)
                {
                    case AttackType.Projectile:
                        dmgSource.TriggerBuff("OnHit", "Projectile", enemyDataOBJ);
                        break;
                    case AttackType.Direct:
                        dmgSource.TriggerBuff("OnHit", "Projectile", enemyDataOBJ);
                        break;
                    case AttackType.AOE:
                        dmgSource.TriggerBuff("OnHit", "AOE", enemyDataOBJ);
                        break;
                    case AttackType.Status:
                        dmgSource.TriggerBuff("OnHit", "Status", enemyDataOBJ);
                        break;
                }
                dmgSource.UpdateDMGDealt(dmgAmount);
            }

            if (currentHealth <= 0 && !isDead)
            {
                isDead = true;
                KillUnit(false, dmgSource);
                currentHealth = 0;
                StatsController.Instance.UpdateKillCounter();
            }
            setDamageText.TriggerFeedback(dmgAmount, DMGInstance.attackType, critResult.Key);
        }
    }
    private KeyValuePair<bool, float> CritCheck(float dmg, float critChance, float critDamage)//Check initial damage for Crit effects
    {
        if (critChance >= Random.Range(1, 101))
        {
            dmg *= critDamage;
            return new KeyValuePair<bool, float>(true, dmg);
        }
        else
        {
            return new KeyValuePair<bool, float>(false, dmg);
        }
    }
    private float DamageClassCheck(float mult, EnemyClass damageClass, AttackType attackType)//check damage class of enemy and DMG source to modify the total DMG
    {
        float modifiedDamage = mult;

        switch (damageClass)
        {
            case EnemyClass.Fodder:
                switch(enemyClass)
                {
                    case EnemyClass.Fodder:
                        modifiedDamage += CombatController.Instance.fodderDMGMults[0];
                        break;
                    case EnemyClass.Standard:
                        modifiedDamage += CombatController.Instance.fodderDMGMults[1];
                        break;
                    case EnemyClass.Large:
                        modifiedDamage += CombatController.Instance.fodderDMGMults[2];
                        break;
                    case EnemyClass.Collosal:
                        modifiedDamage += CombatController.Instance.fodderDMGMults[3];
                        break;
                }
                break;
            case EnemyClass.Standard:
                switch (enemyClass)
                {
                    case EnemyClass.Fodder:
                        modifiedDamage += CombatController.Instance.standardDMGMults[0];
                        break;
                    case EnemyClass.Standard:
                        modifiedDamage += CombatController.Instance.standardDMGMults[1];
                        break;
                    case EnemyClass.Large:
                        modifiedDamage += CombatController.Instance.standardDMGMults[2];
                        break;
                    case EnemyClass.Collosal:
                        modifiedDamage += CombatController.Instance.standardDMGMults[3];
                        break;
                }
                break;
            case EnemyClass.Large:
                switch (enemyClass)
                {
                    case EnemyClass.Fodder:
                        modifiedDamage += CombatController.Instance.largeDMGMults[0];
                        break;
                    case EnemyClass.Standard:
                        modifiedDamage += CombatController.Instance.largeDMGMults[1];
                        break;
                    case EnemyClass.Large:
                        modifiedDamage += CombatController.Instance.largeDMGMults[2];
                        break;
                    case EnemyClass.Collosal:
                        modifiedDamage += CombatController.Instance.largeDMGMults[3];
                        break;
                }
                break;
            case EnemyClass.Collosal:
                switch (enemyClass)
                {
                    case EnemyClass.Fodder:
                        modifiedDamage += CombatController.Instance.collosalDMGMults[0];
                        break;
                    case EnemyClass.Standard:
                        modifiedDamage += CombatController.Instance.collosalDMGMults[1];
                        break;
                    case EnemyClass.Large:
                        modifiedDamage += CombatController.Instance.collosalDMGMults[2];
                        break;
                    case EnemyClass.Collosal:
                        modifiedDamage += CombatController.Instance.collosalDMGMults[3];
                        break;
                }
                break;
            case EnemyClass.None:

                break;
        }

        switch (attackType)
        {
            case AttackType.Status:
                switch (enemyClass)
                {
                    case EnemyClass.Fodder:
                        modifiedDamage += CombatController.Instance.statusDMGMults[0];
                        break;
                    case EnemyClass.Standard:
                        modifiedDamage += CombatController.Instance.statusDMGMults[1];
                        break;
                    case EnemyClass.Large:
                        modifiedDamage += CombatController.Instance.statusDMGMults[2];
                        break;
                    case EnemyClass.Collosal:
                        modifiedDamage += CombatController.Instance.statusDMGMults[3];
                        break;
                }
                break;
        }

        switch(enemyAbility)
        {
            case EnemyAbility.Absorber:
                if(attackType == AttackType.Direct)
                {
                    modifiedDamage += -.2f;
                }
                if (attackType == AttackType.Projectile)
                {
                    modifiedDamage += .2f;
                }
                break;
            case EnemyAbility.TowerShield:

                break;
        }

        return modifiedDamage;
    }
    private float HealthTypeCheck(float mult, HealthType healthType, AttackType attackType)//check highest health type for any damage Mults
    {
        switch(healthType)
        {
            case HealthType.Armor://takes x% less AOE and Status dmg and more Direct DMG
                if(attackType == AttackType.AOE || attackType == AttackType.Status)
                {
                    mult -= .4f;
                }
                else if (attackType == AttackType.Direct || attackType == AttackType.Projectile)
                {
                    mult += .2f;
                }
                break;
        }

        return mult;
    }
    private float ApplyDMGToHealth(float dmgToApply)//look for and apply DMG to the highest health pair above 0, return overflow DMG
    {
        for(int i = 0; i != enemyHealthList.Count; i++)
        {
            if (enemyHealthList[i].healthVal > 0)
            {
                enemyHealthList[i].healthVal -= dmgToApply;
                switch(enemyHealthList[i].healthType)
                {
                    case HealthType.MagicShield:
                        enemyBuffData.TriggerBuffPulse(HealthType.MagicShield, dmgToApply);
                        break;
                }

                if (enemyHealthList[i].healthVal <= 0)
                {
                    CycleHealthEffect(enemyHealthList[i].healthType, true);
                    float overflowDMG = enemyHealthList[i].healthVal;
                    enemyHealthList[i].healthVal = 0;
                    if(i + 1 != enemyHealthList.Count)
                    {
                        switch (enemyHealthList[i + 1].healthType)
                        {//enable new health effect
                            case HealthType.Regenerator:
                                CycleHealthEffect(enemyHealthList[i + 1].healthType);
                                break;
                        }
                    }

                    return overflowDMG;
                }

                return enemyHealthList[i].healthVal;
            }
        }

        return 0;
    }


    //apply or remove special effects and buffs/debuffs
    private void CycleHealthEffect(HealthType healthType, bool doRemove = false)//Apply or remove a health buff
    {
        if(doRemove)
        {
            switch (healthType)
            {
                case HealthType.Speed:
                    if(enemyBuffData.activeHasteBuff != null)
                    {
                        enemyBuffData.activeHasteBuff.buffDuration -= 900;
                    }
                    break;
                case HealthType.Regenerator:
                    if (enemyBuffData.activeRegenBuff != null)
                    {
                        enemyBuffData.activeRegenBuff.buffDuration -= 900;
                    }
                    break;
            }
        }
        else
        {
            BuffData buffToApply = new();
            switch (healthType)
            {
                case HealthType.Speed:
                    buffToApply.buffDuration = 900;
                    buffToApply.buffID = "Haste";

                    TriggerStatusEffect(buffToApply, null);
                    break;
                case HealthType.Regenerator:
                    buffToApply.buffDuration = 900;
                    buffToApply.buffID = "Regen";

                    TriggerStatusEffect(buffToApply, null);
                    break;
            }
        }
    }
    public void TriggerStatusEffect(BuffData effectToTrigger, Tower_BuffData sourceTower)
    {
        if(!isDead)
        {
            enemyBuffData.SetupDebuff(effectToTrigger, sourceTower);
        }
    }
    public void RegenHealth(float regenAmount, bool flatVal = false, HealthType healthType = HealthType.Health, bool boostMax = false)
    {
        if(healthType == HealthType.Health)
        {
            foreach (EnemyHealthPair enemyHealthType in enemyHealthList)
            {
                if (enemyHealthType.healthVal > 0)
                {
                    if (flatVal)
                    {
                        enemyHealthType.healthVal += regenAmount;
                        if (boostMax)
                        {
                            enemyHealthType.maxHealth += regenAmount;
                        }
                    }
                    else
                    {
                        enemyHealthType.healthVal += regenAmount * enemyHealthType.maxHealth;
                        if (boostMax)
                        {
                            enemyHealthType.maxHealth += regenAmount * enemyHealthType.maxHealth;
                        }
                    }

                    if (enemyHealthType.healthVal > enemyHealthType.maxHealth)
                    {
                        enemyHealthType.healthVal = enemyHealthType.maxHealth;
                    }
                    break;
                }
            }
        }
        else
        {
            foreach (EnemyHealthPair enemyHealthType in enemyHealthList)
            {
                if (enemyHealthType.healthType == healthType)
                {
                    if (flatVal)
                    {
                        enemyHealthType.healthVal += regenAmount;
                        if (boostMax)
                        {
                            enemyHealthType.maxHealth += regenAmount;      
                        }
                    }
                    else
                    {
                        enemyHealthType.healthVal += regenAmount * enemyHealthType.maxHealth;
                        if (boostMax)
                        {
                            enemyHealthType.maxHealth += regenAmount * enemyHealthType.maxHealth;
                        }
                    }

                    if (enemyHealthType.healthVal > enemyHealthType.maxHealth)
                    {
                        enemyHealthType.healthVal = enemyHealthType.maxHealth;
                    }
                    break;
                }
            }
        }
    }
    public void GainHealthBar(float val, HealthType healthType)
    {
        EnemyHealthPair pair = new()
        {
            healthVal = val,
            healthType = healthType,
            maxHealth = val
        };
        enemyHealthList.Insert(0, pair);

        //healthUI.SetupHealthBars(enemyHealthList, enemyClass);
    }


    //process to remove this enemy and add it back to the pool
    public void KillUnit(bool reachedTheTC = false, Tower_BuffData dmgSource = null)//move unit and disable unit functions along with provide xp before deleting it
    {
        WaveSystemController.instance.activeEnemies.Remove(this);
        enemyDataOBJ.aiPath.canMove = false;
        enemyDataOBJ.rvoController.layer = Pathfinding.RVO.RVOLayer.Layer10; 
        enemyDataOBJ.visData.collison.center = new Vector3(999,999,999);
        float timer = .5f;
        if (enemyDataOBJ.animator != null)
        {
            enemyDataOBJ.animator.Play("Die");
            AnimationClip deathAnim = FindAnim(enemyDataOBJ.animator, "Die");
            timer = deathAnim.length;
        }
        enemyDataOBJ.StopAllCoroutines();
        enemyDataOBJ.aiPath.distanceTraveled = 0;

        if (!reachedTheTC)
        {
            if(enemyBuffData.enemySupportAI != null)
            {
                enemyBuffData.enemySupportAI.TriggerBuff("OnKill");
            }
            float multAmount = 0;
            if (enemyBuffData.activeTransmutationDebuff != null)
            {
                multAmount = enemyBuffData.activeTransmutationDebuff.buffEffectX;
            }

            ResourceController.Instance.UpdateResources(goldValue * (1 + multAmount * CombatController.Instance.transmutation_BonusPerStack), false);
            if (dmgSource != null)
            {
                dmgSource.TriggerBuff("OnKill", transform.position);
            }
        }
        else
        {
            timer = 0;
        }

        enemyBuffData.CleanUpStatus();

        healthUI.HideUI();

        WaveSystemController.instance.activeEnemyCount -= 1;

        DOVirtual.DelayedCall(timer + .35f, () => RemoveEnemyOBJ(), false);
    }
    private void RemoveEnemyOBJ()
    {
        enemyDataOBJ.visData.collisonOBJ.SetActive(false);

        enemyDataOBJ.death_Feedback.PlayFeedbacks();
        if (enemyDataOBJ.visualTrans.childCount > 0)
        {
            Destroy(enemyDataOBJ.visualTrans.GetChild(0).gameObject, .5f);
        }

        enemyBuffData.enemySupportAI.targetingCollision.enabled = false;
        enemyBuffData.enemySupportAI.StopAllCoroutines();
        enemyBuffData.enemySupportAI.enemiesInRange.Clear();

        DOVirtual.DelayedCall(0.5f, () => gameObject.transform.parent.gameObject.SetActive(false));
    }



    private AnimationClip FindAnim(Animator animator, string name)
    {
        foreach (AnimationClip clip in animator.runtimeAnimatorController.animationClips)
        {
            if (clip.name == name)
            {
                return clip;
            }
        }

        return null;
    }
    private IEnumerator HealthVisualLoop()
    {
        while (!isDead)
        {
            yield return new WaitForSecondsRealtime(.1f);
            healthUI.SetupHealthBars(enemyHealthList, enemyClass);
        }
    }
}
