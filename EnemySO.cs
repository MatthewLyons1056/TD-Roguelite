using GoogleSheetsForUnity;
using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[Serializable]
public class Enemy
{
    [VerticalGroup("ID"), LabelWidth(45), DisplayAsString, LabelText("ID: ")]
    public string ID;

    [TableColumnWidth(120, Resizable = false), AssetSelector(Paths = "Assets/_Prefabs/Towers/NewTowers"), PreviewField(100)]
    public GameObject enemyOBJ;

    [VerticalGroup("ID"), LabelWidth(45), DisplayAsString, LabelText("Name: ")]
    public string Name;

    [VerticalGroup("Data"), LabelWidth(45), DisplayAsString, LabelText("Health: ")]
    public int BaseHealth;

    [VerticalGroup("Data"), LabelWidth(45), DisplayAsString, LabelText("Prio: ")]
    public int PriorityLevel;

    [VerticalGroup("Data"), LabelWidth(45), DisplayAsString, LabelText("Class: ")]
    public string EnemyClass;

    [HideInInspector]
    public string MovementType;

    [HideInInspector]
    public float XP;

    [VerticalGroup("Data"), LabelWidth(45), DisplayAsString, LabelText("SPD: ")]
    public float Speed;

    [VerticalGroup("Data"), LabelWidth(45), DisplayAsString, LabelText("DMG: ")]
    public int Damage;

    [HideInInspector]
    public string BuffID;

    [TableColumnWidth(180, Resizable = true)]
    public BuffEffect buffOBJ;

    [VerticalGroup("Spawn Data"), LabelWidth(45), DisplayAsString, LabelText("Facti: ")]
    public int Faction;

    [VerticalGroup("Spawn Data"), LabelWidth(45), DisplayAsString, LabelText("R-Val: ")]
    public int ResearchVal;

    [VerticalGroup("Spawn Data"), LabelWidth(45), DisplayAsString, LabelText("Cost: ")]
    public int SpawnCost;

    [VerticalGroup("Spawn Data"), LabelWidth(45), DisplayAsString, LabelText("Weight: ")]
    public int SpawnWeight;
    [VerticalGroup("Spawn Data"), LabelWidth(45), DisplayAsString, LabelText("Count: ")]
    public int SpawnCount;

    [HideInInspector]
    public float Red;

    [HideInInspector]
    public float Yellow;

    [HideInInspector]
    public float Green;

    [HideInInspector]
    public float Blue;

    [HideInInspector]
    public float Grey;
}

[Serializable]
public class BuffData
{
    public string buffID;
    public float buffDuration;
    public float buffEffectX;
    public DamageInstance damageInstance;
}

[HideMonoScript]
[CreateAssetMenu(fileName = "Enemies", menuName = "ScriptableObjects/Enemies", order = 0)]
public class EnemySO : ScriptableObject
{
    public TowerBuffsSO BuffsSO;

    private const string _itemTableName = "Enemies";
    [HideInInspector] public UnityAction CloudSyncEvent;
    [HideInInspector] public string SyncResults;

    [TableList(AlwaysExpanded = true, ShowPaging = true, DrawScrollView = false), Searchable, PropertySpace(10), PropertyOrder(10), LabelText("Enemies")]
    public List<Enemy> enemies = new List<Enemy>();

    [DisplayAsString, HideLabel, PropertyOrder(1)]
    public string DownloadStatus = "";

    [Button("Download enemy Data", ButtonSizes.Large), PropertyOrder(-1), PropertySpace(5)]
    public void RetrieveCloudData()
    {
        DownloadStatus = "Downloading Enemy Data...";
        // Suscribe for catching cloud responses.
        Drive.responseCallback += HandleDriveResponse;
        // Make the query.
        Drive.GetTable(_itemTableName, false);
    }

    public void RetrieveCloudDataRuntime()
    {
        // Suscribe for catching cloud responses.
        Drive.responseCallback += HandleDriveResponse;
        // Make the query.
        Drive.GetTable(_itemTableName, true);
    }

    // Processes the data received from the cloud.
    private void HandleDriveResponse(Drive.DataContainer dataContainer)
    {
        if (dataContainer.objType != _itemTableName)
            return;

        // First check the type of answer.
        if (dataContainer.QueryType == Drive.QueryType.getTable)
        {
            string rawJSon = dataContainer.payload;

            // Parse from json to the desired object type.
            Enemy[] enemy1 = JsonHelper.ArrayFromJson<Enemy>(rawJSon);
            bool isEnemiesInList = false;
            List<Enemy> enemiesToRemove = new List<Enemy>();

            //cycle through the new towers and compare them to the old towers, if the new tower has diffrent stats then update them
            foreach (Enemy newEnemy in enemy1)
            {
                isEnemiesInList = false;
                foreach (Enemy enemy in enemies)
                {
                    if (newEnemy.ID == enemy.ID)
                    {
                        enemy.Name = newEnemy.Name;
                        enemy.XP = newEnemy.XP;
                        enemy.Speed = newEnemy.Speed;
                        enemy.BuffID = newEnemy.BuffID;
                        enemy.PriorityLevel = newEnemy.PriorityLevel;
                        enemy.EnemyClass = newEnemy.EnemyClass;
                        enemy.MovementType = newEnemy.MovementType;
                        enemy.SpawnCost = newEnemy.SpawnCost;
                        enemy.SpawnWeight = newEnemy.SpawnWeight;
                        enemy.Damage = newEnemy.Damage;
                        enemy.BaseHealth = newEnemy.BaseHealth;
                        enemy.Faction = newEnemy.Faction;
                        enemy.Red = newEnemy.Red;
                        enemy.Green = newEnemy.Green;
                        enemy.Yellow = newEnemy.Yellow;
                        enemy.Blue = newEnemy.Blue;
                        enemy.Grey = newEnemy.Grey;
                        enemy.ResearchVal = newEnemy.ResearchVal;
                        enemy.SpawnCount = newEnemy.SpawnCount;

                        foreach (BuffEffect buff in BuffsSO.buffs)
                        {
                            if (buff.ID == enemy.BuffID)
                            {
                                enemy.buffOBJ = buff;
                                break;
                            }
                        }

                        isEnemiesInList = true;
                        break;
                    }
                }
                if (!isEnemiesInList)
                {
                    enemies.Add(newEnemy);
                }
            }

            //cycle through the old towers and compare them to the new towers
            foreach (Enemy enemy in enemies)
            {
                isEnemiesInList = false;
                foreach (Enemy newLS in enemy1)
                {
                    if (newLS.ID == enemy.ID)
                    {
                        isEnemiesInList = true;
                        break;
                    }
                }
                if (!isEnemiesInList)
                {
                    enemiesToRemove.Add(enemy);
                }
            }

            //remove any tower that was not found between the old/new tower lists
            foreach (Enemy enemy in enemiesToRemove)
            {
                this.enemies.Remove(enemy);
            }

            //clean up the empty tower objects
            for (int i = enemies.Count - 1; i > -1; i--)
            {
                if (string.IsNullOrEmpty(enemies[i].ID)) enemies.Remove(enemies[i]);
            }

            Debug.Log("updated enemy data SO");
        }
        

        if (dataContainer.QueryType != Drive.QueryType.createTable || dataContainer.QueryType != Drive.QueryType.createObjects)
        {
            Debug.Log(dataContainer.msg);
            SyncResults = dataContainer.msg;
            DownloadStatus = $"{dataContainer.msg.Replace(".", "")} : {DateTime.Now.ToLongDateString()} {DateTime.Now.ToLongTimeString()}";
        }

        if (CloudSyncEvent != null)
            CloudSyncEvent.Invoke();

        Drive.responseCallback -= HandleDriveResponse;

    }
}
