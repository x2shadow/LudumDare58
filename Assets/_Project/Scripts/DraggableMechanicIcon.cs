using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class DraggableMechanicIcon : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public string mechanicName;
    public Image iconImage;

    private RectTransform rect;
    private Canvas canvas;
    private CanvasGroup canvasGroup;
    private Transform originalParent;
    private Vector2 originalAnchoredPos;
    public RectTransform dragRoot; // assign from BuildUIController.iconDragRoot

    private GameObject draggingObj;

    private void Awake()
    {
        rect = GetComponent<RectTransform>();
        canvas = GetComponentInParent<Canvas>();
        canvasGroup = gameObject.GetComponent<CanvasGroup>() ?? gameObject.AddComponent<CanvasGroup>();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        // create a copy to drag so original stays in bottom panel
        draggingObj = new GameObject("Drag_" + mechanicName, typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
        draggingObj.transform.SetParent(dragRoot ? dragRoot : canvas.transform, false);
        var img = draggingObj.GetComponent<Image>();
        if (iconImage) img.sprite = iconImage.sprite;
        img.raycastTarget = false;
        var rt = draggingObj.GetComponent<RectTransform>();
        rt.sizeDelta = rect.sizeDelta;

        canvasGroup.alpha = 0.6f;
        canvasGroup.blocksRaycasts = false;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (draggingObj == null) return;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(draggingObj.transform as RectTransform, eventData.position, eventData.pressEventCamera, out Vector2 lp);
        (draggingObj.transform as RectTransform).anchoredPosition = lp;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        canvasGroup.alpha = 1f;
        canvasGroup.blocksRaycasts = true;

        if (draggingObj != null)
        {
            Destroy(draggingObj);
            draggingObj = null;
        }

        // If dropped over a DropZone, PointerEventData will have pointerEnter be the drop target,
        // but better to rely on IDropHandler on drop zone (see MechanicDropZone).
    }
}
