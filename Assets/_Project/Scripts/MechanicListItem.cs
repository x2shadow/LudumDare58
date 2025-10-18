using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class MechanicListItem : MonoBehaviour, IPointerClickHandler
{
    public TextMeshProUGUI label;
    private string mechanicName;
    private BuildUIController parentController;

    public void Setup(string name, BuildUIController parent)
    {
        mechanicName = name;
        parentController = parent;
        if (label) label.text = name;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Right)
        {
            parentController?.RemoveMechanic(gameObject, mechanicName);
        }
    }
}
