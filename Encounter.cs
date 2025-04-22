using UnityEngine;
using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;


[CreateAssetMenu(fileName = "NewEncounter", menuName = "ScriptableObjects/Encounter", order = 1)]
public class Encounter : ScriptableObject
{
    public string encounterTitle; // Name for the encounter

    public GameObject map; // Map for the encounter

    public EncounterType encounterType; // Set the encounter type

    // This array will only show when encounterType is Random
    [ShowIf("ShouldShowMainDialogue")]
    public DialogueOption[] dialogues;

    // This field will show when ShouldShowMainDialogue returns true
    [ShowIf("ShouldShowMainDialogue")]
    public string mainDialogue; // Dialogue box

    // Helper method to control when mainDialogue should be visible
    private bool ShouldShowMainDialogue()
    {
        return encounterType == EncounterType.Random ||
               encounterType == EncounterType.Rest ||
               encounterType == EncounterType.Treasure||
               encounterType == EncounterType.Shop;
    }
}

[Serializable]
public class DialogueOption
{
    [Serializable]
    public enum RewardType
    {
        Currency, Launcher, Frame, Enhancement, Healing
    }

    public RewardType type;
    public bool setReward;
    public bool pooledRewards;

    public string dialogue;

    [ShowIf("ShowReward")]
    public int currencyVal;
    internal LaunchSystem launcher;
    internal Frames frame;
    internal Enhancement enhancement;

    [ShowIf("ShowReward")]
    public string rewardID;

    [ShowIf("ShowPool")]
    public List<string> rewardIDs = new();
    [ShowIf("ShowPool")]
    public Vector2 currencyRange;

    private bool ShowReward()
    {
        return setReward;
    }
    private bool ShowPool()
    {
        return pooledRewards;
    }
}