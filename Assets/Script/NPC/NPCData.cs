using UnityEngine;

[CreateAssetMenu(fileName = "NPCData", menuName = "Scriptable Objects/NPCData")]
public class NPCData : ScriptableObject
{
    public string npcName;
    public GameObject package;

    [Header("Static NPC Dialogue")]
    public ItemDialogueData staticDialogue;
}
