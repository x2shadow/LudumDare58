using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class BuildUIController : MonoBehaviour
{
    [Header("References")]
    public GameObject pcCanvas; // Canvas с UI билдера (слева/снизу)
    public RectTransform dropZoneParent; // контейнер для списка в левой панели (content)
    public GameObject listItemPrefab; // prefab для записи механики (Text + maybe icon + MechanicListItem)
    public Transform iconSourceParent; // панель с иконками внизу (там исходные иконки)
    public RectTransform iconDragRoot; // top-level canvas rect transform куда при перетаске помещаем иконку

    [Header("Player / Camera")]
    public PlayerController playerController; // твой существующий контроллер
    public Transform cinemachineTarget; // CinemachineCameraTarget (из PlayerController)
    [Tooltip("Скорость перехода камеры")]
    public float cameraTransitionSpeed = 6f;

    [Header("Build")]
    public BuildManager buildManager; // проверка комбинаций
    public Button buildButton;
    public Text infoText; // короткие сообщения Success/Error

    // internal
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

        // Block player input (this also prevents look/move)
        playerController.SetInputBlocked(true);

        // show cursor
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // store original cinemachine target local transform (so we can restore)
        originalTargetLocalPos = cinemachineTarget.localPosition;
        originalTargetLocalRot = cinemachineTarget.localRotation;

        // If cameraViewPoint is provided, animate target local position/rotation to match that point in world-space
        if (cameraCoroutine != null) StopCoroutine(cameraCoroutine);
        cameraCoroutine = StartCoroutine(AnimateCinemachineTargetToView(cameraViewPoint));

        // Show UI
        pcCanvas.SetActive(true);
        // optionally select first UI element
        EventSystem.current?.SetSelectedGameObject(null);
    }

    private IEnumerator AnimateCinemachineTargetToView(Transform viewPoint)
    {
        if (viewPoint == null) yield break;

        // We animate in world space by setting cinemachineTarget.position/rotation towards the viewPoint.
        // But cinemachineTarget may be childed to player — to be robust, animate world position.
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

        // hide UI
        pcCanvas.SetActive(false);

        // restore camera target
        if (cameraCoroutine != null) StopCoroutine(cameraCoroutine);
        cameraCoroutine = StartCoroutine(RestoreCinemachineTarget());

        // hide cursor and return control to player
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        playerController.SetInputBlocked(false);
        currentMechanicNames.Clear();
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

    // Called from DropZone when a mechanic is added
    public void AddMechanic(string mechanicName)
    {
        // create list item UI
        GameObject go = Instantiate(listItemPrefab, dropZoneParent);
        var item = go.GetComponent<MechanicListItem>();
        if (item != null)
        {
            item.Setup(mechanicName, this);
        }
        else
        {
            Debug.Log("item == null");
            // fallback: if prefab just has Text
            var text = go.GetComponentInChildren<Text>();
            if (text) text.text = mechanicName;
        }
        currentMechanicNames.Add(mechanicName);
    }

    // Called by MechanicListItem on right-click
    public void RemoveMechanic(GameObject listItem, string mechanicName)
    {
        if (listItem != null) Destroy(listItem);
        currentMechanicNames.Remove(mechanicName);
    }

    private void OnBuildButton()
    {
        bool ok = buildManager.Evaluate(currentMechanicNames);
        if (ok)
        {
            StartCoroutine(ShowResultAndClose("Build successful!", true));
        }
        else
        {
            StartCoroutine(ShowResultAndClose("Build failed", false));
        }
    }

    private IEnumerator ShowResultAndClose(string message, bool success)
    {
        if (infoText != null)
        {
            infoText.text = message;
        }
        // немного подождать, чтобы пользователь видел результат (в джеме небольшая пауза ок)
        float wait = success ? 1.2f : 1.0f;
        float timer = 0f;
        while (timer < wait)
        {
            timer += Time.unscaledDeltaTime; // UI time independent
            yield return null;
        }

        Close();
        infoText.text = "InfoText";
    }
}
