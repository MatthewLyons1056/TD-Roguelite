using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using DG.Tweening;
using QFSW.QC;
using MoreMountains.Feedbacks;
using CodeMonkey.Utils;

public class BuyTowerSystem : MonoBehaviour
{
    public static BuyTowerSystem Instance {  get; private set; }

    [FoldoutGroup("Refrences")] public MMF_Player mffPlayer_InvalidPlacement_Invalid;
    [FoldoutGroup("Refrences")] public MMF_Player mffPlayer_InvalidPlacement_NoGold;
    private InventoryController inventoryController;
    private TowerBlueprint towerBlueprint;

    public bool canPlace = true, canTriggerButtons = true;

    internal int activeTowerButtonCounter;

    internal int targetTowerInventoryOBJ = -1;

    [FoldoutGroup("Tower info"), SerializeField] private GameObject activeTowerOBJ;
    [FoldoutGroup("Tower info"), SerializeField] private GameObject towerIndicatorOBJ;
    [FoldoutGroup("Tower info"), SerializeField] private RangeIndicatorControler rangeIndicatorController;
    [FoldoutGroup("Tower info")] public int currentTower = 0;

    public TowerButtonData activeLauncher, activeFrame;
    internal bool mouseInBuyTowerArea;
    public List<int> activeButtonNums = new();

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
        inventoryController = InventoryController.instance;
        TogglePlacementBool(true);

        foreach (TowerButtonData button in ActionBarController.Instance.towerButtonScripts)
        {
            button.transform.localScale = Vector3.zero;
            button.gameObject.SetActive(false);
        }

        StartCoroutine(BuildModeChecker());
    }

    void Update()
    {    
        if(!GameMenuLogic.GameIsPaused)
        {
            if (StateMachine.Instance.CurrentState == StateMachine.GameState.BuildMode)
            {
                PlacementLogic(); //Controls placement location during buildmode
            }
        }
    }
    
    public void PlacementLogic() //controls placement location during buildmode
    {
        if(Input.GetKeyDown(KeyCode.Escape)) //if player presses escape, clear tower placement and leave buildmode
        {
            ClearTowerPlacementData();
        }

        if (Input.GetMouseButtonDown(1))//leave buildmode and cancel tower placement
        {
            ClearTowerPlacementData();
        }


        //controls movement and position of towers when moving the mouse
        if (StateMachine.Instance.CurrentState == StateMachine.GameState.BuildMode) //buildmode is active, and it sets the blueprint to the position of the raycast from the mouse
        {
            var timeToReachPos = .2f;

            Vector3 mousePosition = FindMousePosition.Instance.ReturnCellPosition(); //set mouse position to the return of GetSelectedMapPostion
            towerIndicatorOBJ.transform.DOMove(mousePosition, timeToReachPos).SetUpdate(true); //.SetEase(Ease.Linear); //set the blueprint(mouseIndicator) to the return of mouse position

            if (Input.GetKeyDown(KeyCode.R))
            {
                //rotate tower with DoTween
                towerIndicatorOBJ.transform.DORotate(new Vector3(0, towerIndicatorOBJ.transform.eulerAngles.y + 90, 0), 0.1f, RotateMode.FastBeyond360)
                    .SetEase(Ease.OutQuad)
                    .SetUpdate(true); // unscaled time
            }
        }

        if(rangeIndicatorController != null)
        {
            if (canPlace)
            {
                rangeIndicatorController.ChangeDamageIndicator(true);
            }
            else
            {
                rangeIndicatorController.ChangeDamageIndicator(false);
            }
        }
    }
    public void TogglePlacementBool(bool value)
    {
        canPlace = value;
    }
    
    public void CreateTower() //player has the sufficient cost to purchase tower, tower is placed
    {
        if (StateMachine.Instance.CurrentState == StateMachine.GameState.BuildMode) //wont check anything else unless in build mode 
        {
            if (towerBlueprint.goldCost <= ResourceController.Instance.goldAmount && canPlace == true)
            {
                Vector3 mousePosition = FindMousePosition.Instance.ReturnCellPosition();
                GameObject newTower = Instantiate(activeTowerOBJ, mousePosition + new Vector3(0,100, 0), towerIndicatorOBJ.transform.rotation);

                TowerDataOBJ newTowerData = newTower.GetComponent<TowerDataOBJ>();
                newTowerData.LoadTowerData(towerBlueprint, mousePosition);
                if(targetTowerInventoryOBJ > -1 && targetTowerInventoryOBJ != 10)
                {
                    TowerInventory.Instance.DiscardCards(activeButtonNums);
                    //TowerInventory.Instance.RerollInventory();
                }
                ResourceController.Instance.UpdateResources(-towerBlueprint.goldCost, false);
                ClearTowerPlacementData();
            }
            else //do not place
            {
                if(towerBlueprint.goldCost >= ResourceController.Instance.goldAmount)
                {
                    mffPlayer_InvalidPlacement_NoGold.PlayFeedbacks();
                }
                else
                {
                    mffPlayer_InvalidPlacement_Invalid.PlayFeedbacks();
                }

                //play cant place sound
                AudioReferenceManager.Instance.PlaySound("CantPlace");
            }
        }
        
    }

    public void BuyTower(int towerArrayVal, TowerBlueprint blueprint = null) //this is when the player goes to purchase the tower, creates a example tower that can be placed in-game
    {
        if (StateMachine.Instance.CurrentState == StateMachine.GameState.NormalMode && canTriggerButtons || StateMachine.Instance.CurrentState == StateMachine.GameState.BuildMode && canTriggerButtons) //checks to see if it's normal mode build
        {         
            if(targetTowerInventoryOBJ == towerArrayVal)
            {
                ClearTowerPlacementData();
                return;
            }
            StateMachine.Instance.SwitchState(StateMachine.GameState.BuildMode);
            Cursor.visible = false;
            if (StateMachine.Instance.CurrentState == StateMachine.GameState.BuildMode)
            {
                if (towerIndicatorOBJ != null)
                {
                    ClearTowerIndicator();
                }

                targetTowerInventoryOBJ = towerArrayVal;

                towerBlueprint = blueprint;
                Vector3 mousePosition = FindMousePosition.Instance.ReturnCellPosition();
                var towerIndicatorOBJOffset = new Vector3(0, 0, 0);
                towerIndicatorOBJ.transform.position = mousePosition + towerIndicatorOBJOffset;

                BuildTowerVisual(towerBlueprint);
            }           
        }
        AudioReferenceManager.Instance.PlaySound("ButtonPress");
    }
    public void ClearTowerPlacementData(bool partialClear = false)//clean up data after a tower was placed or canceled
    {
        if(!partialClear)
        {
            if (activeLauncher != null)
            {
                activeLauncher.RemoveSelection();
            }
            if (activeFrame != null)
            {
                activeFrame.RemoveSelection();
            }
        }
        targetTowerInventoryOBJ = -1;
        StateMachine.Instance.SwitchState(StateMachine.GameState.NormalMode);
        ClearTowerIndicator();
    }

    
    private void BuildTowerVisual(TowerBlueprint towerBlueprint)//build the indicator tower visuals by piecing the components together
    {
        GameObject launcher = Instantiate(towerBlueprint.LaunchSystemOBJ.launcSYSOBJ, towerIndicatorOBJ.transform.position, towerBlueprint.LaunchSystemOBJ.launcSYSOBJ.transform.rotation, towerIndicatorOBJ.transform);
        GameObject frame = Instantiate(towerBlueprint.FrameOBJ.frameOBJ, towerIndicatorOBJ.transform.position, towerBlueprint.FrameOBJ.frameOBJ.transform.rotation, towerIndicatorOBJ.transform);

        Transform LauncherBase, FrameTop, FrameBase;

        if(LibraryLink.Instance.dataLibrary.GetNewFireMode(towerBlueprint).Contains("Line"))
        {
            Transform DirFX;
            DirFX = launcher.transform.Find("DirFX");
            if (DirFX != null)
            {
                DirFX.gameObject.SetActive(true);
            }
        }

        LauncherBase = launcher.transform.Find("LauncherBase");
        FrameTop = frame.transform.Find("FrameTop");
        FrameBase = frame.transform.Find("FrameBase");

        launcher.transform.position = FrameTop.position - LauncherBase.localPosition;
        launcher.transform.localScale = FrameTop.localScale;

        float range = LibraryLink.Instance.dataLibrary.GetNewRange(towerBlueprint);

        if(LibraryLink.Instance.dataLibrary.GetNewFireMode(towerBlueprint) == "Arc")
        {
            Instantiate(LibraryLink.Instance.dataLibrary.deadZoneVis, FrameBase.position, FrameBase.rotation, FrameBase);
        }

        GameObject rangeCircle = Instantiate(LibraryLink.Instance.dataLibrary.rangeCircle, FrameBase.position, FrameBase.rotation, FrameBase);
        rangeCircle.transform.localScale = new Vector3(range * 2 + 1.25f, .5f, range * 2 + 1.25f);
        rangeIndicatorController = rangeCircle.GetComponent<RangeIndicatorControler>();
    }
    private void ClearTowerIndicator()
    {
        foreach (Transform child in towerIndicatorOBJ.transform)
        {
            Destroy(child.gameObject);
        }
        towerIndicatorOBJ.transform.rotation = Quaternion.identity;
        rangeIndicatorController = null;
    }


    //tower button UI
    public IEnumerator InitializeTowerButtons(List<TowerInventoryObject> towerInventoryList)
    {
        canTriggerButtons = false;
        TowerButtonData currentButton = null;
        TextMeshProUGUI[] buttonText = null;

        yield return new WaitForSeconds(1f);

        for (int i = 0; i != towerInventoryList.Count; i++)
        {
            ActionBarController.Instance.towerButtonScripts[i].gameObject.SetActive(true);
            ActionBarController.Instance.towerButtonScripts[i].transform.DOScale(Vector3.one, .33f).SetEase(Ease.Linear).SetUpdate(UpdateType.Normal, true);
            currentButton = ActionBarController.Instance.towerButtonScripts[i];
            buttonText = currentButton.textOBJs;
            switch(towerInventoryList[i].Type)
            {
                case TowerButtonType.Launcher:
                    currentButton.launcher = towerInventoryList[i].launcher;
                    currentButton.Type = TowerButtonType.Launcher;
                    break;
                case TowerButtonType.Frame:
                    currentButton.frame = towerInventoryList[i].frame;
                    currentButton.Type = TowerButtonType.Frame;
                    break;
            }
            currentButton.ShowBlackout();
            currentButton.buttonNum = i;

            if (i != 9)
            {
                buttonText[1].text = $"{i + 1}";
            }
            else
            {
                buttonText[1].text = $"0";
            }

            activeTowerButtonCounter = i + 1;
            yield return new WaitForSeconds(.15f);
        }

        canTriggerButtons = true;
        UpdateTowerButtonGrayOut();
    }
    public IEnumerator UpdateTowerButtons(List<TowerInventoryObject> towerInventoryList)
    {
        canTriggerButtons = false;
        TowerButtonData currentButton = null;
        TextMeshProUGUI[] buttonText = null;

        foreach(TowerButtonData button in ActionBarController.Instance.towerButtonScripts)
        {
            button.gameObject.SetActive(false);
        }

        for (int i = 0; i != towerInventoryList.Count; i++)
        {
            ActionBarController.Instance.towerButtonScripts[i].gameObject.SetActive(true);
            if (ActionBarController.Instance.towerButtonScripts[i].transform.localScale == Vector3.zero)
            {
                ActionBarController.Instance.towerButtonScripts[i].transform.DOScale(Vector3.one, .33f).SetEase(Ease.Linear).SetUpdate(UpdateType.Normal, true);
            }
            currentButton = ActionBarController.Instance.towerButtonScripts[i];
            buttonText = currentButton.textOBJs;
            currentButton.ClearButton();
            switch (towerInventoryList[i].Type)
            {
                case TowerButtonType.Launcher:
                    currentButton.launcher = towerInventoryList[i].launcher;
                    currentButton.Type = TowerButtonType.Launcher;
                    break;
                case TowerButtonType.Frame:
                    currentButton.frame = towerInventoryList[i].frame;
                    currentButton.Type = TowerButtonType.Frame;
                    break;
            }
            currentButton.ShowBlackout();

            if (i != 9)
            {
                buttonText[1].text = $"{i + 1}";
            }
            else
            {
                buttonText[1].text = $"0";
            }

            activeTowerButtonCounter = i + 1;
            yield return new WaitForSeconds(.1f);
        }

        canTriggerButtons = true;
        UpdateTowerButtonGrayOut();     
    }
    public void UpdateTowerButtonGrayOut()
    {
        TowerButtonData currentButton = null;

        for (int i = 0; i != TowerInventory.Instance.towerHandPile.Count; i++)
        {
            currentButton = ActionBarController.Instance.towerButtonScripts[i];

            currentButton.CheckForGreyOut();
        }
    }
    public void CreateFusionTower()
    {
        if(activeFrame != null && activeLauncher != null)
        {
            StateMachine.Instance.SwitchState(StateMachine.GameState.BuildMode);
            BuyTower(1, LibraryLink.Instance.dataLibrary.CreateSpecificTower(activeLauncher.launcher.ID, activeFrame.frame.ID));
        }
        else if (activeLauncher != null)
        {
            StateMachine.Instance.SwitchState(StateMachine.GameState.BuildMode);
            BuyTower(1, LibraryLink.Instance.dataLibrary.CreateSpecificTower(activeLauncher.launcher.ID, "F_002"));
        }
        else if (activeFrame != null)
        {
            StateMachine.Instance.SwitchState(StateMachine.GameState.BuildMode);
            BuyTower(1, LibraryLink.Instance.dataLibrary.CreateSpecificTower("LS_005", activeFrame.frame.ID));
        }
    }
    public void SlotInTowerPart(string slotType, TowerButtonData data, int num)
    {
        Debug.Log(slotType);
        switch (slotType)
        {
            case "Launcher":
                if (data == activeLauncher)
                {
                    activeLauncher.RemoveSelection();
                }
                else
                {
                    if (activeLauncher != null)
                    {
                        activeLauncher.RemoveSelection();
                    }
                    activeLauncher = data;
                    data.SetAsActiveComponent();
                    activeButtonNums.Add(data.buttonNum);
                }
                break;
            case "Frame":
                if (data == activeFrame)
                {
                    activeFrame.RemoveSelection();
                }
                else
                {
                    if (activeFrame != null)
                    {
                        activeFrame.RemoveSelection();
                    }
                    activeFrame = data;
                    data.SetAsActiveComponent();
                    activeButtonNums.Add(data.buttonNum);
                }
                break;
        }
    }


    [Command, Button]
    public void CreateNewCombatTower(string LauncherID = "", string FrameID = "")
    {
        //TowerInventory.Instance.AddTowerToInventory(LibraryLink.Instance.dataLibrary.CreateSpecificTower("Combat", LauncherID, FrameID));
    }
    [Command, Button]
    public void CreateNewSupportTower(string LauncherID = "", string FrameID = "")
    {
        //TowerInventory.Instance.AddTowerToInventory(LibraryLink.Instance.dataLibrary.CreateSpecificTower("Support", LauncherID, FrameID));
    }

    public IEnumerator BuildModeChecker()
    {
        while (true)
        {
            yield return new WaitForSecondsRealtime(.1f);
            if(!GameMenuLogic.GameIsPaused)
            {
                if (StateMachine.Instance.CurrentState == StateMachine.GameState.BuildMode && Input.mousePosition.y < Screen.height * .25f)
                {
                    ClearTowerPlacementData(true);
                }
                else if (StateMachine.Instance.CurrentState == StateMachine.GameState.NormalMode && Input.mousePosition.y > Screen.height * .25f)
                {
                    CreateFusionTower();
                }
            }
        }
    }
}
