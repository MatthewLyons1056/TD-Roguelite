using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using System.Linq;

public class Encounter_Map : MonoBehaviour
{

    //This script contributes to generating the encounters on the map

    //This value is for the live version of the grid, not the backend but instead the controller

    //numbers/base field values
    [FoldoutGroup("Grid Values")] public int width = 1; //width
    [FoldoutGroup("Grid Values")] public int height = 1; //hieght
    [FoldoutGroup("Grid Values")] public float cellSize = 10;
    //grid
    private Encounter_Grid grid; //grid holder
    [FoldoutGroup("Path Values")] public int numberOfPaths = 4; //4 by default
    [FoldoutGroup("Path Values")] public int treasureLane = 6; //lane 6 will have the treasure
    //obj
    [FoldoutGroup("OBJ's")] public GameObject cellObj; //game object to represent cell
    [FoldoutGroup("OBJ's")] public GameObject parentOBJ; //parent OBJ

    [FoldoutGroup("Encounter Types Total")][SerializeField] int battle = 0;
    [FoldoutGroup("Encounter Types Total")][SerializeField] int eliteBattle = 0;
    [FoldoutGroup("Encounter Types Total")][SerializeField] int bossBattle= 0;
    [FoldoutGroup("Encounter Types Total")][SerializeField] int random= 0;
    [FoldoutGroup("Encounter Types Total")][SerializeField] int shop = 0;
    [FoldoutGroup("Encounter Types Total")][SerializeField] int treasure= 0;
    [FoldoutGroup("Encounter Types Total")][SerializeField] int rest= 0;
    [FoldoutGroup("Encounter Types Total")][SerializeField] int totalNodes= 0;


    //spawnWeightsForRandomEncounters

    private Dictionary<EncounterType, float> encounterSpawnWeights = new Dictionary<EncounterType, float>() //setting dictionary
    {
        {EncounterType.Battle, 0.35f },
        {EncounterType.EliteBattle, 0.16f },
        {EncounterType.BossBattle, 0 },
        {EncounterType.Random, 0.22f },
        {EncounterType.Shop, 0.05f },
        {EncounterType.Treasure, 0.0f },
        {EncounterType.Rest, 0.12f },
    };

    private EncounterType ChooseRandomEncounterType(Dictionary<EncounterType, float> encounterSpawnWeights, Encounter_Cell currentCell, Vector2Int cellPos)
    {
        const int maxAttempts = 20; // Prevent an infinite loop.
        for (int attempt = 0; attempt < maxAttempts; attempt++)
        {
            // Perform weighted random selection.
            float totalWeight = 0f;
            foreach (float weight in encounterSpawnWeights.Values)
            {
                totalWeight += weight;
            }
            float randomValue = Random.Range(0f, totalWeight);
            EncounterType chosenType = EncounterType.Battle; // default fallback

            foreach (var pair in encounterSpawnWeights)
            {
                randomValue -= pair.Value;
                if (randomValue <= 0)
                {
                    chosenType = pair.Key;
                    break;
                }
            }

            // Rule for Rests: 
            // 1. Do not allow a Rest if any neighbor is already Rest.
            // 2. Also do not allow a Rest in the first 2 rows (e.g., grid positions where x < 2).
            if (chosenType == EncounterType.Rest)
            {
                bool neighborHasRest = false;
                foreach (Vector2Int neighborPos in currentCell.viableNeighborPositions)
                {
                    if (grid.pathedCells.ContainsKey(neighborPos))
                    {
                        Encounter_Cell neighborCell = grid.pathedCells[neighborPos].GetComponent<Encounter_Cell>();
                        if (neighborCell.encounterType == EncounterType.Rest)
                        {
                            neighborHasRest = true;
                            break;
                        }
                    }
                }
                if (neighborHasRest || cellPos.x < 2)
                {
                    // Rule violation: either a neighbor is Rest or this cell is in the first 2 rows.
                    continue; // re-roll.
                }
            }

            if(chosenType == EncounterType.EliteBattle)
            {
                if(cellPos.x < 3) //elites can only show up as early as floor 4
                {
                    continue;
                }
            }

            // Rule for Shops: Two Shops cannot be adjacent.
            if (chosenType == EncounterType.Shop)
            {
                bool neighborHasShop = false;
                foreach (Vector2Int neighborPos in currentCell.viableNeighborPositions)
                {
                    if (grid.pathedCells.ContainsKey(neighborPos))
                    {
                        Encounter_Cell neighborCell = grid.pathedCells[neighborPos].GetComponent<Encounter_Cell>();
                        if (neighborCell.encounterType == EncounterType.Shop)
                        {
                            neighborHasShop = true;
                            break;
                        }
                    }
                }
                if (neighborHasShop)
                {
                    continue; // re-roll.
                }
            }

            // If no rules are violated, return the chosen type.
            return chosenType;
        }
        // Fallback if no valid type is found after several attempts.
        return EncounterType.Battle;
    }


    //control the logic for pathedCells = cells that exist currently

    void RunEncounterSeries() //test function to host encounters
    {
        //logic for changing encounters from dictionary reference
        //should change update the encounters list of every node

        //FIRST RUN THROUGH IS TO GIVE THE MAP NODES THAT HAVE RULES
        /*
         BASIC RULES
            - THE FIRST ROW IS ALL BATTLES
            - THE 6TH ROW IS ALL TREASURES
            - THE LAST ROW IS ALL REST SITES
         */

        foreach (var kvp in grid.pathedCells) //kvp = key value pair 
        {
            Encounter_Cell currentCell = kvp.Value.GetComponent<Encounter_Cell>();
            //Debug.Log(kvp.Key);
            if (kvp.Key.x == 0) //grabs everything in the first row, converts the encounter to Encounter.Battle
            {
                currentCell.SetEncounterType(EncounterType.Battle);
                
            }
            else if (kvp.Key.x == width - 1)
            {
                currentCell.SetEncounterType(EncounterType.Rest);
            }
            else if(kvp.Key.x == treasureLane)
            {
                currentCell.SetEncounterType(EncounterType.Treasure);
            }
        }
        //THIS IS THE SECOND RUNTHROUGH, ALL REMAINING ENCOUNTERS WILL BE RANDOMIZED BASED ON WEIGHTS AND POSITIONING IN THE GRID
       /* foreach(var kvp in grid.pathedCells.Reverse())
        {
            Encounter_Cell currentCell = kvp.Value.GetComponent<Encounter_Cell>();
            EncounterType chosenType = ChooseRandomEncounterType(encounterSpawnWeights, currentCell);
            currentCell.SetEncounterType(chosenType); //should randomize type
        }*/

        for(int x = width - 1; x >= 0; x--)
        {
            for(int z = height - 1; z >= 0; z--)
            {
                Vector2Int pos = new Vector2Int(x, z);

                if (grid.pathedCells.ContainsKey(pos))
                {
                    //Debug.Log(pos);
                    Encounter_Cell currentCell = grid.pathedCells[pos].GetComponent<Encounter_Cell>();
                    currentCell.currentPos = pos;
                    EncounterType chosenType = ChooseRandomEncounterType(encounterSpawnWeights, currentCell, pos);
                    currentCell.SetEncounterType(chosenType); //should randomize type
                }
                
            }
        }

        verifyEncounterTypes();
    }

    void verifyEncounterTypes() //used to count all encounter types and display them in tool and will reroll the map if the encounters are not sufficient enough
    {
        battle = 0;
        eliteBattle = 0;
        bossBattle = 0;
        random = 0;
        shop = 0;
        treasure = 0;
        rest = 0;
        totalNodes = 0;

        foreach(var kvp in grid.pathedCells)
        {
            Encounter_Cell currentCell = kvp.Value.GetComponent<Encounter_Cell>();
            totalNodes++;
            switch (currentCell.encounterType)
            {
                case EncounterType.Battle: battle++; 
                    break;
                case EncounterType.EliteBattle: eliteBattle++; 
                    break;
                case EncounterType.BossBattle: bossBattle++; 
                    break;
                case EncounterType.Random: random++; 
                    break;
                case EncounterType.Shop: shop++; 
                    break;
                case EncounterType.Treasure: treasure++; 
                    break;
                case EncounterType.Rest: rest++; 
                    break;
                    default:
                    break;
            }
        }


        ///CONFIRM RULES AFTER MAP IS GENERATED
        ///

        

        if(battle  >= 16)
        {
            RerollEncounter(); //this is a terrible way to reroll, instead just reroll the encounter types, not the whole map XD
        }
        if(eliteBattle >= 6)
        {
            RerollEncounter();
        }
        if(random >= 10)
        {
            RerollEncounter();
        }
        if(shop >= 5) 
        {
            RerollEncounter();
        }

        if(rest >= 12)
        {
            RerollEncounter();
        }
    }

    void RerollEncounter()
    {
        foreach (var kvp in grid.pathedCells) //kvp = key value pair 
        {
            Encounter_Cell currentCell = kvp.Value.GetComponent<Encounter_Cell>();

            currentCell.hasEncounterBeenSet = false; //this will help reroll
        }
        RunEncounterSeries();
    }


    

    //intializing map logic

    [Button]
    void InitializeMap()
    {
        ClearGrid();
        grid = new Encounter_Grid(width, height, cellSize, cellObj, parentOBJ);
        grid.GeneratePath(numberOfPaths);
        RunEncounterSeries();
    }
    [Button]
    void ClearGrid() //function for clearing grid
    {

        //clearing grid
        List<Transform> list = new List<Transform>();
        foreach (Transform child in gameObject.transform)
        {
            list.Add(child);
        }
        foreach (Transform child in list)
        {
            DestroyImmediate(child.gameObject);
        }

        //lineParentFind
        GameObject lineParent = GameObject.Find("PathLines");
        if (lineParent == null)
        {
            lineParent = new GameObject("PathLines");
        }
        // Clear existing lines.
        for (int i = lineParent.transform.childCount - 1; i >= 0; i--)
        {
            if (Application.isPlaying) //adding application.IsPlaying so i can use my tools outside of playmode XD
            {
                Destroy(lineParent.transform.GetChild(i).gameObject);

            }
            else
            {
                DestroyImmediate(lineParent.transform.GetChild(i).gameObject);

            }

        }
    }

    [Button]
    void ToggleCellText()
    {
        grid.ToggleText();
    }
}