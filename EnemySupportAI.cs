using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemySupportAI : MonoBehaviour
{
    [FoldoutGroup("Support"), SerializeField] internal List<EnemyDataOBJ> enemiesInRange = new();
    [FoldoutGroup("Support")] public SphereCollider targetingCollision;
    [FoldoutGroup("Support"), SerializeField] private BuffData buffToTrigger;

    [TabGroup("Buff sources")] public List<BuffEffect> onKillBuffs = new();

    [TabGroup("Status Effects")] public List<StatusEffect_Data> enemyStatusEffects = new();

    public void InitalizeSupportBuff(BuffEffect buffToSetup)
    {
        enemyStatusEffects.Clear();
        onKillBuffs.Clear();

        switch (buffToSetup.ID)
        {
            case "BU_901":
                targetingCollision.enabled = true;
                targetingCollision.radius = buffToSetup.ValueX;
                buffToTrigger = new BuffData
                {
                    buffDuration = buffToSetup.Duration,
                    buffID = "Haste"
                };
                StartCoroutine(SupportEffectCycle(buffToSetup));
                break;
            case "BU_902":
                targetingCollision.enabled = true;
                targetingCollision.radius = buffToSetup.ValueX;
                buffToTrigger = new BuffData
                {
                    buffDuration = buffToSetup.Duration,
                    buffID = "Healing"
                };
                StartCoroutine(SupportEffectCycle(buffToSetup));
                break;
            case "BU_903":
                onKillBuffs.Add(buffToSetup);
                InitalizeStatusEffects("Haste", buffToSetup.Duration, buffToSetup.ValueY, "Enemy", buffToSetup.UID, 1);
                InitalizeStatusEffects("Burn", buffToSetup.Duration, buffToSetup.ValueY, "Enemy", buffToSetup.UID, 1);
                break;
        }
    }


    private IEnumerator SupportEffectCycle(BuffEffect buffToSetup)
    {
        yield return new WaitForSeconds(buffToSetup.ValueZ);

        while (true)
        {
            switch (buffToSetup.ID)
            {
                case "BU_901":
                    foreach(EnemyDataOBJ enemy in enemiesInRange)
                    {
                        enemy.unitHealth.TriggerStatusEffect(buffToTrigger, null);
                    }
                    break;
                case "BU_902":
                    foreach (EnemyDataOBJ enemy in enemiesInRange)
                    {
                        enemy.unitHealth.TriggerStatusEffect(buffToTrigger, null);
                    }
                    break;
            }
            yield return new WaitForSeconds(buffToSetup.ValueY);
        }
    }

    public void BuffPulse(HealthType healthType, float val)
    {
        List<EnemyDataOBJ> targetList = new();

        switch (healthType)
        {
            case HealthType.MagicShield:
                targetList = UnitSearch.FindNearEnemiesList(transform.position, CombatController.Instance.magicShield_TargetLimit, CombatController.Instance.magicShield_SearchRange);
                bool appliedRegen = false;
                foreach(EnemyDataOBJ enemy in targetList)
                {
                    appliedRegen = false;
                    foreach(EnemyHealthPair health in enemy.unitHealth.enemyHealthList)
                    {
                        if(health.healthType == HealthType.Armor)
                        {
                            enemy.unitHealth.RegenHealth(val * CombatController.Instance.magicShield_EffectVal, true, HealthType.Armor, true);
                            appliedRegen = true;
                            break;
                        }
                    }
                    if (!appliedRegen)
                    {
                        enemy.unitHealth.GainHealthBar(val * CombatController.Instance.magicShield_EffectVal, HealthType.Armor);
                    }
                }
                break;
        }
    }


    //trigger events
    private void OnTriggerEnter(Collider other)//add a target to the list when in range
    {
        if (other.CompareTag("Enemy"))
        {
            EnemyDataOBJ enemyData = other.GetComponentInParent<EnemyDataOBJ>();
            enemiesInRange.Add(enemyData);
        }
    }
    private void OnTriggerExit(Collider other)//remove a target from the list when out of range
    {
        if (other.CompareTag("Enemy"))
        {
            EnemyDataOBJ enemyData = other.GetComponentInParent<EnemyDataOBJ>();
            enemiesInRange.Remove(enemyData);
        }
    }


    //trigger buffs
    public void TriggerBuff(string buffType)
    {
        switch(buffType)
        {
            case "OnKill":
                foreach(BuffEffect buff in onKillBuffs)
                {
                    switch (buff.ID)
                    {
                        case "BU_903"://leave a linger AOE on death
                            Linger_Data linger;
                            linger = new()
                            {
                                Length_Size = buff.ValueX,
                                Width_Size = buff.ValueX,
                                DMG_Percent = buff.ValueY,
                                Duration = buff.ValueZ
                            };

                            CreateBuffLinger(linger, transform.position, 1);
                            break;
                    }
                }
                break;
        }
    }


    //create AOE effects
    private void CreateBuffLinger(Linger_Data linger_Data, Vector3 spawnPoint, float damagePercent)
    {
        if (linger_Data.DMG_Percent > 0)
        {
            GameObject linger_OBJ = ProjectilePoolingManager.Instance.GetPooledObject("Linger");
            if (linger_OBJ == null)
            {
                linger_OBJ = Instantiate(LibraryLink.Instance.dataLibrary.lingerAOE, ProjectilePoolingManager.Instance.lingerHolder);
                ProjectilePoolingManager.Instance.AddObjectToPool("Linger", linger_OBJ);
            }

            ProjectileInteractions linger_Interactions = linger_OBJ.GetComponent<ProjectileInteractions>();
            linger_OBJ.transform.SetPositionAndRotation(spawnPoint, Quaternion.identity);
            linger_OBJ.SetActive(true);
            linger_Interactions.canInteract = true;

            if (linger_Interactions != null)
            {
                linger_Interactions.pierceVal = 99999;
                linger_Interactions.damageInstance = DMGProcessor.SetupDamageInstance(linger_Data.DMG_Percent * damagePercent, EnemyClass.Fodder, AttackType.AOE);

                foreach (StatusEffect_Data statusEffect in enemyStatusEffects)
                {
                    StatusEffect_Data newStatus = new()
                    {
                        buffData = new()
                        {
                            buffDuration = statusEffect.buffData.buffDuration * linger_Data.DMG_Percent * damagePercent / .2f,
                            buffEffectX = statusEffect.buffData.buffEffectX * linger_Data.DMG_Percent * damagePercent / .2f,
                            buffID = statusEffect.buffData.buffID
                        },
                    };
                    linger_Interactions.statusEffects.Add(newStatus);
                }

                linger_Interactions.enemiesInRange.Clear();
                linger_Interactions.name = $"Enemy Linger: {gameObject.name}";
                linger_Interactions.gameObject.transform.localScale = new Vector3(linger_Data.Width_Size * damagePercent, .2f, linger_Data.Length_Size * damagePercent);
                linger_Interactions.sourceTowerBuff = null;
                linger_Interactions.StartCoroutine(linger_Interactions.TriggerLinger(linger_Data.Duration));
            }
        }
    }


    //initalization
    private void InitalizeStatusEffects(string effectID, float duration, float effectX, string applicationMethod, string sourceID, float BaseModifier = 0)
    {
        StatusEffect_Data statusEffect = new()
        {
            buffData = new BuffData
            {
                buffDuration = duration,
                buffEffectX = effectX, //current power of the status effect, scales with stats and base modifier
                buffID = effectID
            },
            applicationMethod = applicationMethod,
            sourceID = sourceID,
            baseModifier = BaseModifier  //base modifier allows the status to scale whenever the towers damage value changes 
        };

        switch (statusEffect.applicationMethod)
        {
            case "Enemy":
                enemyStatusEffects.Add(statusEffect);
                break;
        }
    }
}
