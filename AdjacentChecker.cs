using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MoreMountains;
using MoreMountains.Feedbacks;

public class AdjacentChecker : MonoBehaviour
{
    [FoldoutGroup("Adjacent"), SerializeField] internal List<TowerDataOBJ> towersInRange = new();
    [FoldoutGroup("Adjacent"), SerializeField] internal TowerDataOBJ selfTowerOBJ;

    public TowerOutlineController towerOutlineRef; // outline reference
    public TowerIconController towerIconRef;

    // Dictionary to track how many colliders are in the trigger for each tower
    private Dictionary<TowerDataOBJ, int> towerColliderCount = new();

    private void Start()
    {
        if (GetComponentInParent<TowerDataOBJ>() != null)
        {
            selfTowerOBJ = GetComponentInParent<TowerDataOBJ>();
        }
    }

    // Trigger events
    private void OnTriggerEnter(Collider other) // add a target to the list when in range
    {
        if (other.CompareTag("AdjacentCheck"))
        {
            if (other.GetComponentInParent<TowerDataOBJ>())
            {
                TowerDataOBJ AIOBJ = other.GetComponentInParent<TowerDataOBJ>();
                TowerOutlineController towerOutlineRef = other.GetComponentInParent<TowerOutlineController>();
                TowerIconController towerIconRef = AIOBJ.GetComponentInChildren<TowerIconController>();
                MMF_Player scaleTower = AIOBJ.transform.Find("Feedbacks/AdjacencyFeedbackStart").GetComponent<MMF_Player>();
                if (!towersInRange.Contains(AIOBJ))
                {
                    towersInRange.Add(AIOBJ);
                    towerColliderCount[AIOBJ] = 0; // Initialize entry counter

                    if (selfTowerOBJ != null)
                    {
                        selfTowerOBJ.buffData.adjacentCounter.Value += AIOBJ.towerBlueprint.FrameOBJ.AdjacencyTier;
                        if(selfTowerOBJ.supportAI != null)
                        {
                            selfTowerOBJ.supportAI.AddCombatTower(AIOBJ);
                        }
                    }
                }

                // Increment the counter for this tower
                towerColliderCount[AIOBJ]++;

                // If the tower is in range, trigger adjacency
                if (selfTowerOBJ == null)
                {
                    towerOutlineRef.RenderOutline("test");
                    towerIconRef.toggle = true;
                    scaleTower.PlayFeedbacks(); // Play scale
                }
            }
        }
    }

    private void OnTriggerExit(Collider other) // remove a target from the list when out of range
    {
        if (other.CompareTag("AdjacentCheck"))
        {
            if (other.GetComponentInParent<TowerDataOBJ>())
            {
                TowerDataOBJ AIOBJ = other.GetComponentInParent<TowerDataOBJ>();
                TowerOutlineController towerOutlineRef = other.GetComponentInParent<TowerOutlineController>();

                TowerIconController towerIconRef = AIOBJ.GetComponentInChildren<TowerIconController>();
                MMF_Player scaleTower = AIOBJ.transform.Find("Feedbacks/AdjacencyFeedbackEnd").GetComponent<MMF_Player>();
                // Decrement the counter for this tower
                if (towerColliderCount.ContainsKey(AIOBJ))
                {
                    towerColliderCount[AIOBJ]--;

                    // Only remove adjacency and outline if the counter reaches zero
                    if (towerColliderCount[AIOBJ] == 0)
                    {
                        towersInRange.Remove(AIOBJ);
                        towerColliderCount.Remove(AIOBJ); // Cleanup

                        if (selfTowerOBJ != null)
                        {
                            selfTowerOBJ.buffData.adjacentCounter.Value -= AIOBJ.towerBlueprint.FrameOBJ.AdjacencyTier;
                            if (selfTowerOBJ.supportAI != null)
                            {
                                selfTowerOBJ.supportAI.RemoveCombatTower(AIOBJ);
                            }
                        }
                        else
                        {
                            towerOutlineRef.RemoveOutline();
                            towerIconRef.toggle = false;
                            scaleTower.PlayFeedbacks();
                        }
                    }
                }
            }
        }
    }

    private void OnDestroy()//
    {
        RemoveTowerOutlineList();
        
    }

    // Function to render outlines for all towers in range
    void RenderTowerOutlineList()
    {
        foreach (TowerDataOBJ towerOutlineController in towersInRange)
        {
            TowerOutlineController towerIconCon = towerOutlineController.GetComponentInChildren<TowerOutlineController>();
            towerIconCon.RenderOutline("test");
            
        }
    }

    // Function to remove outlines for all towers in range
    void RemoveTowerOutlineList() //Adding bonus functionality for removing all toggles for up arrow, consider renaming function if adding more functionality
    {
        foreach (TowerDataOBJ towerOutlineController in towersInRange)
        {
            TowerOutlineController towerOutlineRef = towerOutlineController.GetComponentInChildren<TowerOutlineController>();
            towerOutlineRef.RemoveOutline();
            TowerIconController towerIconRef = towerOutlineController.GetComponentInChildren<TowerIconController>();
            towerIconRef.toggle = false;
            MMF_Player resetTowerScale = towerIconRef.transform.parent.Find("Feedbacks/AdjacencyFeedbackEnd").GetComponent<MMF_Player>();
            resetTowerScale.PlayFeedbacks();
        }
    }

    
}
