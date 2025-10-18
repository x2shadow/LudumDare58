using UnityEngine;

public class PlayerHoldItem : MonoBehaviour
{
    [Tooltip("Точка на игроке (hand) куда прикреплять диск")]
    public Transform holdPoint;
    private GameObject heldGO;
    private GameDisc heldDisc;

    public bool HasDisc => heldDisc != null;

    public GameDisc GetHeldDisc() => heldDisc;

    // Взять диск: instantiate prefab и прикрепить
    public void PickupDisc(GameObject discPrefab, string gameName, bool isStory, Sprite icon = null, DialogueScript dialogueScript = null)
    {
        if (heldGO != null) Destroy(heldGO);

        heldGO = Instantiate(discPrefab, holdPoint ? holdPoint : this.transform);
        heldGO.transform.localPosition = Vector3.zero;
        //heldGO.transform.localRotation = Quaternion.identity;
        //heldGO.transform.localScale = Vector3.one;

        heldDisc = heldGO.GetComponent<GameDisc>();
        if (heldDisc != null)
            heldDisc.Setup(gameName, isStory, icon, dialogueScript);

        // Костыль подсказки
        if (InteractionIndicatorManager.Instance != null) InteractionIndicatorManager.Instance.ResetAll();
    }

    // Удалить диск (после сдачи)
    public void RemoveHeldDisc()
    {
        if (heldGO != null) Destroy(heldGO);
        heldGO = null;
        heldDisc = null;
    }

    // Возврат игрового объекта held (если нужен)
    public GameObject GetHeldGameObject() => heldGO;
}
