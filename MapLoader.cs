using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
using Pathfinding;
using DG.Tweening;

public class MapLoader : MonoBehaviour
{
    //Location: Game_Scene
    //What does this do? Loads the map and map specific logic depending on the encounter type

    public static MapLoader instance { get; private set; }

    public Dialogue_System d_system;
    public EncounterType encounterToLoad;


    //grab encounter sOBJ
    public Encounter encounterSO;

    private void Start()
    {
        d_system = FindObjectOfType<Dialogue_System>();  //finds the Dialogue system
    }

    private void Awake()
    {
        if(instance != null && instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            instance = this;
        }
    }



    public void LoadMap(EncounterType encounter)
    {
        StartCoroutine(LoadMapCoroutine(encounter));
        
    }
    private IEnumerator LoadMapCoroutine(EncounterType encounter) //this function loads the map
    {
        /*if (d_system != null)
        {
            d_system.ToggleDialogue(false);
        }*/

        //clear map if map already exists

        foreach (Transform child in transform)
        {
            if (Application.isPlaying)
            {
                Debug.Log("Destroying " + child.name);
                Destroy(child.gameObject);
            }
            else
            {
                DestroyImmediate(child.gameObject);
                Debug.Log("Destroying " + child.name);
            }
        }
        yield return new WaitForEndOfFrame(); //wait a frame to help look for encounter type when moving from scenes
        GameObject mapToLoad = null;
        //Instantiate(LibraryLink.Instance.dataLibrary.levelMapsSO.FindMap(), transform);
        if (Application.isPlaying)
        {
            //find encounter based no random encounter
            encounterSO = Map_Containers.Instance.GetEncounter(encounter);
            mapToLoad = encounterSO.map; //chooses map to load
        }
        else
        {
            Debug.LogError("Game needs to be running to generate random map");
        }
        Debug.Log("Loading " + encounter + " map: " + mapToLoad);

        //load the map + map specifics

        //StartCoroutine(HandleMapLoadSpecific(encounter));
        Instantiate(mapToLoad, transform); //LOAD MAP
    }

    //here is the main map load function --> currently calling this in LevelIntroSequence script

    public IEnumerator HandleMapLoadSpecific(EncounterType encounter) //this function loads the map specifics
    {

        yield return new WaitForSeconds(.5f);

        switch (encounter)
        {
            case EncounterType.Battle:
                LoadBattleLogic();
                break;
            case EncounterType.EliteBattle:
                LoadBattleLogic();
                break;
            case EncounterType.BossBattle:
                break;
            case EncounterType.Random:
                LoadRandomLogic();
                break;
            case EncounterType.Shop:
                IncludeDialogue();
                break;
            case EncounterType.Treasure:
                IncludeDialogue();
                break;
            case EncounterType.Rest:
                IncludeDialogue();
                break;
            case EncounterType.Armory:
                LoadArmoryLogic();
                break;
            default:
                break;
        }
    }

    #region REGION TO CONTAIN OPTIONAL DEPENDANCIES DEPENDING ON THE ENCOUNTER
    void LoadArmoryLogic() //load specific armory logic
    {
        Debug.Log("loading armory specific logic");
        //find towerInventory
        //DOVirtual.DelayedCall(.8f, () => TowerInventory.Instance.CreateLoadoutOptions()); --> use this once inventory is global instance
    }
    void LoadBattleLogic()
    {
        //update grid after map has been created
        AstarPath.active.Scan();
        //after map has been loaded, find the correct grid
        FindMousePosition.Instance.InitializeMousePOS();

        LoadBattleUI();

        TowerInventory.Instance.DrawCards(true); //draws cards --> putting this here because of the delay
    }

    void LoadBattleUI() //UI logic thats supports battle UI
    {
        ResourcesUI.Instance.introFeedback.PlayFeedbacks();
        Timer.Instance.introFeedback.PlayFeedbacks();
        HPBarLogic.Instance.introFeedback.PlayFeedbacks();
        HPBarLogic.Instance.controlsIntroFeedback.PlayFeedbacks();
        DPS_ChartUI.Instance.introFeedback.PlayFeedbacks();
        ActionBarController.Instance.introFeedback.PlayFeedbacks();
        TowerInventory.Instance.Initalize();
    }

    void LoadRandomLogic()
    {
        //choose random encounter stuff
        IncludeDialogue();
    }


    void IncludeDialogue()
    {
        d_system.ToggleDialogue(true);
    }
    #endregion
}
