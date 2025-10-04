using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class BuildUIController : MonoBehaviour
{
    [Header("References")]
    public GameObject pcCanvas;
    public RectTransform dropZoneParent;
    public GameObject listItemPrefab;
    public Transform iconSourceParent;
    public RectTransform iconDragRoot;

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
        if (item != null)
        {
            item.Setup(mechanicName, this);
        }
        else
        {
            var text = go.GetComponentInChildren<Text>();
            if (text) text.text = mechanicName;
        }
        currentMechanicNames.Add(mechanicName);
    }

    public void RemoveMechanic(GameObject listItem, string mechanicName)
    {
        if (listItem != null) Destroy(listItem);
        currentMechanicNames.Remove(mechanicName);
    }

    private void OnBuildButton()
    {
        // Оцениваем билд
        string resultGame = buildManager.Evaluate(currentMechanicNames);

        // Сразу после билда очищаем DropZone (по требованию)
        ClearMechanics();

        if (!string.IsNullOrEmpty(resultGame))
        {
            // Успешный билд — разблокируем игру в коллекции
            collectionManager?.UnlockGame(resultGame);
            StartCoroutine(ShowResultAndClose($"Build successful: {resultGame}", true));
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

        float wait = success ? 1.2f : 1.0f;
        float timer = 0f;
        while (timer < wait)
        {
            timer += Time.unscaledDeltaTime;
            yield return null;
        }

        Close();
    }

    // Удаляем все элементы в dropZoneParent и очищаем список
    private void ClearMechanics()
    {
        if (dropZoneParent != null)
        {
            for (int i = dropZoneParent.childCount - 1; i >= 0; i--)
            {
                var child = dropZoneParent.GetChild(i).gameObject;
                Destroy(child);
            }
        }
        currentMechanicNames.Clear();
    }

    // (опционально) если другой код хочет принудительно очистить
    public void ForceClearMechanics() => ClearMechanics();
}
