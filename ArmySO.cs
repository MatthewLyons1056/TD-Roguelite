using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class EnemyHealthPair
{
    [HorizontalGroup("Settings"), LabelWidth(25), LabelText("Type: ")]
    public HealthType healthType;
    [HorizontalGroup("Settings"), LabelWidth(25), LabelText("Val: ")]
    public float healthVal;
    internal float maxHealth;
}

[Serializable]
public class Unit
{
    [PreviewField(100, ObjectFieldAlignment.Left), VerticalGroup("OBJ"), LabelWidth(75), LabelText("Enemy OBJ: ")]
    public UnityEngine.Object enemyOBJ;

    [HideInInspector]
    public Enemy Enemy;
    [HideInInspector] 
    public string enemyID;

    [VerticalGroup("Settings"), LabelWidth(85), LabelText("Enemy Count: ")]
    public int enemyCount;
    [VerticalGroup("Settings"), LabelWidth(85), LabelText("Spawn Delay: ")]
    public float SpawnDelay = 1f;
    [VerticalGroup("Settings"), LabelWidth(85), LabelText("Unit Health: ")]
    public List<EnemyHealthPair> unitHealthList = new();

    [VerticalGroup("Stats"), LabelWidth(85), DisplayAsString, LabelText("Enemy Name: ")]
    public string enemyName;
    [VerticalGroup("Stats"), LabelWidth(85), DisplayAsString, LabelText("Unit Health: ")]
    public float unitHealth;
    [VerticalGroup("Stats"), LabelWidth(85), DisplayAsString, LabelText("Group Health: ")]
    public float groupHealth;
    [VerticalGroup("Stats"), LabelWidth(85), DisplayAsString, LabelText("Enemy Class: ")]
    public string enemyClass;
}

[Serializable]
public class UnitWave
{
    [TableList(ShowIndexLabels = true, AlwaysExpanded = true)]
    public List<Unit> Units = new();

    [VerticalGroup("Wave Data") ,BoxGroup("Wave Data/Stats")] public float enemyHealthModifier = 1;
    [VerticalGroup("Wave Data"), BoxGroup("Wave Data/Stats"), ReadOnly] public int totalEnemyCount;
    [VerticalGroup("Wave Data"), BoxGroup("Wave Data/Stats"), ReadOnly] public float totalHealthValue;
    [VerticalGroup("Wave Data"), BoxGroup("Wave Data/Stats"), HideInInspector] public float totalXPGain;
    [VerticalGroup("Wave Data"), BoxGroup("Wave Data/Stats"), HideInInspector] public float cumulativeXPGain;
    [VerticalGroup("Wave Data"), BoxGroup("Wave Data/Stats"), ReadOnly] public int fodder_Enemies, standard_Enemies, large_Enemies, collosal_Enemies;

    [VerticalGroup("Wave Data"), BoxGroup("Wave Data/Timer"), LabelWidth(85), LabelText("Wave Delay: ")]
    public float nextWaveDelay = 2f;
    [VerticalGroup("Wave Data"), BoxGroup("Wave Data/Timer"), LabelWidth(85), LabelText("Cluster Delay: ")]
    public float unitClusterDelay = 1f;
    
    [VerticalGroup("Wave Data"), BoxGroup("Wave Data/Gold")] public float totalGoldToDrop = 75;
    [VerticalGroup("Wave Data"), BoxGroup("Wave Data/Gold"), ReadOnly] public float waveGold;
    [VerticalGroup("Wave Data"), BoxGroup("Wave Data/Gold"), ReadOnly] public float fodder_Gold, standard_Gold, large_Gold, collosal_Gold;
}


[HideMonoScript]
[CreateAssetMenu(fileName = "NewArmy", menuName = "ScriptableObjects/Army", order = 0)]
public class ArmySO : ScriptableObject
{
    [TableList(ShowIndexLabels = true, ShowPaging = true, NumberOfItemsPerPage = 5)]
    public List<UnitWave> Waves = new();

    [PropertySpace(100)]
    [ReadOnly] public int waveCount;
    [PropertySpace(10)]
    public float enemyHealthModifier = 1;
    public int researchLevel = 1;
    [ReadOnly] public int totalEnemyCount;
    [ReadOnly] public float totalHealthValue;
    [ReadOnly] public float totalGoldGain;
    [HideInInspector] public float totalXPGain;

    [PropertySpace(50)]
    public DataLibrary dataLibrary;
    [Button]
    public void GenerateWaveValues()
    {
        int count = 0;
        foreach (UnitWave wave in Waves)
        {
            wave.totalEnemyCount = 0;
            wave.totalHealthValue = 0;
            wave.totalXPGain = 0;
            wave.cumulativeXPGain = 0;
            wave.waveGold = wave.totalGoldToDrop * .2f;
            wave.fodder_Enemies = 0;
            wave.standard_Enemies = 0;
            wave.large_Enemies = 0;
            wave.collosal_Enemies = 0;

            float remainingGold = wave.totalGoldToDrop * .8f;

            foreach (Unit unit in wave.Units)
            {
                wave.totalHealthValue += unit.groupHealth;
                unit.enemyID = unit.enemyOBJ.name;
                foreach (Enemy enemy in dataLibrary.enemies)
                {
                    if(enemy.ID == unit.enemyID)
                    {
                        unit.Enemy = enemy;
                        unit.enemyName = enemy.Name;
                        unit.enemyClass = enemy.EnemyClass;
                        wave.totalXPGain += enemy.XP * unit.enemyCount;
                        wave.cumulativeXPGain = wave.totalXPGain;
                        switch(enemy.EnemyClass)
                        {
                            case "Fodder":
                                wave.fodder_Enemies += unit.enemyCount;
                                break;
                            case "Standard":
                                wave.standard_Enemies += unit.enemyCount;
                                break;
                            case "Large":
                                wave.large_Enemies += unit.enemyCount;
                                break;
                            case "Collosal":
                                wave.collosal_Enemies += unit.enemyCount;
                                break;
                        }
                        if (count != 0)
                        {
                            wave.cumulativeXPGain = wave.totalXPGain + Waves[count - 1].cumulativeXPGain;
                        }
                    }
                }
                wave.totalEnemyCount += unit.enemyCount;
            }
            count++;

            if(wave.collosal_Enemies > 0)
            {
                wave.collosal_Gold = SetGoldPerEnemy(remainingGold, "Collosal") / wave.collosal_Enemies;
                remainingGold -= wave.collosal_Gold * wave.collosal_Enemies;
            }
            if (wave.large_Enemies > 0)
            {
                wave.large_Gold = SetGoldPerEnemy(remainingGold, "Large") / wave.large_Enemies;
                remainingGold -= wave.large_Gold * wave.large_Enemies;
            }
            if (wave.standard_Enemies > 0)
            {
                wave.standard_Gold = SetGoldPerEnemy(remainingGold, "Standard") / wave.standard_Enemies;
                remainingGold -= wave.standard_Gold * wave.standard_Enemies;
            }
            if (wave.fodder_Enemies > 0)
            {
                wave.fodder_Gold = SetGoldPerEnemy(remainingGold, "Fodder") / wave.fodder_Enemies;
            }
        }
    }

    [Button]
    public void GenerateArmyValues()
    {
        waveCount = Waves.Count;
        totalEnemyCount = 0;
        totalHealthValue = 0;
        totalGoldGain = 0;
        totalXPGain = 0;

        foreach (UnitWave wave in Waves)
        {
            totalEnemyCount += wave.totalEnemyCount;
            totalHealthValue += wave.totalHealthValue;
            totalGoldGain += wave.totalGoldToDrop;
            totalXPGain += wave.totalXPGain;
        }
    }
    public float SetGoldPerEnemy(float amountToGive, string enemyClass)
    {
        switch (enemyClass)
        {
            case "Fodder"://takes whats left--6% total

                return amountToGive;
            case "Standard"://takes 75%--18% total

                return amountToGive * .75f;
            case "Large"://takes 50%--24% total

                return amountToGive * .5f;
            case "Collosal"://takes 40%--32% total
                
                return amountToGive * .4f;
        }

        return 1f;
    }


    [Button]
    public void CreateArmy()//auto generate a army based on the research value
    {
        int waveCount = researchLevel / 6;
        if(waveCount > 10)
        {
            waveCount = 10;
        }

        enemyHealthModifier = 1;

        Waves.Clear();

        for (int i = 0; i != waveCount; i++)
        {
            UnitWave wave = new();
            wave = FillWave(wave, i + 1);

            Waves.Add(wave);
        }

        GenerateWaveValues();
        GenerateArmyValues();
    }
    private UnitWave FillWave(UnitWave unitWave, int waveNum)//fill in the data of a single wave
    {
        int spawnCost = Mathf.RoundToInt(15 * Mathf.Log(waveNum) + researchLevel);

        unitWave.enemyHealthModifier = enemyHealthModifier;
        unitWave.nextWaveDelay = 1.5f;
        unitWave.unitClusterDelay = UnityEngine.Random.Range(.5f, 1.1f);
        unitWave.totalGoldToDrop = researchLevel * waveNum + 50;

        while(spawnCost > 0)
        {
            Enemy newEnemy = dataLibrary.GetRandomEnemy("test", researchLevel);

            List<EnemyHealthPair> unitHealthList = new();
            EnemyHealthPair healthPair = new();

            if(newEnemy.Grey > 0)
            {
                healthPair = new()
                {
                    healthVal = newEnemy.BaseHealth * newEnemy.Grey * enemyHealthModifier,
                    healthType = HealthType.Speed
                };
                unitHealthList.Add(healthPair);
            }
            if (newEnemy.Green > 0)
            {
                healthPair = new()
                {
                    healthVal = newEnemy.BaseHealth * newEnemy.Green * enemyHealthModifier,
                    healthType = HealthType.Regenerator
                };
                unitHealthList.Add(healthPair);
            }
            if (newEnemy.Yellow > 0)
            {
                healthPair = new()
                {
                    healthVal = newEnemy.BaseHealth * newEnemy.Yellow * enemyHealthModifier,
                    healthType = HealthType.Armor
                };
                unitHealthList.Add(healthPair);
            }
            if (newEnemy.Blue > 0)
            {
                healthPair = new()
                {
                    healthVal = newEnemy.BaseHealth * newEnemy.Blue * enemyHealthModifier,
                    healthType = HealthType.MagicShield
                };
                unitHealthList.Add(healthPair);
            }
            if (newEnemy.Red > 0)
            {
                healthPair = new()
                {
                    healthVal = newEnemy.BaseHealth * newEnemy.Red * enemyHealthModifier,
                    healthType = HealthType.Health
                };
                unitHealthList.Add(healthPair);
            }

            int spawnAmount = UnityEngine.Random.Range(1, newEnemy.SpawnCount);
            if(spawnAmount > waveCount)
            {
                spawnAmount = UnityEngine.Random.Range(waveCount - 1, waveCount + 2);
            }

            Unit unit = new()
            {
                enemyOBJ = newEnemy.enemyOBJ,
                enemyCount = spawnAmount,
                SpawnDelay = UnityEngine.Random.Range(.2f, .6f),
                unitHealthList = unitHealthList,
                unitHealth = newEnemy.BaseHealth * enemyHealthModifier
            };
            unit.groupHealth = unit.unitHealth * unit.enemyCount;

            unitWave.Units.Add(unit);
            spawnCost -= newEnemy.SpawnCost * spawnAmount;
        }


        return unitWave;
    }
}
