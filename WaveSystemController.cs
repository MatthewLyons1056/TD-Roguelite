using QFSW.QC;
using Sirenix.OdinInspector;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class EnemySpawnCost
{
    public string ID;
    public Enemy enemyData;
    public int introduceAtXWave;
    public int removeAtXWave;
}

[Serializable]
public class SpawnableEnemies
{
    public string ID;
    public Enemy enemyData;
    public int weightMin, weightMax;
}

[Serializable]
public class SpawnCluster
{
    public int clusterSpawnAmount = 6;
    public float spawnClusterTimer = .5f;
    public int spawnClusterCostLimit = 12;
}

public class WaveSystemController : MonoBehaviour
{
    public static WaveSystemController instance;

    [TabGroup("Waves", "Armies", false, 0), SerializeField] private List<ArmySO> armies = new();
    [TabGroup("Waves", "Armies", false, 0), SerializeField] private bool useArmy;

    [FoldoutGroup("Refrences", 25)] public DataLibrary dataLibrary;

    [TabGroup("Waves", "Scaling", false, 0)] public int spawnAllowanceMax;
    [TabGroup("Waves", "Scaling", false, 0)] public int spawnAllowanceGainPerWave;
    [TabGroup("Waves", "Scaling", false, 0)] public float spawnAllowancePercentPerWave;
    [DisplayAsString, SerializeField] private int currentSpawnAllowance;
    [TabGroup("Waves", "Scaling", false, 0)] public int currentWave;
    [TabGroup("Waves", "Scaling", false, 0)] public float currentHealthModifier = 1;
    [TabGroup("Waves", "Scaling", false, 0)] public float modifierIncreasePerWave = .1f;
    [TabGroup("Waves", "Scaling", false, 0)] public SpawnCluster baseSpawnCluster;
    [TabGroup("Waves", "Scaling", false, 0)] public SpawnCluster spawnClusterScaler;
    [TabGroup("Waves", "Scaling", false, 0)] public int boostClusterScalerAtXWave;

    [TabGroup("Waves", "Timers", false, 1), SerializeField] private float spawnDelay, waveDelay;
    [DisplayAsString] public int activeEnemyCount;
    public List<UnitHealth> activeEnemies;
    private bool isRunningCluster;

    [TabGroup("Spawns", "Enemy spawn table", false, 1)] public List<EnemySpawnCost> enemySpawns = new();
    [TabGroup("Spawns", "Spawnable Enemies", false, 2)] public List<SpawnableEnemies> spawnableEnemies = new();
    [TabGroup("Spawns", "Spawnable Enemies", false, 2), SerializeField] private int maxSpawnRoll;
    [TabGroup("Spawns", "Spawn points", false, 4)] public List<Transform> spawnPoints = new();
    private bool loadingSpawns;

    public List<Skill> waveSkills = new();

    private void Awake()
    {
        if (instance != null)
        {
            Debug.Log("more than one wave controller in scene");
            return;
        }

        instance = this;

        waveSkills = SkillManager.Instance.GetSkills(SkillManager.SkillClusterType.Wave);
    }
    private void Start()
    {
        maxSpawnRoll = 0;
        UpdateEnemyLists();
    }
    public void TriggerMatchStart()
    {
        LibraryLink.Instance.dataLibrary.SetupArmy();

        StartCoroutine(ArmySpawner());
    }

    public IEnumerator ArmySpawner()
    {
        GameObject[] spawns = GameObject.FindGameObjectsWithTag("EnemySpawn");
        foreach (GameObject respawn in spawns)
        {
            spawnPoints.Add(respawn.transform);
        }

        armies.Add(LibraryLink.Instance.dataLibrary.GetArmy(LevelDataHolder.Instance.armyToLoad));

        float goldDropAmount = 0;

        yield return new WaitForSeconds(waveDelay);

        for (int i = 0; i != armies.Count; i++)//spawn an army
        {
            for (int x = 0; x != armies[i].Waves.Count; x++)//spawn a wave
            {
                currentWave++;

                Timer.Instance.waveText.text = $"{currentWave}";
                StatsController.Instance.ResetWaveDMG();
                TowerInventory.Instance.TriggerWaveStartBuffs();

                foreach (Skill skill in waveSkills)
                {
                    switch (skill.ID)
                    {

                    }
                }

                for (int y = 0; y != armies[i].Waves[x].Units.Count; y++)//spawn a unit cluster
                {
                    currentHealthModifier = armies[i].enemyHealthModifier * armies[i].Waves[x].enemyHealthModifier;
                    switch (armies[i].Waves[x].Units[y].Enemy.EnemyClass)
                    {
                        case "Fodder":
                            goldDropAmount = armies[i].Waves[x].fodder_Gold;
                            break;
                        case "Standard":
                            goldDropAmount = armies[i].Waves[x].standard_Gold;
                            break;
                        case "Large":
                            goldDropAmount = armies[i].Waves[x].large_Gold;
                            break;
                        case "Collosal":
                            goldDropAmount = armies[i].Waves[x].collosal_Gold;
                            break;
                    }

                    for (int z = 0; z != armies[i].Waves[x].Units[y].enemyCount; z++)//spawn the unit
                    {
                        int spawnOBJ = UnityEngine.Random.Range(0, spawnPoints.Count);
                        Transform randomSpawnPoint = spawnPoints[spawnOBJ];

                        if (randomSpawnPoint != null)
                        {
                            Vector3 spawnPos = new(randomSpawnPoint.position.x + UnityEngine.Random.Range(-spawnPoints[spawnOBJ].localScale.x / 2, spawnPoints[spawnOBJ].localScale.x / 2), randomSpawnPoint.position.y, randomSpawnPoint.position.z + UnityEngine.Random.Range(-spawnPoints[spawnOBJ].localScale.z / 2, spawnPoints[spawnOBJ].localScale.z / 2));
                            GameObject enemyOBJ = EnemyPoolingManager.Instance.GetPooledObject();
                            if (enemyOBJ == null)
                            {
                                enemyOBJ = Instantiate(dataLibrary.enemyBaseOBJ, EnemyPoolingManager.Instance.enemyHolder);
                                EnemyPoolingManager.Instance.AddObjectToPool("Enemy", enemyOBJ);
                            }
                            enemyOBJ.SetActive(true);
                            enemyOBJ.transform.position = spawnPos;

                            enemyOBJ.GetComponentInChildren<EnemyDataOBJ>().LoadEnemyData(armies[i].Waves[x].Units[y].Enemy, armies[i].Waves[x].Units[y].unitHealthList, enemyOBJ, goldDropAmount);
                            activeEnemyCount += 1;
                        }
                        yield return new WaitForSeconds(armies[i].Waves[x].Units[y].SpawnDelay);
                    }
                    yield return new WaitForSeconds(armies[i].Waves[x].unitClusterDelay);
                }

                yield return new WaitUntil(() => activeEnemyCount <= 0);
                ResourceController.Instance.UpdateResources(armies[i].Waves[x].waveGold);
                yield return new WaitForSeconds(armies[i].Waves[x].nextWaveDelay);

                if (currentWave > SaveSystemController.Instance.Highest_Wave)
                {
                    SaveSystemController.Instance.Highest_Wave = currentWave;
                    SaveSystemController.Instance.SaveStat("Highest_Wave", currentWave);
                    TrophyManager.Instance.CheckForTrophy(TrophyManager.TrophyType.Wave);
                }
                SaveSystemController.Instance.SaveWaveCycleStats();
            }
        }

        StateMachine.Instance.SwitchState(StateMachine.GameState.Victory);
    }
    public IEnumerator SpawnSelectWave(int waveNum)
    {
        waveNum -= 1;
        armies.Clear();
        armies.Add(LibraryLink.Instance.dataLibrary.GetArmy(LevelDataHolder.Instance.armyToLoad));
        float goldDropAmount = 0;

        if (armies[0].Waves.Count > waveNum)//spawn a wave
        {
            currentWave++;
            Debug.Log($"wave {currentWave}");
            StatsController.Instance.ResetWaveDMG();
            TowerInventory.Instance.TriggerWaveStartBuffs();

            foreach (Skill skill in waveSkills)
            {
                switch (skill.ID)
                {

                }
            }

            for (int y = 0; y != armies[0].Waves[waveNum].Units.Count; y++)//spawn a unit cluster
            {
                Debug.Log($"Spawn units in wave {currentWave}");
                currentHealthModifier = armies[0].enemyHealthModifier * armies[0].Waves[waveNum].enemyHealthModifier;
                switch (armies[0].Waves[waveNum].Units[y].Enemy.EnemyClass)
                {
                    case "Fodder":
                        goldDropAmount = armies[0].Waves[waveNum].fodder_Gold;
                        break;
                    case "Standard":
                        goldDropAmount = armies[0].Waves[waveNum].standard_Gold;
                        break;
                    case "Large":
                        goldDropAmount = armies[0].Waves[waveNum].large_Gold;
                        break;
                    case "Collosal":
                        goldDropAmount = armies[0].Waves[waveNum].collosal_Gold;
                        break;
                }

                for (int z = 0; z != armies[0].Waves[waveNum].Units[y].enemyCount; z++)//spawn the unit
                {
                    int spawnOBJ = UnityEngine.Random.Range(0, spawnPoints.Count);
                    Transform randomSpawnPoint = spawnPoints[spawnOBJ];

                    Debug.Log(randomSpawnPoint);
                    if (randomSpawnPoint != null)
                    {
                        Vector3 spawnPos = new(randomSpawnPoint.position.x + UnityEngine.Random.Range(-spawnPoints[spawnOBJ].localScale.x / 2, spawnPoints[spawnOBJ].localScale.x / 2), randomSpawnPoint.position.y, randomSpawnPoint.position.z + UnityEngine.Random.Range(-spawnPoints[spawnOBJ].localScale.z / 2, spawnPoints[spawnOBJ].localScale.z / 2));
                        GameObject enemyOBJ = EnemyPoolingManager.Instance.GetPooledObject();
                        if (enemyOBJ == null)
                        {
                            enemyOBJ = Instantiate(dataLibrary.enemyBaseOBJ, EnemyPoolingManager.Instance.enemyHolder);
                            EnemyPoolingManager.Instance.AddObjectToPool("Enemy", enemyOBJ);
                        }
                        enemyOBJ.SetActive(true);
                        enemyOBJ.transform.position = spawnPos;

                        enemyOBJ.GetComponentInChildren<EnemyDataOBJ>().LoadEnemyData(armies[0].Waves[waveNum].Units[y].Enemy, armies[0].Waves[waveNum].Units[y].unitHealthList, enemyOBJ, goldDropAmount);
                        activeEnemyCount += 1;
                    }
                    yield return new WaitForSeconds(armies[0].Waves[waveNum].Units[y].SpawnDelay);
                }
                yield return new WaitForSeconds(armies[0].Waves[waveNum].unitClusterDelay);
            }

            yield return new WaitUntil(() => activeEnemyCount <= 0);
            ResourceController.Instance.UpdateResources(armies[0].Waves[waveNum].waveGold);
            yield return new WaitForSeconds(armies[0].Waves[waveNum].nextWaveDelay);

            if (currentWave > SaveSystemController.Instance.Highest_Wave)
            {
                SaveSystemController.Instance.Highest_Wave = currentWave;
                TrophyManager.Instance.CheckForTrophy(TrophyManager.TrophyType.Wave);
            }
            SaveSystemController.Instance.SaveWaveCycleStats();
        }
    }


    public IEnumerator WaveController()//primary controller, this will spawn waves of enemies and cycle the waves while also scaling the enemy strength
    {
        currentSpawnAllowance = spawnAllowanceMax;

        yield return new WaitForSeconds(waveDelay);

        while (true)//player is alive
        {
            currentWave++;
            loadingSpawns = true;
            UpdateEnemyLists();

            yield return new WaitUntil(() => loadingSpawns == false);

            while (currentSpawnAllowance > 0)
            {
                if(UnityEngine.Random.Range(1,11) <= 3 && !isRunningCluster)//30% chance
                {
                    isRunningCluster = true;
                    StartCoroutine(ClusterController());
                    //yield return new WaitUntil(() => isRunningCluster == false);
                    yield return new WaitForSeconds(spawnDelay * 1.5f);
                }
                else
                {
                    RollForEnemySpawn();
                    yield return new WaitForSeconds(spawnDelay);
                }
            }
            yield return new WaitUntil(() => activeEnemyCount == 0);

            yield return new WaitForSeconds(waveDelay);

            currentHealthModifier += modifierIncreasePerWave;
            //currentSpawnAllowance = Mathf.RoundToInt((spawnAllowanceMax + spawnAllowanceGainPerWave) * spawnAllowancePercentPerWave);
            float growthFactor = 1 + (spawnAllowancePercentPerWave / 100); //calculates the % increase for spawnAllowancePercentPerWave
            currentSpawnAllowance = Mathf.RoundToInt(spawnAllowanceMax * Mathf.Pow(growthFactor, currentWave));
        }
    }
    private IEnumerator ClusterController()//controls a cluster spawn, this spawn allows multiple enemies to quickly spawn into the field.
    {
        //cluster spawns ignore standard spawn cost limitations, they can spawn until the spawn attempt counter hits max or the cost used hits the cluster spawn limit

        int costUsed = 0, enemiesSpawned = 0;
        while(baseSpawnCluster.spawnClusterCostLimit > costUsed && baseSpawnCluster.clusterSpawnAmount > enemiesSpawned)
        {
            costUsed += RollEnemyInt();
            enemiesSpawned++;
            yield return new WaitForSeconds(baseSpawnCluster.spawnClusterTimer);
        }
        isRunningCluster = false;
    }


    private void RollForEnemySpawn()//roll for an enemy to spawn, use the spawn weights and chances to spawn an enemy. if the roll given is to expensive then try a reroll 1 time
    {
        int spawnRoll = UnityEngine.Random.Range(1, maxSpawnRoll + 1);
        foreach (SpawnableEnemies enemy in spawnableEnemies)
        {
            if (enemy != null && enemy.enemyData != null)
            {
                if (enemy.weightMin <= spawnRoll && enemy.weightMax >= spawnRoll)
                {
                    activeEnemyCount += 1;
                    currentSpawnAllowance -= enemy.enemyData.SpawnCost;
                    if (spawnPoints != null && spawnPoints.Count > 0)
                    {
                        Transform randomSpawnPoint = spawnPoints[UnityEngine.Random.Range(0, spawnPoints.Count)];
                        if (randomSpawnPoint != null)
                        {
                            GameObject enemyOBJ = Instantiate(dataLibrary.enemyBaseOBJ, randomSpawnPoint.position, transform.rotation);
                            if (enemyOBJ != null)
                            {
                                //Instantiate(enemy.enemyData.enemyOBJ, enemyOBJ.transform.position, enemyOBJ.transform.rotation, enemyOBJ.transform).GetComponent<EnemyDataOBJ>().LoadEnemyData(enemy.enemyData, enemyOBJ);
                            }
                        }
                    }
                    break;
                }
            }
        }
    }
    private int RollEnemyInt()//roll for an enemy to spawn, use the spawn weights and chances to spawn an enemy. cannot retry a spawn roll and has a spawn cost return value
    {
        int costValue = 0;
        int spawnRoll = UnityEngine.Random.Range(1, maxSpawnRoll + 1);
        foreach (SpawnableEnemies enemy in spawnableEnemies)
        {
            if (enemy != null && enemy.enemyData != null)
            {
                if (enemy.weightMin <= spawnRoll && enemy.weightMax >= spawnRoll)
                {
                    activeEnemyCount += 1;
                    currentSpawnAllowance -= enemy.enemyData.SpawnCost;
                    costValue = enemy.enemyData.SpawnCost;
                    if (spawnPoints != null && spawnPoints.Count > 0)
                    {
                        Transform randomSpawnPoint = spawnPoints[UnityEngine.Random.Range(0, spawnPoints.Count)];
                        if (randomSpawnPoint != null)
                        {
                            GameObject enemyOBJ = Instantiate(dataLibrary.enemyBaseOBJ, randomSpawnPoint.position, transform.rotation);
                            if (enemyOBJ != null)
                            {
                                //Instantiate(enemy.enemyData.enemyOBJ, enemyOBJ.transform.position, enemyOBJ.transform.rotation, enemyOBJ.transform).GetComponent<EnemyDataOBJ>().LoadEnemyData(enemy.enemyData, enemyOBJ);
                            }
                        }
                    }
                    break;
                }
            }
        }

        return costValue;
    }


    [TabGroup("Spawns", "Enemy spawn table", false, 1), Button("Generate spawn table", ButtonSizes.Medium), PropertyOrder(10), PropertySpace(5)]
    public void GenerateEnemySpawns()//create new enemies and update old ones, this list is used to add enemies to the waves
    {
        bool canAddEnemy;

        foreach (Enemy enemy in dataLibrary.enemies)
        {
            canAddEnemy = true;

            EnemySpawnCost newEnemy = new EnemySpawnCost
            {
                ID = enemy.ID,
                enemyData = enemy,
                introduceAtXWave = 0,
                removeAtXWave = 100
            };

            foreach (EnemySpawnCost oldEnemy in enemySpawns)
            {
                if (oldEnemy != null && oldEnemy.ID == newEnemy.ID)
                {
                    oldEnemy.enemyData = newEnemy.enemyData;
                    canAddEnemy = false;
                    break;
                }
            }

            if (canAddEnemy)
            {
                enemySpawns.Add(newEnemy);
            }
        }

        List<EnemySpawnCost> enemiesToRemove = new();
        bool enemyNotFound = true;
        foreach (EnemySpawnCost enemy in enemySpawns)
        {
            enemyNotFound = true;
            foreach (Enemy newEnemy in dataLibrary.enemies)
            {
                if (newEnemy.ID == enemy.ID)
                {
                    enemyNotFound = false;
                    break;
                }
            }

            if (enemyNotFound)
            {
                enemiesToRemove.Add(enemy);
            }
        }

        foreach (EnemySpawnCost enemyToRemove in enemiesToRemove)
        {
            enemySpawns.Remove(enemyToRemove);
        }
    }
    public void UpdateEnemyLists()//scan the enemy list and add new enemies as spawnable if the wave matches the counter
    {
        List<SpawnableEnemies> enemiesToRemove = new();

        foreach (EnemySpawnCost enemy in enemySpawns)
        {
            if (enemy.removeAtXWave == currentWave)//check for enemies to remove from spawn table
            {
                foreach (SpawnableEnemies spawnEnemy in spawnableEnemies)
                {
                    if (spawnEnemy.ID == enemy.ID)
                    {
                        enemiesToRemove.Add(spawnEnemy);
                        break;
                    }
                }
            }

            if (enemy.introduceAtXWave == currentWave)//check for enemies to add to spawn table
            {
                SpawnableEnemies newEnemy = new()
                {
                    ID = enemy.ID,
                    enemyData = enemy.enemyData,
                };

                spawnableEnemies.Add(newEnemy);
            }
        }

        if(currentWave % boostClusterScalerAtXWave == 0)//check for increase in the cluster spawner
        {
            baseSpawnCluster.spawnClusterCostLimit += spawnClusterScaler.spawnClusterCostLimit;
            baseSpawnCluster.spawnClusterTimer += spawnClusterScaler.spawnClusterTimer;
            baseSpawnCluster.clusterSpawnAmount += spawnClusterScaler.clusterSpawnAmount;
        }

        foreach(SpawnableEnemies removeEnemy in enemiesToRemove)//remove enemies from spawn table
        {
            spawnableEnemies.Remove(removeEnemy);
        }

        GenerateSpawnWeights();
    }
    private void GenerateSpawnWeights()//generate spawn weights of enemies before the next wave begins
    {
        int highestWeight = 0;

        foreach (SpawnableEnemies enemy in spawnableEnemies)
        {
            enemy.weightMin = highestWeight + 1;
            enemy.weightMax = enemy.enemyData.SpawnWeight + highestWeight;
            highestWeight = enemy.weightMax;
        }

        maxSpawnRoll = highestWeight;
        loadingSpawns = false;
    }


    [Command]
    void EnemySpawnDelay(float spawnTimer)
    {
        spawnDelay = spawnTimer;
    }    

    public void SpawnSpecificEnemy(Enemy enemyToSpawn)
    {
        int spawnOBJ = UnityEngine.Random.Range(0, spawnPoints.Count);
        Transform randomSpawnPoint = spawnPoints[spawnOBJ];

        if (randomSpawnPoint != null)
        {
            Vector3 spawnPos = new(randomSpawnPoint.position.x + UnityEngine.Random.Range(-spawnPoints[spawnOBJ].localScale.x / 2, spawnPoints[spawnOBJ].localScale.x / 2), randomSpawnPoint.position.y, randomSpawnPoint.position.z + UnityEngine.Random.Range(-spawnPoints[spawnOBJ].localScale.z / 2, spawnPoints[spawnOBJ].localScale.z / 2));
            GameObject enemyOBJ = EnemyPoolingManager.Instance.GetPooledObject();
            if (enemyOBJ == null)
            {
                enemyOBJ = Instantiate(dataLibrary.enemyBaseOBJ, EnemyPoolingManager.Instance.enemyHolder);
                EnemyPoolingManager.Instance.AddObjectToPool("Enemy", enemyOBJ);
            }
            enemyOBJ.SetActive(true);
            enemyOBJ.transform.position = spawnPos;

            List<EnemyHealthPair> unitHealthList = new();
            EnemyHealthPair enemyHealth = new()
            {
                healthVal = 15
            };
            unitHealthList.Add(enemyHealth);
            enemyOBJ.GetComponentInChildren<EnemyDataOBJ>().LoadEnemyData(enemyToSpawn, unitHealthList, enemyOBJ);
            activeEnemyCount += 1;
        }
    }
}
