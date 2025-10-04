using UnityEngine;
using UnityEngine.EventSystems;

public class MechanicDropZone : MonoBehaviour, IDropHandler
{
    public BuildUIController buildUIController;

    // For simplicity, draggable's GameObject has DraggableMechanicIcon with mechanicName
    public void OnDrop(PointerEventData eventData)
    {
        var dragged = eventData.pointerDrag;
        if (dragged == null) return;

        var draggable = dragged.GetComponent<DraggableMechanicIcon>();
        if (draggable == null) return;

        Debug.Log("Dropped " + draggable.mechanicName);
        buildUIController.AddMechanic(draggable.mechanicName);
    }
}
