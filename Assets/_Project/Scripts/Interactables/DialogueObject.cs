using UnityEngine;

public class DialogueObject : MonoBehaviour, IInteractable
{
    [SerializeField] DialogueScript dialogueScript;

    public bool used = false;

    public void Interact(PlayerController player)
    {
        if (used) return;
        Debug.Log("Объект использован!");
        player.dialogueRunner.StartDialogue(dialogueScript, 0);
        used = true;
    }

    public bool GetUsed()
    {
        return used;
    }
}
