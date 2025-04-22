using System.Collections;
using UnityEngine;
using Pathfinding;
using Pathfinding.RVO;
using Sirenix.OdinInspector;
using MoreMountains.Feedbacks;
using DG.Tweening;
using System.Collections.Generic;

public class EnemyDataOBJ : MonoBehaviour
{
    public Enemy enemyData;
    public Transform visualTrans;
    private int targetWaypointCluster;
    [FoldoutGroup("Refrences", 25)] public UnitHealth unitHealth;
    [FoldoutGroup("Refrences", 25)] public AIPath aiPath;
    [FoldoutGroup("Refrences", 25)] internal Animator animator;
    [FoldoutGroup("Refrences", 25)] public RVOController rvoController;
    [FoldoutGroup("Refrences", 25)] public AIDestinationSetter destinationSetter;
    [FoldoutGroup("Refrences", 25)] public EnemyVisualData visData;

    [FoldoutGroup("Feedback", 25)] public MMF_Player death_Feedback, spawnFeedback;

    public void LoadEnemyData(Enemy enemy, List<EnemyHealthPair> unitHealthList, GameObject parent, float goldAmount = 5)
    {
        enemyData = enemy;
        visualTrans.localScale = Vector3.zero;
        visData = Instantiate(enemy.enemyOBJ, visualTrans).GetComponent<EnemyVisualData>();
        aiPath.maxSpeed = 0;

        spawnFeedback.PlayFeedbacks();
        DOVirtual.DelayedCall(spawnFeedback.TotalDuration, () => unitHealth.healthUI.ShowUI(), false);

        parent.name = $"Enemy: {enemyData.Name}";

        unitHealth.Initalize(enemyData, unitHealthList, WaveSystemController.instance.currentHealthModifier, goldAmount);

        animator = GetComponentInChildren<Animator>();
        if(animator != null )
        {
            animator.Play("WalkFWD");
        }

        aiPath.enableRotation = true;
        aiPath.canMove = true;
        targetWaypointCluster = LevelOBJData.Instance.GetClusterNum(transform.position);
        destinationSetter.target = LevelOBJData.Instance.GetNextWaypoint(null, targetWaypointCluster);
        DOVirtual.DelayedCall(spawnFeedback.TotalDuration, () => aiPath.maxSpeed = enemyData.Speed, false);
        aiPath.savedMaxSpeed = enemyData.Speed;

        switch (enemyData.EnemyClass)
        {
            case "Fodder":
                rvoController.layer = RVOLayer.Layer2;
                rvoController.collidesWith = RVOLayer.Layer2 | RVOLayer.DefaultObstacle | RVOLayer.DefaultAgent;
                break;
            case "Standard":
                rvoController.layer = RVOLayer.Layer3;
                rvoController.collidesWith = RVOLayer.Layer3 | RVOLayer.DefaultObstacle | RVOLayer.DefaultAgent;
                break;
            case "Large":
                rvoController.layer = RVOLayer.Layer4;
                rvoController.collidesWith = RVOLayer.Layer4 | RVOLayer.DefaultObstacle | RVOLayer.DefaultAgent;
                break;
            case "Collosal":
                rvoController.layer = RVOLayer.Layer5;
                rvoController.collidesWith = RVOLayer.Layer5 | RVOLayer.DefaultObstacle | RVOLayer.DefaultAgent;
                break;
        }

        StartCoroutine(CheckForNewPoint());
    }

    public IEnumerator CheckForNewPoint()
    {
        while(!unitHealth.isDead)
        {
            yield return new WaitForSeconds(1);

            yield return new WaitUntil(() => aiPath.reachedEndOfPath);

            destinationSetter.target = LevelOBJData.Instance.GetNextWaypoint(destinationSetter.target, targetWaypointCluster);
        }
    }
}
