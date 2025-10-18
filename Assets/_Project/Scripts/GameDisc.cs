using UnityEngine;

public class GameDisc : MonoBehaviour
{
    public string gameName;
    public bool isStory = false;
    public DialogueScript dialogueScript;
    public Sprite icon; // если хочешь отображать спрайт в UI
    public GameObject coverImage;

    // optional: звуки/эффекты при сдаче можно тут же запускать

    // helper: задать данные на инстансе
    public void Setup(string gameName, bool isStory, Sprite icon = null, DialogueScript dialogueScript = null)
    {
        this.gameName = gameName;
        this.isStory = isStory;
        this.icon = icon;
        this.dialogueScript = dialogueScript;

        coverImage.GetComponent<MeshRenderer>().material.mainTexture = icon.texture;
        coverImage.GetComponent<MeshRenderer>().material.SetTexture("_EmissionMap", icon.texture);
    }
}
