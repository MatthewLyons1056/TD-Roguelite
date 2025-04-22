using MoreMountains.Feedbacks;
using System.Collections;
using System.Collections.Generic;
using System.Xml.Serialization;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Encounter_MapProgression : MonoBehaviour
{

    public EncounterType loadingEncounterType;
    public List<Vector2Int> viableNeighborPositions = new List<Vector2Int>(); //list of viable neighbor positions that the player can move their token to

    //mmf players for scene transitions
    public MMF_Player gameTransition;

    public bool skipEncounters = false;  //dev bool toggle for auto skippin encounters
    
    public  void MapLoadingSequence()
    {
        if(loadingEncounterType == null)
        {
            Debug.Log("encounterType is null");
        }
        else
        {
            //Debug.Log("encounterType is " +loadingEncounterType);
            ConfirmMap();
        }
    }

    void ConfirmMap() //this script will check the type, an
    {
        if(!skipEncounters)
        {
            switch (loadingEncounterType)
            {
                case EncounterType.Battle:
                    Encounter_Master_Controller.Instance.LoadEncounter(EncounterType.Battle);
                    LoadMap(EncounterType.Battle);
                    break;
                case EncounterType.EliteBattle:
                    Encounter_Master_Controller.Instance.LoadEncounter(EncounterType.EliteBattle);
                    LoadMap(EncounterType.EliteBattle);
                    break;
                case EncounterType.BossBattle:
                    Encounter_Master_Controller.Instance.LoadEncounter(EncounterType.BossBattle);
                    LoadMap(EncounterType.BossBattle);
                    break;
                case EncounterType.Random:

                    Encounter_Master_Controller.Instance.LoadEncounter(EncounterType.Random);
                    LoadMap(EncounterType.Random);
                    break;
                case EncounterType.Shop:
                    Encounter_Master_Controller.Instance.LoadEncounter(EncounterType.Shop);
                    LoadMap(EncounterType.Shop);
                    break;
                case EncounterType.Treasure:
                    Encounter_Master_Controller.Instance.LoadEncounter(EncounterType.Treasure);
                    LoadMap(EncounterType.Treasure);
                    break;
                case EncounterType.Rest:
                    Encounter_Master_Controller.Instance.LoadEncounter(EncounterType.Rest);
                    LoadMap(EncounterType.Rest);
                    break;
                case EncounterType.Armory:
                    Encounter_Master_Controller.Instance.LoadEncounter(EncounterType.Armory);
                    LoadMap(EncounterType.Armory);
                    break;
                default:
                    Encounter_Master_Controller.Instance.LoadEncounter(EncounterType.Battle);
                    LoadMap(EncounterType.Battle);

                    break;
            }
        }
    }
    void LoadMap(EncounterType encounter)
    {

        //this script will change everything depending on the type
        Debug.Log(encounter.ToString());
        //SceneManager.LoadScene("Game_Scene"); //loading scene
        gameTransition.PlayFeedbacks(); // play transition
        OpenWorld_SaveController.instance.LoadOverWorld(false);
    }
}
