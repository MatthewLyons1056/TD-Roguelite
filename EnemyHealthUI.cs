using DG.Tweening;
using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class EnemyHealthUI : MonoBehaviour
{
    [FoldoutGroup("UI")] public GameObject t1_HealthChunk, t2_HealthChunk, t3_HealthChunk, t4_HealthChunk;
    [FoldoutGroup("UI")] public RectTransform UIRectTrans;
    [FoldoutGroup("UI")] public List<Image> healthBars = new();
    [FoldoutGroup("UI")] public CanvasGroup healthCanvas;

    [FoldoutGroup("Health Settings"), SerializeField] private float T1_MaxHealth, T2_MaxHealth, T3_MaxHealth, T4_MaxHealth;
    [FoldoutGroup("Health Settings"), SerializeField] private Color healthColor, armorColor, shieldColor, speedColor, regenColor;

    private new Camera camera;

    private void Start()
    {
        camera = Camera.main;
    }
    
    private void LateUpdate()
    {
        healthCanvas.transform.LookAt(camera.transform, camera.transform.rotation * Vector3.up);
    }

    public void SetupHealthBars(List<EnemyHealthPair> unitHealthList, EnemyClass enemyClass, bool forceClear = false)
    {
        float healthRemaining = 0;
        float healthTier = T1_MaxHealth;

        int healthBarCounter = 0;
        foreach (EnemyHealthPair pair in unitHealthList)
        {
            switch (pair.maxHealth)
            {
                case float n when n <= T1_MaxHealth * 5:
                    healthTier = T1_MaxHealth;
                    break;
                case float n when n <= T2_MaxHealth * 5 && n > T1_MaxHealth * 5:
                    healthTier = T2_MaxHealth;
                    break;
                case float n when n <= T3_MaxHealth * 5 && n > T2_MaxHealth * 5:
                    healthTier = T3_MaxHealth;
                    break;
                case float n when n > T3_MaxHealth * 5:
                    healthTier = T4_MaxHealth;
                    break;
            }

            healthRemaining = pair.maxHealth;
            while (healthRemaining > 0)
            {
                healthBarCounter++;
                healthRemaining -= healthTier;
            }
        }

        if (healthBarCounter == healthBars.Count && !forceClear)
        {
            UpdateHealthBar(unitHealthList, enemyClass);
        }
        else
        {
            float sizeMult = 1;
            foreach (Transform child in UIRectTrans)
            {
                Destroy(child.gameObject);
            }
            healthBars.Clear();

            healthRemaining = 0;
            foreach (EnemyHealthPair pair in unitHealthList)
            {
                GameObject objToCreate = null;
                switch (pair.maxHealth)
                {
                    case float n when n <= T1_MaxHealth * 5:
                        healthTier = T1_MaxHealth;
                        sizeMult = 1;
                        objToCreate = t1_HealthChunk;
                        break;
                    case float n when n <= T2_MaxHealth * 5 && n > T1_MaxHealth * 5:
                        healthTier = T2_MaxHealth;
                        sizeMult = 1.5f;
                        objToCreate = t2_HealthChunk;
                        break;
                    case float n when n <= T3_MaxHealth * 5 && n > T2_MaxHealth * 5:
                        healthTier = T3_MaxHealth;
                        sizeMult = 2;
                        objToCreate = t3_HealthChunk;
                        break;
                    case float n when n > T3_MaxHealth * 5:
                        healthTier = T4_MaxHealth;
                        sizeMult = 2.5f;
                        objToCreate = t4_HealthChunk;
                        break;
                }

                healthRemaining = pair.maxHealth;
                RectTransform UI = null;
                while (healthRemaining > 0)
                {
                    UI = Instantiate(objToCreate, UIRectTrans).GetComponent<RectTransform>();
                    healthBars.Add(UI.GetChild(0).GetComponent<Image>());
                    ApplyHealthColor(pair.healthType, healthBars[^1]);
                    healthRemaining -= healthTier;
                    if (healthRemaining < 0)
                    {
                        UI.sizeDelta = new Vector2((healthTier + healthRemaining) / healthTier * .1f * sizeMult, .1f * sizeMult);
                    }
                }
            }

            UpdateHealthBar(unitHealthList, enemyClass);
        }
    }
    public void UpdateHealthBar(List<EnemyHealthPair> unitHealthList, EnemyClass enemyClass)
    {
        float healthLost = 0;
        int i = 0;
        int barsToCheck = 0;
        float healthTier = T1_MaxHealth;

        foreach (EnemyHealthPair pair in unitHealthList)
        {
            switch (pair.maxHealth)
            {
                case float n when n <= T1_MaxHealth * 5:
                    healthTier = T1_MaxHealth;
                    break;
                case float n when n <= T2_MaxHealth * 5 && n > T1_MaxHealth * 5:
                    healthTier = T2_MaxHealth;
                    break;
                case float n when n <= T3_MaxHealth * 5 && n > T2_MaxHealth * 5:
                    healthTier = T3_MaxHealth;
                    break;
                case float n when n > T3_MaxHealth * 5:
                    healthTier = T4_MaxHealth;
                    break;
            }

            barsToCheck = Mathf.CeilToInt(pair.maxHealth / healthTier);
            healthLost = pair.maxHealth - pair.healthVal;
            while (healthLost > 0)
            {
                if (healthLost > healthTier)
                {
                    if (i > healthBars.Count - 1)
                    {
                        return;
                    }
                    healthBars[i].fillAmount = 0;
                    i++;
                    healthLost -= healthTier;
                    barsToCheck--;
                }
                else
                {
                    if (i > healthBars.Count - 1)
                    {
                        return;
                    }

                    if(pair.maxHealth - pair.healthVal == pair.maxHealth)
                    {
                        healthBars[i].fillAmount = 0;
                        barsToCheck--;
                    }
                    else if (barsToCheck > 1)
                    {
                        healthBars[i].fillAmount = 1 - healthLost / healthTier;
                        barsToCheck--;
                    }
                    else
                    {
                        if(pair.maxHealth % healthTier > 0)
                        {
                            healthBars[i].fillAmount = 1f - healthLost / (pair.maxHealth % healthTier);
                        }
                        else
                        {
                            healthBars[i].fillAmount = 1 - healthLost / healthTier;
                        }
                    }
                    i++;
                    healthLost = 0;
                }
            }

            while (i < pair.maxHealth / healthTier)
            {
                if(i > healthBars.Count - 1)
                {
                    return;
                }
                healthBars[i].fillAmount = 1;
                i++;
            }
        }
    }


    private void ApplyHealthColor(HealthType healthType, Image image)
    {
        switch(healthType)
        {
            case HealthType.Health:
                image.color = healthColor;
                break;
            case HealthType.MagicShield:
                image.color = shieldColor;
                break;
            case HealthType.Armor:
                image.color = armorColor;
                break;
            case HealthType.Speed:
                image.color = speedColor;
                break;
            case HealthType.Regenerator:
                image.color = regenColor;
                break;
        }
    }

    public void HideUI()
    {
        healthCanvas.Hide();
    }
    public void ShowUI()
    {
        healthCanvas.Show();
    }
}
