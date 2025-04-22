using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyBuffData : MonoBehaviour
{
    [System.Serializable]
    public enum BuffToTest
    {
        Burn, Slow, Frozen, Smite, Bleed, Thorns, Stagger, Galvanized, Haste, Regen, Transmutation
    }
    public BuffToTest buffType;

    public EnemyDataOBJ enemyDataOBJ;
    public EnemySupportAI enemySupportAI;

    internal BuffData activeBurnDebuff = null;
    internal BuffData activeSlowDebuff = null;
    internal BuffData activeFrozenDebuff = null;
    internal BuffData activeSmiteDebuff = null;
    internal BuffData activeBleedDebuff = null;
    internal BuffData activeThornsDebuff = null;
    internal BuffData activeStaggerDebuff = null;
    internal BuffData activeGalvanizedDebuff = null;
    internal BuffData activeTransmutationDebuff = null;

    public List<Material> debuffMats = new();

    internal BuffData activeHasteBuff = null;
    internal BuffData activeRegenBuff = null;

    internal Observable<float> speedPercent = new();
    private ProjectileInteractions activeThornsOBJ;
    private float smiteDecayTimer;

    void Start()
    {
        speedPercent.Changed += OnSpeedChanged;
    }

    public void SetupDebuff(BuffData debuff, Tower_BuffData debuffSource)
    {
        switch (debuff.buffID)
        {
            case "Burn":
                if(activeBurnDebuff == null)
                {
                    if(!CombatController.Instance.burn_CanApplyToCold)
                    {
                        if(activeFrozenDebuff != null || activeSlowDebuff != null)
                        {
                            return;
                        }
                    }
                    activeBurnDebuff = new BuffData
                    {
                        buffDuration = debuff.buffDuration * CombatController.Instance.burn_DurationBonus,
                        buffEffectX = debuff.buffEffectX * CombatController.Instance.burn_DMGBonus,
                        buffID = debuff.buffID,
                        damageInstance = DMGProcessor.SetupDamageInstance(debuff.buffEffectX * CombatController.Instance.burn_DMGBonus, EnemyClass.None, AttackType.Status)
                    };
                    StartCoroutine(Burned(debuffSource));
                }
                else
                {
                    activeBurnDebuff.buffDuration += debuff.buffDuration * CombatController.Instance.burn_DurationBonus;
                    activeBurnDebuff.buffEffectX += debuff.buffEffectX * CombatController.Instance.burn_DMGBonus;
                    activeBurnDebuff.damageInstance = DMGProcessor.SetupDamageInstance(debuff.buffEffectX * CombatController.Instance.burn_DMGBonus, activeBurnDebuff.damageInstance);
                    if (activeBurnDebuff.buffDuration > CombatController.Instance.burn_MaxDuration)
                    {
                        activeBurnDebuff.buffDuration = CombatController.Instance.burn_MaxDuration;
                    }
                }
                break;
            case "Slow":
                if (!CombatController.Instance.slow_CanApplyToHot)
                {
                    if (activeBurnDebuff != null)
                    {
                        return;
                    }
                }
                if (enemyDataOBJ.unitHealth.enemyClass <= CombatController.Instance.slow_TargetLimit)
                {
                    if (activeSlowDebuff == null)
                    {
                        activeSlowDebuff = new BuffData
                        {
                            buffDuration = debuff.buffDuration * CombatController.Instance.slow_DurationBonus,
                            buffEffectX = CombatController.Instance.slow_EffectVal,
                            buffID = debuff.buffID
                        };
                        StartCoroutine(Slowed());
                    }
                    else
                    {
                        activeSlowDebuff.buffDuration += debuff.buffDuration * CombatController.Instance.slow_DurationBonus;
                        activeSlowDebuff.buffEffectX = CombatController.Instance.slow_EffectVal;
                        if (activeSlowDebuff.buffDuration > CombatController.Instance.slow_MaxDuration)
                        {
                            activeSlowDebuff.buffDuration = CombatController.Instance.slow_MaxDuration;
                        }
                    }
                }
                break;
            case "Frozen":
                if (!CombatController.Instance.frozen_CanApplyToHot)
                {
                    if (activeBurnDebuff != null)
                    {
                        return;
                    }
                }
                if (enemyDataOBJ.unitHealth.enemyClass <= CombatController.Instance.frozen_TargetLimit)
                {
                    if (activeFrozenDebuff == null)
                    {
                        activeFrozenDebuff = new BuffData
                        {
                            buffDuration = debuff.buffDuration * CombatController.Instance.frozen_DurationBonus,
                            buffEffectX = -2,
                            buffID = debuff.buffID
                        };
                        StartCoroutine(Frozen());
                    }
                    else
                    {
                        activeFrozenDebuff.buffDuration += debuff.buffDuration * CombatController.Instance.frozen_DurationBonus;
                        activeFrozenDebuff.buffEffectX = -2;
                        if (activeFrozenDebuff.buffDuration > CombatController.Instance.frozen_MaxDuration)
                        {
                            activeFrozenDebuff.buffDuration = CombatController.Instance.frozen_MaxDuration;
                        }
                    }
                }
                break;
            case "Smite":
                if (activeSmiteDebuff == null)
                {
                    activeSmiteDebuff = new BuffData
                    {
                        buffDuration = 1,
                        buffEffectX = debuff.buffEffectX * CombatController.Instance.smite_DMGBonus,
                        buffID = debuff.buffID,
                        damageInstance = DMGProcessor.SetupDamageInstance(debuff.buffEffectX * CombatController.Instance.smite_DMGBonus, EnemyClass.None, AttackType.AOE)
                    };
                    StartCoroutine(Smite(debuffSource));
                }
                else
                {
                    activeSmiteDebuff.buffDuration += 1;
                    activeSmiteDebuff.buffEffectX += debuff.buffEffectX * CombatController.Instance.smite_DMGBonus;
                    activeSmiteDebuff.damageInstance = DMGProcessor.SetupDamageInstance(debuff.buffEffectX * CombatController.Instance.smite_DMGBonus, activeSmiteDebuff.damageInstance);
                }
                break;
            case "Bleed":
                if (activeBleedDebuff == null)
                {
                    activeBleedDebuff = new BuffData
                    {
                        buffDuration = debuff.buffDuration * CombatController.Instance.bleed_DurationBonus,
                        buffEffectX = debuff.buffEffectX * CombatController.Instance.bleed_DMGBonus,
                        buffID = debuff.buffID,
                        damageInstance = DMGProcessor.SetupDamageInstance(debuff.buffEffectX * CombatController.Instance.bleed_DMGBonus, EnemyClass.None, AttackType.Status)
                    };
                    StartCoroutine(Bleed(debuffSource));
                }
                else
                {
                    activeBleedDebuff.buffDuration += debuff.buffDuration * CombatController.Instance.bleed_DurationBonus;

                    if(debuff.buffEffectX * CombatController.Instance.bleed_DMGBonus > activeBleedDebuff.buffEffectX)
                    {
                        activeBleedDebuff.damageInstance = DMGProcessor.SetupDamageInstance(debuff.buffEffectX * CombatController.Instance.bleed_DMGBonus, EnemyClass.None, AttackType.Status);
                    }

                    if (activeBleedDebuff.buffDuration > CombatController.Instance.bleed_MaxDuration)
                    {
                        activeBleedDebuff.buffDuration = CombatController.Instance.bleed_MaxDuration;
                    }
                }
                break;
            case "Thorns":
                if (activeThornsDebuff == null)
                {
                    activeThornsDebuff = new BuffData
                    {
                        buffDuration = debuff.buffDuration * CombatController.Instance.thorns_DurationBonus,
                        buffEffectX = debuff.buffEffectX * CombatController.Instance.thorns_DMGBonus,
                        buffID = debuff.buffID,
                        damageInstance = DMGProcessor.SetupDamageInstance(debuff.buffEffectX * CombatController.Instance.thorns_DMGBonus, EnemyClass.None, AttackType.AOE)
                    };
                    StartCoroutine(Thorns(debuffSource));
                }
                else if (activeThornsOBJ != null)
                {
                    activeThornsDebuff.buffDuration += debuff.buffDuration * CombatController.Instance.thorns_DurationBonus;
                    activeThornsDebuff.buffEffectX += debuff.buffEffectX * CombatController.Instance.thorns_DMGBonus;
                    activeThornsOBJ.damageInstance = DMGProcessor.SetupDamageInstance(debuff.buffEffectX * CombatController.Instance.thorns_DMGBonus, activeThornsOBJ.damageInstance);
                    if (activeThornsDebuff.buffDuration > CombatController.Instance.thorns_MaxDuration)
                    {
                        activeThornsDebuff.buffDuration = CombatController.Instance.thorns_MaxDuration;
                    }
                }
                break;
            case "Stagger":
                if (enemyDataOBJ.unitHealth.enemyClass > EnemyClass.Standard)
                {
                    if (activeStaggerDebuff == null)
                    {
                        activeStaggerDebuff = new BuffData
                        {
                            buffDuration = CombatController.Instance.stagger_Window,
                            buffEffectX = debuff.buffEffectX * CombatController.Instance.stagger_DMGBonus,
                            buffID = debuff.buffID
                        };
                        StartCoroutine(Stagger());
                    }
                    else
                    {
                        activeStaggerDebuff.buffDuration = CombatController.Instance.stagger_Window;
                        activeStaggerDebuff.buffEffectX += debuff.buffEffectX * CombatController.Instance.stagger_DMGBonus;
                    }
                }
                break;
            case "Galvanized":
                if (activeGalvanizedDebuff == null)
                {
                    activeGalvanizedDebuff = new BuffData
                    {
                        buffDuration = 10,
                        buffEffectX = debuff.buffEffectX,
                        buffID = debuff.buffID,
                        damageInstance = DMGProcessor.SetupDamageInstance(debuff.buffEffectX, EnemyClass.None, AttackType.Direct)
                    };
                    StartCoroutine(Galvanized(debuffSource));
                }
                else
                {
                    activeGalvanizedDebuff.buffEffectX += debuff.buffEffectX;
                    activeGalvanizedDebuff.damageInstance = DMGProcessor.SetupDamageInstance(debuff.buffEffectX, activeGalvanizedDebuff.damageInstance);
                }
                break;
            case "Haste":
                if (activeHasteBuff == null)
                {
                    activeHasteBuff = new BuffData
                    {
                        buffDuration = debuff.buffDuration,
                        buffEffectX = CombatController.Instance.haste_EffectVal,
                        buffID = debuff.buffID
                    };
                    StartCoroutine(Haste());
                }
                else
                {
                    activeHasteBuff.buffDuration += debuff.buffDuration;
                    activeHasteBuff.buffEffectX = CombatController.Instance.haste_EffectVal;
                }
                break;
            case "Regen":
                if (activeRegenBuff == null)
                {
                    activeRegenBuff = new BuffData
                    {
                        buffDuration = debuff.buffDuration,
                        buffEffectX = CombatController.Instance.regen_EffectVal,
                        buffID = debuff.buffID
                    };
                    StartCoroutine(Regen());
                }
                else
                {
                    activeRegenBuff.buffDuration += debuff.buffDuration;
                    activeRegenBuff.buffEffectX = CombatController.Instance.regen_EffectVal;
                }
                break;
            case "Transmutation":
                if (activeTransmutationDebuff == null)
                {
                    activeTransmutationDebuff = new BuffData
                    {
                        buffDuration = debuff.buffDuration,
                        buffEffectX = 1,
                        buffID = debuff.buffID,
                        damageInstance = null
                    };
                    StartCoroutine(Transmutation());
                }
                else
                {
                    activeTransmutationDebuff.buffDuration += debuff.buffDuration;
                    activeTransmutationDebuff.buffEffectX += 1;;
                    if (activeTransmutationDebuff.buffEffectX > CombatController.Instance.transmutation_StackLimit)
                    {
                        activeTransmutationDebuff.buffEffectX = CombatController.Instance.transmutation_StackLimit;
                    }
                }
                break;
        }
    }


    private IEnumerator Burned(Tower_BuffData debuffSource)//BURNED causes enemies to take half of the burn damage every .5 seconds as MAGIC, DMG goes up by 2% per tick
    {
        UpdateTargetVisuals();
        while (activeBurnDebuff.buffDuration > 0)
        {
            enemyDataOBJ.unitHealth.TakeDamage(activeBurnDebuff.damageInstance, debuffSource);
            yield return new WaitForSeconds(CombatController.Instance.burn_TimeTillDMG);
            activeBurnDebuff.damageInstance = DMGProcessor.SetupDamageInstance(activeBurnDebuff.damageInstance.damageVal * CombatController.Instance.burn_DMGMult, activeBurnDebuff.damageInstance, true);
            activeBurnDebuff.buffDuration -= CombatController.Instance.burn_TimeTillDMG;
        }

        activeBurnDebuff = null;
        UpdateTargetVisuals();
    }
    private IEnumerator Slowed()//SLOWED reduces move speed by 30% for it's duration, does not affect collosal enemies
    {
        UpdateTargetVisuals();
        speedPercent.Value += activeSlowDebuff.buffEffectX;

        while (activeSlowDebuff.buffDuration > 0)
        {
            yield return new WaitForSeconds(1f);
            activeSlowDebuff.buffDuration -= 1;
        }

        speedPercent.Value -= activeSlowDebuff.buffEffectX;

        activeSlowDebuff = null;
        UpdateTargetVisuals();
    }
    private IEnumerator Frozen()//FROZEN prevents any movement from the enemy for it's duration, does not affect large and above enemies, goes into a 2 second cooldown
    {
        UpdateTargetVisuals();
        speedPercent.Value += activeFrozenDebuff.buffEffectX;

        while (activeFrozenDebuff.buffDuration > 0)
        {
            yield return new WaitForSeconds(1f);
            activeFrozenDebuff.buffDuration -= 1;
        }

        speedPercent.Value -= activeFrozenDebuff.buffEffectX;

        yield return new WaitForSeconds(2);

        activeFrozenDebuff = null;
        UpdateTargetVisuals();
    }
    private IEnumerator Smite(Tower_BuffData debuffSource)//SMITE causes an enemy to create AOEs on itself every 2s dealing holy damage in a 2m radius
    {
        UpdateTargetVisuals();
        smiteDecayTimer = CombatController.Instance.smite_TimeTillDetonate;

        while (smiteDecayTimer > 0)
        {
            yield return new WaitForEndOfFrame();

            smiteDecayTimer -= Time.deltaTime;

            if(activeSmiteDebuff.buffDuration >= CombatController.Instance.smite_MaxStacks || smiteDecayTimer <= 0)
            {
                GameObject aoe_OBJ = ProjectilePoolingManager.Instance.GetPooledObject("Smite");
                if (aoe_OBJ == null)
                {
                    aoe_OBJ = Instantiate(LibraryLink.Instance.dataLibrary.smiteAOE, ProjectilePoolingManager.Instance.smiteHolder);
                    ProjectilePoolingManager.Instance.AddObjectToPool("Smite", aoe_OBJ);
                }

                ProjectileInteractions AOE_Interactions = aoe_OBJ.GetComponent<ProjectileInteractions>();
                aoe_OBJ.transform.SetPositionAndRotation(enemyDataOBJ.visData.centerPoint.position, Quaternion.identity);
                aoe_OBJ.SetActive(true);
                AOE_Interactions.canInteract = true;

                if (AOE_Interactions != null)
                {
                    AOE_Interactions.pierceVal = 99999;

                    AOE_Interactions.damageInstance = activeSmiteDebuff.damageInstance;
                    AOE_Interactions.name = $"Smite AOE: {gameObject.name}";
                    AOE_Interactions.gameObject.transform.localScale = new Vector3(CombatController.Instance.smite_Size + (activeSmiteDebuff.buffDuration * CombatController.Instance.smite_SizePerStack), CombatController.Instance.smite_Size + (activeSmiteDebuff.buffDuration * CombatController.Instance.smite_SizePerStack), CombatController.Instance.smite_Size + (activeSmiteDebuff.buffDuration * CombatController.Instance.smite_SizePerStack));
                    AOE_Interactions.sourceTowerBuff = debuffSource;
                    AOE_Interactions.StartCoroutine(AOE_Interactions.TriggerAOE());
                }

                smiteDecayTimer = 0;
            }
        }

        activeSmiteDebuff = null;
        UpdateTargetVisuals();
    }
    private IEnumerator Bleed(Tower_BuffData debuffSource)//BLEED causes an enemy to take damage based on distance traveled, checks every 1.5s
    {
        UpdateTargetVisuals();
        float distanceTraveled = 0;
        float previousDistance = 0;

        while (activeBleedDebuff.buffDuration > 0)
        {
            distanceTraveled = 0;
            previousDistance = enemyDataOBJ.aiPath.distanceTraveled;

            yield return new WaitForSeconds(1f);

            distanceTraveled = enemyDataOBJ.aiPath.distanceTraveled - previousDistance;
            if(distanceTraveled < 0)
            {
                distanceTraveled = 0;
            }
            activeBleedDebuff.buffDuration -= 1f;
            enemyDataOBJ.unitHealth.TakeDamage(activeBleedDebuff.damageInstance, debuffSource, distanceTraveled);
            yield return new WaitForSeconds(.5f);
            activeBleedDebuff.buffDuration -= .5f;
        }

        activeBleedDebuff = null;
        UpdateTargetVisuals();
    }
    private IEnumerator Thorns(Tower_BuffData debuffSource)//THORNS attaches a linger AOE to the enemy dealing damage to self and nearby enemies
    {
        yield return new WaitForSeconds(.1f);

        GameObject linger_OBJ = ProjectilePoolingManager.Instance.GetPooledObject("Thorns");
        if (linger_OBJ == null)
        {
            linger_OBJ = Instantiate(LibraryLink.Instance.dataLibrary.thornsAOE, ProjectilePoolingManager.Instance.lingerHolder);
            ProjectilePoolingManager.Instance.AddObjectToPool("Thorns", linger_OBJ);
        }

        activeThornsOBJ = linger_OBJ.GetComponent<ProjectileInteractions>();
        linger_OBJ.transform.SetPositionAndRotation(enemyDataOBJ.visData.centerPoint.position, Quaternion.identity);
        linger_OBJ.transform.parent = enemyDataOBJ.visData.centerPoint;
        linger_OBJ.SetActive(true);
        activeThornsOBJ.canInteract = true;

        if (activeThornsOBJ != null)
        {
            activeThornsOBJ.pierceVal = 99999;

            activeThornsOBJ.enemiesInRange.Clear();
            activeThornsOBJ.damageInstance = activeThornsDebuff.damageInstance;
            activeThornsOBJ.name = $"Thorns AOE: {gameObject.name}";
            activeThornsOBJ.gameObject.transform.localScale = new Vector3(enemyDataOBJ.visData.collison.radius * 1.25f * CombatController.Instance.thorns_SizeBonus, enemyDataOBJ.visData.collison.height, enemyDataOBJ.visData.collison.radius * 1.25f * CombatController.Instance.thorns_SizeBonus);
            activeThornsOBJ.sourceTowerBuff = debuffSource;
            activeThornsOBJ.StartCoroutine(activeThornsOBJ.TriggerLinger(9999));
        }

        while (activeThornsDebuff.buffDuration > 0)
        {
            yield return new WaitForSeconds(.5f);


            activeThornsDebuff.buffDuration -= .5f;
        }

        activeThornsOBJ.transform.parent = ProjectilePoolingManager.Instance.lingerHolder;
        activeThornsOBJ.StopAllCoroutines();
        activeThornsOBJ.TriggerDetonationEvents();
        activeThornsDebuff.buffDuration = 0;

        activeThornsDebuff = null;
    }
    private IEnumerator Stagger()//STAGGER builds up to 100 and fully decays after 2s without build-up, when full cause a stun for 1s, after the stun the unit moves 30% slower for 3s -- only affects large and above units
    {
        UpdateTargetVisuals();
        while (activeStaggerDebuff.buffDuration > 0)
        {
            yield return new WaitForEndOfFrame();

            activeStaggerDebuff.buffDuration -= Time.deltaTime;

            if (activeStaggerDebuff.buffEffectX >= 100)
            {
                speedPercent.Value -= 2;
                yield return new WaitForSeconds(CombatController.Instance.stagger_StunDuration);//stun duration
                speedPercent.Value += 2 + CombatController.Instance.stagger_SlowVal;
                yield return new WaitForSeconds(CombatController.Instance.stagger_SlowDuration);//slow duration
                speedPercent.Value += CombatController.Instance.stagger_SlowVal;
                yield return new WaitForSeconds(CombatController.Instance.stagger_Cooldown);//cooldown
                activeStaggerDebuff.buffDuration = 0;
                break;
            }
        }

        activeStaggerDebuff = null;
        UpdateTargetVisuals();
    }
    private IEnumerator Galvanized(Tower_BuffData debuffSource)//GALVANIZED decays over time, at fixed intervals the enemy fires off a chaining bolt every few seconds, once full the enemy take a massive damage burst and has a long cooldown before another application
    {
        UpdateTargetVisuals();
        List<EnemyDataOBJ> targetEnemy = new();

        for(int i = 0; i != CombatController.Instance.galvanized_ChainCount; i++)
        {
            yield return new WaitForSeconds(CombatController.Instance.galvanized_ChainCooldown);

            targetEnemy = UnitSearch.FindNearEnemiesList(transform.position, 6, 3f);
            for (int z = 0; z < targetEnemy.Count; z++)
            {
                targetEnemy[z].unitHealth.TakeDamage(activeGalvanizedDebuff.damageInstance , debuffSource, CombatController.Instance.galvanized_ChainDMGBonus);
            }
        }

        yield return new WaitForSeconds(.5f);

        enemyDataOBJ.unitHealth.TakeDamage(DMGProcessor.SetupDamageInstance(activeGalvanizedDebuff.buffEffectX, EnemyClass.Large, AttackType.Direct), debuffSource, CombatController.Instance.galvanized_FinishDMGBonus);

        yield return new WaitForSeconds(CombatController.Instance.galvanized_Cooldown);

        activeGalvanizedDebuff = null;
        UpdateTargetVisuals();
    }
    private IEnumerator Transmutation()
    {
        while (activeTransmutationDebuff.buffDuration > 0)
        {
            yield return new WaitForSeconds(1f);
            activeTransmutationDebuff.buffDuration -= 1;
        }

        activeTransmutationDebuff = null;
    }


    private IEnumerator Haste()//HASTE increase enemy move speed by 35%
    {
        yield return new WaitForSeconds(.75f);
        activeHasteBuff.buffDuration += .5f;
        speedPercent.Value += activeHasteBuff.buffEffectX;

        while (activeHasteBuff.buffDuration > 0)
        {
            yield return new WaitForSeconds(1f);
            activeHasteBuff.buffDuration -= 1;
        }

        speedPercent.Value -= activeHasteBuff.buffEffectX;

        activeHasteBuff = null;
    }
    private IEnumerator Regen()//REGEN heals a health pair up to max health at a rate of 5% per second
    {
        while (activeRegenBuff.buffDuration > 0)
        {
            yield return new WaitForSeconds(1f);
            activeRegenBuff.buffDuration -= 1;
            enemyDataOBJ.unitHealth.RegenHealth(activeRegenBuff.buffEffectX);
        }

        activeRegenBuff = null;
    }


    public void GenerateSupportAI()
    {
        enemySupportAI.InitalizeSupportBuff(enemyDataOBJ.enemyData.buffOBJ);
    }
    public void TriggerBuffPulse(HealthType healthType, float val)
    {
        enemySupportAI.BuffPulse(healthType, val);
    }


    void OnSpeedChanged(object target, Observable<float>.ChangedEventArgs args)
    {
        enemyDataOBJ.aiPath.maxSpeed = (speedPercent.Value + 1) * enemyDataOBJ.aiPath.savedMaxSpeed;

        if(enemyDataOBJ.unitHealth.isDead)
        {
            enemyDataOBJ.animator.speed = 1;
        }
        else if(speedPercent.Value + 1 <= 0)
        {
            enemyDataOBJ.animator.speed = 0;
            enemyDataOBJ.aiPath.enableRotation = false;
        }
        else
        {
            enemyDataOBJ.animator.speed = speedPercent.Value + 1;
            enemyDataOBJ.aiPath.enableRotation = true;
        }
    }
    public void UpdateTargetVisuals()//update enemy for debuff visuals
    {
        BuffToTest visualToApply = BuffToTest.Haste;
        while(visualToApply == BuffToTest.Haste)
        {
            if (activeStaggerDebuff != null)
            {
                visualToApply = BuffToTest.Stagger;
                break;
            }

            if (activeSlowDebuff != null || activeFrozenDebuff != null)
            {
                visualToApply = BuffToTest.Frozen;
                break;
            }

            if (activeSmiteDebuff != null)
            {
                visualToApply = BuffToTest.Smite;
                break;
            }

            if (activeGalvanizedDebuff != null)
            {
                visualToApply = BuffToTest.Galvanized;
                break;
            }

            if (activeBleedDebuff != null)
            {
                visualToApply = BuffToTest.Bleed;
                break;
            }

            if (activeBurnDebuff != null)
            {
                visualToApply = BuffToTest.Burn;
                break;
            }

            break;
        }

        List<Material> newEnemyMats = new()
        {
            enemyDataOBJ.visData.enemyMesh.materials[0]
        };

        switch (visualToApply)
        {
            case BuffToTest.Stagger:
                newEnemyMats.Add(debuffMats[0]);
                break;
            case BuffToTest.Frozen:
                newEnemyMats.Add(debuffMats[1]);
                break;
            case BuffToTest.Smite:
                newEnemyMats.Add(debuffMats[2]);
                break;
            case BuffToTest.Galvanized:
                newEnemyMats.Add(debuffMats[3]);
                break;
            case BuffToTest.Bleed:
                newEnemyMats.Add(debuffMats[4]);
                break;
            case BuffToTest.Burn:
                newEnemyMats.Add(debuffMats[5]);
                break;
        }

        enemyDataOBJ.visData.enemyMesh.SetMaterials(newEnemyMats);
    }

    public void CleanUpStatus()
    {
        StopAllCoroutines();

        speedPercent.Value = 0;

        activeBurnDebuff = null;
        activeBleedDebuff = null;
        activeFrozenDebuff = null;
        activeGalvanizedDebuff = null;
        activeSlowDebuff = null;
        activeSlowDebuff = null;
        activeStaggerDebuff = null;
        activeThornsDebuff = null;
        activeTransmutationDebuff = null;

        activeHasteBuff = null;
        activeRegenBuff = null;

        if (activeThornsOBJ != null)
        {
            activeThornsOBJ.transform.parent = ProjectilePoolingManager.Instance.thornsHolder;
            activeThornsOBJ.StopAllCoroutines();
            activeThornsOBJ.TriggerDetonationEvents();
        }
        if(activeSmiteDebuff != null)
        {
            GameObject aoe_OBJ = ProjectilePoolingManager.Instance.GetPooledObject("Smite");
            if (aoe_OBJ == null)
            {
                aoe_OBJ = Instantiate(LibraryLink.Instance.dataLibrary.smiteAOE, ProjectilePoolingManager.Instance.smiteHolder);
                ProjectilePoolingManager.Instance.AddObjectToPool("Smite", aoe_OBJ);
            }

            ProjectileInteractions AOE_Interactions = aoe_OBJ.GetComponent<ProjectileInteractions>();
            aoe_OBJ.transform.SetPositionAndRotation(enemyDataOBJ.visData.centerPoint.position, Quaternion.identity);
            aoe_OBJ.SetActive(true);
            AOE_Interactions.canInteract = true;

            if (AOE_Interactions != null)
            {
                AOE_Interactions.pierceVal = 99999;
                
                AOE_Interactions.damageInstance = activeSmiteDebuff.damageInstance;
                AOE_Interactions.name = $"Smite AOE: {gameObject.name}";
                AOE_Interactions.gameObject.transform.localScale = new Vector3(CombatController.Instance.smite_Size + (activeSmiteDebuff.buffDuration * CombatController.Instance.smite_SizePerStack), CombatController.Instance.smite_Size + (activeSmiteDebuff.buffDuration * CombatController.Instance.smite_SizePerStack), CombatController.Instance.smite_Size + (activeSmiteDebuff.buffDuration * CombatController.Instance.smite_SizePerStack));
                AOE_Interactions.sourceTowerBuff = null;
                AOE_Interactions.StartCoroutine(AOE_Interactions.TriggerAOE());
            }

            activeSmiteDebuff = null;
        }
    }


    [Button]
    public void TriggerAnEffect()
    {
        BuffData buffToUse = new()
        {
            buffDuration = 0,
            buffEffectX = 0,
            buffID = ""
        };

        switch (buffType)
        {
            case BuffToTest.Burn:
                buffToUse.buffEffectX = 3;
                buffToUse.buffDuration = 5;
                buffToUse.buffID = "Burn";
                SetupDebuff(buffToUse, null);
                break;
            case BuffToTest.Slow:
                buffToUse.buffEffectX = .25f;
                buffToUse.buffDuration = 8;
                buffToUse.buffID = "Slow";
                SetupDebuff(buffToUse, null);
                break;
            case BuffToTest.Frozen:
                buffToUse.buffEffectX = 0;
                buffToUse.buffDuration = 3;
                buffToUse.buffID = "Frozen";
                SetupDebuff(buffToUse, null);
                break;
            case BuffToTest.Smite:
                buffToUse.buffEffectX = 8;
                buffToUse.buffDuration = 10;
                buffToUse.buffID = "Smite";
                SetupDebuff(buffToUse, null);
                break;
            case BuffToTest.Bleed:
                buffToUse.buffEffectX = 2;
                buffToUse.buffDuration = 8;
                buffToUse.buffID = "Bleed";
                SetupDebuff(buffToUse, null);
                break;
            case BuffToTest.Thorns:
                buffToUse.buffEffectX = 3;
                buffToUse.buffDuration = 10;
                buffToUse.buffID = "Thorns";
                SetupDebuff(buffToUse, null);
                break;
            case BuffToTest.Stagger:
                buffToUse.buffEffectX = 25;
                buffToUse.buffDuration = 2;
                buffToUse.buffID = "Stagger";
                SetupDebuff(buffToUse, null);
                break;
            case BuffToTest.Galvanized:
                buffToUse.buffEffectX = 20;
                buffToUse.buffDuration = 0;
                buffToUse.buffID = "Galvanized";
                SetupDebuff(buffToUse, null);
                break;
            case BuffToTest.Transmutation:
                buffToUse.buffEffectX = 1;
                buffToUse.buffDuration = 30;
                buffToUse.buffID = "Transmutation";
                SetupDebuff(buffToUse, null);
                break;
        }
    }
}
