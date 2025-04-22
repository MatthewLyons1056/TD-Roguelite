using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyPoolingManager : MonoBehaviour
{
    public static EnemyPoolingManager Instance { get; private set; }


    private List<GameObject> enemiesPooled;
    [TabGroup("Pooling", "Enemy", false, 2)] public GameObject enemyOBJ;
    [TabGroup("Pooling", "Enemy", false, 2)] public int enemyAmount;
    [TabGroup("Pooling", "Enemy", false, 2)] public Transform enemyHolder;

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

    void Start()
    {
        GameObject OBJ;

        enemiesPooled = new List<GameObject>();
        for (int i = 0; i < enemyAmount; i++)
        {
            OBJ = Instantiate(enemyOBJ, enemyHolder);
            OBJ.SetActive(false);
            enemiesPooled.Add(OBJ);
        }
    }

    public GameObject GetPooledObject(string OBJ_Type = "Enemy")//find an available pooled object based on object type
    {
        switch (OBJ_Type)
        {
            case "Enemy":
                for (int i = 0; i < enemyAmount; i++)
                {
                    if (!enemiesPooled[i].activeInHierarchy)
                    {
                        return enemiesPooled[i];
                    }
                }
                break;
        }


        return null;
    }

    public void AddObjectToPool(string OBJ_Type, GameObject objectToAdd)//when an object is not available in the pool then add a new one 
    {
        switch (OBJ_Type)
        {
            case "Enemy":
                enemyAmount++;
                enemiesPooled.Add(objectToAdd);
                break;
        }
    }
}
