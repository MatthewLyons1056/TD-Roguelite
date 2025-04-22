using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
using System.Linq;


public class EncounterMap_Interaction : MonoBehaviour
{
    //This script holds the logic for handling and interacting with the physical map nodes in the scene

    private GameObject lastHitNode = null;

    [FoldoutGroup("Dependancy's")] public Encounter_MapProgression mapProgress;
    [SerializeField] private Encounter_TokenController token;


    private void Awake()
    {
        if(mapProgress == null)
        {
            mapProgress = GameObject.FindAnyObjectByType<Encounter_MapProgression>();
        }
    }

    private void Start()
    {
        StartCoroutine(FindToken());
    }

    void Update()
    {
        HandleHoverRaycast();
        HandleClickRaycast();
    }

    private void HandleHoverRaycast()
    {
        // Create a ray from the camera using the mouse position.
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit))
        {
            if (hit.collider.CompareTag("MapPin"))
            {
                // If we're hovering over a new MapPin.
                if (hit.collider.gameObject != lastHitNode)
                {
                    if (lastHitNode != null)
                    {
                        lastHitNode.GetComponentInChildren<Encounter_Cell_Visual>().HoverExitFeedback();
                    }
                    hit.collider.gameObject.GetComponentInChildren<Encounter_Cell_Visual>().HoverOverFeedback();
                    lastHitNode = hit.collider.gameObject;
                }
            }
            else
            {
                // Not hovering over a MapPin, exit hover state if needed.
                if (lastHitNode != null)
                {
                    lastHitNode.GetComponentInChildren<Encounter_Cell_Visual>().HoverExitFeedback();
                    lastHitNode = null;
                }
            }
        }
        else
        {
            // No collider hit, exit hover state if needed.
            if (lastHitNode != null)
            {
                lastHitNode.GetComponentInChildren<Encounter_Cell_Visual>().HoverExitFeedback();
                lastHitNode = null;
            }
        }
    }
    private void HandleClickRaycast()
    {
        // Check for a left mouse button click.
        if (Input.GetMouseButtonDown(0))
        {
            Ray clickRay = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit clickHit;
            if (Physics.Raycast(clickRay, out clickHit))
            {
                if (clickHit.collider.CompareTag("MapPin"))
                {
                    Encounter_Cell cell = clickHit.collider.GetComponent<Encounter_Cell>(); //get cell
                    EncounterType encounter = cell.encounterType; //get encounter type

                    clickHit.collider.gameObject.GetComponentInChildren<Encounter_Cell_Visual>().OnClickFeedback(); //clicking

                    //check to see if the selection was a viable neighbor
                    if(CheckViableCell(cell))
                    {
                        mapProgress.loadingEncounterType = encounter; //setting encounter type when selecting
                        mapProgress.MapLoadingSequence(); //loading encounter on select
                        mapProgress.viableNeighborPositions = cell.viableNeighborPositions; //set next 
                        token.viableNeighborPositions = cell.viableNeighborPositions;

                        //logic for moving token
                        Vector3 cellPos = cell.transform.position;
                        token.MoveToken(cellPos);

                    }
                    else
                    {
                        Debug.Log("Non viable cell!");
                    }
                    
                }
            }
        }
    }
    public void Hit(RaycastHit hit)
    {
        hit.collider.gameObject.GetComponentInChildren<Encounter_Cell_Visual>().HoverOverFeedback();
    }

    public void Exit(RaycastHit hit)
    {

        Encounter_Cell_Visual cell_visual = hit.collider.gameObject.GetComponentInChildren<Encounter_Cell_Visual>();
        cell_visual.HoverExitFeedback();
    }

    bool CheckViableCell(Encounter_Cell cell) //funciton to check the neighborpositions to see if they are viable //should turn this from void to something else to confirm
    {
        //check to see if it's in the first row for a valid select

        if(cell.currentPos.x == 0 & !Encounter_Master_Controller.Instance.selectedFirstNode)
        {
            //Debug.Log("frist row! " + cell.currentPos);
            Encounter_Master_Controller.Instance.selectedFirstNode = true; //set flag to true
            return true;
        }
        else
        {
            Vector2Int cellPos = cell.currentPos; //gets cell current pos
            List<Vector2Int> mapList = token.viableNeighborPositions; //checks token for viable neighbors
            bool hasCommon = mapList.Any(pos => pos == cellPos);

            if (hasCommon)
            {
                //Debug.Log("Has common neighbor!");
            }
            else
            {
                //Debug.Log("not viable neighbor");

            }
            return hasCommon;
        }

        //else check to see if there are viable neighbors for selection

    }


    IEnumerator FindToken()
    {
        yield return new WaitForSeconds(1);
        token = FindObjectOfType<Encounter_TokenController>();
    }
}
