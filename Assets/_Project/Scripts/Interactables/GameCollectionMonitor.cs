using System.Collections;
using UnityEngine;

public class GameCollectionMonitor : MonoBehaviour, IInteractable
{
    [Header("Camera Viewpoint")]
    public Transform cameraViewPoint;

    [Header("References")]
    public PlayerController playerController;
    public Transform cinemachineTarget;

    [Tooltip("Как быстро подлетать и возвращать")]
    public float cameraTransitionSpeed = 6f;

    [Tooltip("Сколько держать камеру на мониторе (в секундах)")]
    public float lingerSeconds = 2f;

    private Vector3 originalLocalPos;
    private Quaternion originalLocalRot;

    public void Interact(PlayerController player)
    {
        // Сохраняем оригинал и запускаем подлет
        if (playerController == null) playerController = player;
        originalLocalPos = cinemachineTarget.localPosition;
        originalLocalRot = cinemachineTarget.localRotation;

        playerController.SetInputBlocked(true);
        //Cursor.lockState = CursorLockMode.None;
        //Cursor.visible = true;

        StartCoroutine(DoMonitorView());
    }

    public bool GetUsed() => false;

    private IEnumerator DoMonitorView()
    {
        // подлет
        yield return StartCoroutine(AnimateToWorldTransform(cameraViewPoint.position, cameraViewPoint.rotation));

        // ждём lingerSeconds (не блокируем Time.timeScale)
        float t = 0f;
        while (t < lingerSeconds)
        {
            t += Time.unscaledDeltaTime;
            yield return null;
        }

        // возврат
        yield return StartCoroutine(AnimateToLocalTransform(originalLocalPos, originalLocalRot));

        playerController.SetInputBlocked(false);
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private IEnumerator AnimateToWorldTransform(Vector3 targetPos, Quaternion targetRot)
    {
        float progress = 0f;
        Vector3 startPos = cinemachineTarget.position;
        Quaternion startRot = cinemachineTarget.rotation;

        while (progress < 1f)
        {
            progress += Time.deltaTime * cameraTransitionSpeed;
            cinemachineTarget.position = Vector3.Lerp(startPos, targetPos, progress);
            cinemachineTarget.rotation = Quaternion.Slerp(startRot, targetRot, progress);
            yield return null;
        }
    }

    private IEnumerator AnimateToLocalTransform(Vector3 localPos, Quaternion localRot)
    {
        float progress = 0f;
        Vector3 startPos = cinemachineTarget.localPosition;
        Quaternion startRot = cinemachineTarget.localRotation;

        while (progress < 1f)
        {
            progress += Time.deltaTime * cameraTransitionSpeed;
            cinemachineTarget.localPosition = Vector3.Lerp(startPos, localPos, progress);
            cinemachineTarget.localRotation = Quaternion.Slerp(startRot, localRot, progress);
            yield return null;
        }
    }
}
