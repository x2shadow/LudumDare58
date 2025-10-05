using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class BuildUIController : MonoBehaviour
{
    [Header("UI / Prefabs")]
    public GameObject listItemPrefab;
    public RectTransform dropZoneParent;
    [Tooltip("Где лежат иконки внизу (parent для иконок)")]
    public Transform iconSourceParent;
    [Tooltip("prefab иконки механики (тот же, что внизу)")]
    public GameObject mechanicIconPrefab;

    [Header("References")]
    public GameIdeaBoard gameIdeaBoard;
    public GameObject pcCanvas;
    public RectTransform iconDragRoot;

    [Header("Drop layout")]
    [Tooltip("Если у тебя нет VerticalLayoutGroup, используем этот оффсет при добавлении")]
    public float listItemSpacing = 40f;


    [Header("Player / Camera")]
    public PlayerController playerController;
    public Transform cinemachineTarget;
    public float cameraTransitionSpeed = 6f;

    [Header("Build")]
    public BuildManager buildManager;
    public Button buildButton;
    public Text infoText;

    [Header("Collection")]
    public GameCollectionManager collectionManager; // ссылка на менеджер коллекции

    private bool isOpen = false;
    private Vector3 originalTargetLocalPos;
    private Quaternion originalTargetLocalRot;
    private Coroutine cameraCoroutine;
    private List<string> currentMechanicNames = new List<string>();

    private void Awake()
    {
        if (pcCanvas != null) pcCanvas.SetActive(false);
        if (buildButton != null) buildButton.onClick.AddListener(OnBuildButton);
    }

    public void Open(PlayerController player, Transform cameraViewPoint)
    {
        if (isOpen) return;
        isOpen = true;

        playerController.SetInputBlocked(true);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        originalTargetLocalPos = cinemachineTarget.localPosition;
        originalTargetLocalRot = cinemachineTarget.localRotation;

        if (cameraCoroutine != null) StopCoroutine(cameraCoroutine);
        cameraCoroutine = StartCoroutine(AnimateCinemachineTargetToView(cameraViewPoint));

        pcCanvas.SetActive(true);
        EventSystem.current?.SetSelectedGameObject(null);
    }

    private IEnumerator AnimateCinemachineTargetToView(Transform viewPoint)
    {
        if (viewPoint == null) yield break;

        float t = 0f;
        Vector3 startPos = cinemachineTarget.position;
        Quaternion startRot = cinemachineTarget.rotation;
        Vector3 endPos = viewPoint.position;
        Quaternion endRot = viewPoint.rotation;

        while (t < 1f)
        {
            t += Time.deltaTime * cameraTransitionSpeed;
            cinemachineTarget.position = Vector3.Lerp(startPos, endPos, t);
            cinemachineTarget.rotation = Quaternion.Slerp(startRot, endRot, t);
            yield return null;
        }
    }

    public void Close()
    {
        if (!isOpen) return;
        isOpen = false;

        pcCanvas.SetActive(false);

        if (cameraCoroutine != null) StopCoroutine(cameraCoroutine);
        cameraCoroutine = StartCoroutine(RestoreCinemachineTarget());

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        playerController.SetInputBlocked(false);

        // Очистить текущие механики (это запасная очистка на случай)
        ClearMechanics();
    }

    private IEnumerator RestoreCinemachineTarget()
    {
        float t = 0f;
        Vector3 startPos = cinemachineTarget.localPosition;
        Quaternion startRot = cinemachineTarget.localRotation;
        Vector3 endPos = originalTargetLocalPos;
        Quaternion endRot = originalTargetLocalRot;

        while (t < 1f)
        {
            t += Time.deltaTime * cameraTransitionSpeed;
            cinemachineTarget.localPosition = Vector3.Lerp(startPos, endPos, t);
            cinemachineTarget.localRotation = Quaternion.Slerp(startRot, endRot, t);
            yield return null;
        }
    }

    public void AddMechanic(string mechanicName)
    {
        GameObject go = Instantiate(listItemPrefab, dropZoneParent);
        var item = go.GetComponent<MechanicListItem>();
        if (item != null) item.Setup(mechanicName, this);
        else
        {
            var text = go.GetComponentInChildren<Text>();
            if (text) text.text = mechanicName;
        }

        // позиционирование со смещением (если НЕ используешь VerticalLayoutGroup)
        // если у контейнера есть LayoutGroup — он сам расставит элементы,
        // но если нет — ставим вручную оффсет.
        var rt = go.GetComponent<RectTransform>();
        if (rt != null)
        {
            int index = dropZoneParent.childCount - 1; // новый элемент — последний
            Vector2 anchored = new Vector2(rt.anchoredPosition.x, -index * listItemSpacing);
            rt.anchoredPosition = anchored;
        }

        currentMechanicNames.Add(mechanicName);
    }

    private void OnBuildButton()
    {
        MechanicCombo combo = buildManager.Evaluate(currentMechanicNames);

        // Очистка DropZone сразу после билда (по требованию)
        ClearMechanics();

        if (combo != null)
        {
            // разблокируем в коллекции
            bool newlyUnlocked = collectionManager?.UnlockGame(combo.gameName) ?? false;

            // следующая идея
            gameIdeaBoard?.OnBuildCompleted(combo.gameName);

            // если это первый раз и combo.unlockMechanicName задана — добавляем новую механику в нижнюю панель
            if (newlyUnlocked && !string.IsNullOrEmpty(combo.unlockMechanicName) && mechanicIconPrefab != null && iconSourceParent != null)
            {
                RevealNewMechanic(combo.unlockMechanicName, combo.unlockMechanicIcon);
            }

            StartCoroutine(ShowResultAndClose($"Build successful: {combo.gameName}", true));
        }
        else
        {
            StartCoroutine(ShowResultAndClose("Build failed", false));
        }
    }

    private void RevealNewMechanic(string mechName, Sprite mechIcon)
    {
        // Instantiate icon prefab in the bottom panel (iconSourceParent)
        GameObject iconGO = Instantiate(mechanicIconPrefab, iconSourceParent, false);
        var draggable = iconGO.GetComponent<DraggableMechanicIcon>();
        if (draggable != null)
        {
            draggable.mechanicName = mechName;
            // try to find Image field and assign sprite
            if (draggable.iconImage != null && mechIcon != null) draggable.iconImage.sprite = mechIcon;
            // ensure dragRoot is set
            if (draggable.dragRoot == null && iconDragRoot != null) draggable.dragRoot = iconDragRoot;
        }
        else
        {
            // fallback: try to find an Image child to set sprite
            var img = iconGO.GetComponentInChildren<Image>();
            if (img != null && mechIcon != null) img.sprite = mechIcon;
        }

        // optional: give some animation/pop effect here (scale up, fade in), but keep minimal for jam
    }

    private void ClearMechanics()
    {
        if (dropZoneParent != null)
        {
            for (int i = dropZoneParent.childCount - 1; i >= 0; i--)
            {
                Destroy(dropZoneParent.GetChild(i).gameObject);
            }
        }
        currentMechanicNames.Clear();
    }

    public void RemoveMechanic(GameObject listItem, string mechanicName)
    {
        if (listItem != null) Destroy(listItem);
        currentMechanicNames.Remove(mechanicName);
    }

    private IEnumerator ShowResultAndClose(string message, bool success)
    {
        if (infoText != null)
        {
            infoText.text = message;
        }

        float wait = success ? 1.2f : 1.0f;
        float timer = 0f;
        while (timer < wait)
        {
            timer += Time.unscaledDeltaTime;
            yield return null;
        }

        Close();
        
        infoText.text = "InfoText";
    }

    // (опционально) если другой код хочет принудительно очистить
    public void ForceClearMechanics() => ClearMechanics();
}
