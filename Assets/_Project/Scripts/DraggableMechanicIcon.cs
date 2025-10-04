using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(RectTransform))]
public class DraggableMechanicIcon : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public string mechanicName;
    public Image iconImage;

    [Tooltip("root для перетаскиваемой копии (обычно top-level RectTransform в Canvas)")]
    public RectTransform dragRoot;

    private RectTransform rect;
    private Canvas parentCanvas;
    private CanvasGroup myCanvasGroup;

    private GameObject draggingObj;
    private RectTransform draggingRect;
    private CanvasGroup draggingCanvasGroup;

    private void Awake()
    {
        rect = GetComponent<RectTransform>();
        parentCanvas = GetComponentInParent<Canvas>();
        myCanvasGroup = GetComponent<CanvasGroup>() ?? gameObject.AddComponent<CanvasGroup>();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        // ensure dragRoot
        if (dragRoot == null)
        {
            if (parentCanvas != null) dragRoot = parentCanvas.transform as RectTransform;
            else
            {
                Debug.LogWarning("DraggableMechanicIcon: dragRoot not set and no parent Canvas found.");
                dragRoot = transform.root as RectTransform;
            }
        }

        // Visual: dim original a bit and allow raycasts to pass through it
        myCanvasGroup.alpha = 0.6f;
        myCanvasGroup.blocksRaycasts = false;

        // Create top-level dragging copy (so original stays in layout)
        draggingObj = new GameObject("Drag_" + mechanicName, typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
        draggingObj.transform.SetParent(dragRoot, false);
        draggingRect = draggingObj.GetComponent<RectTransform>();
        draggingCanvasGroup = draggingObj.GetComponent<CanvasGroup>();
        var img = draggingObj.GetComponent<Image>();

        // copy sprite
        if (iconImage != null) img.sprite = iconImage.sprite;
        img.raycastTarget = false; // copy must not block raycasts (allow Drop target to receive events)

        // size
        draggingRect.sizeDelta = rect.sizeDelta;

        // Make sure dragging object is drawn above others
        //var c = draggingObj.AddComponent<Canvas>();
        //c.overrideSorting = true;
        //c.sortingOrder = 9999;

        // place it immediately under cursor (world position calculation)
        UpdateDraggingPosition(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (draggingObj == null) return;
        UpdateDraggingPosition(eventData);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        // restore original visual
        myCanvasGroup.alpha = 1f;
        myCanvasGroup.blocksRaycasts = true;

        if (draggingObj != null)
        {
            Destroy(draggingObj);
            draggingObj = null;
            draggingRect = null;
            draggingCanvasGroup = null;
        }
    }

    private void UpdateDraggingPosition(PointerEventData eventData)
    {
        // Convert screen point to world point in dragRoot rect
        // For Screen Space - Overlay use camera = null, for Camera mode use eventData.pressEventCamera
        Camera cam = (parentCanvas != null && parentCanvas.renderMode != RenderMode.ScreenSpaceOverlay) 
                      ? (eventData.pressEventCamera ?? parentCanvas.worldCamera) 
                      : null;

        Vector3 worldPos;
        bool ok = RectTransformUtility.ScreenPointToWorldPointInRectangle(dragRoot, eventData.position, cam, out worldPos);
        if (ok && draggingRect != null)
        {
            draggingRect.position = worldPos;
            // optional: match rotation/scale
            draggingRect.rotation = dragRoot.rotation;
        }
        else
        {
            // fallback: put at event position in screen space (convert)
            Vector3 screenPos = new Vector3(eventData.position.x, eventData.position.y, 0f);
            draggingRect.position = screenPos;
        }
    }
}
