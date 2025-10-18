using UnityEngine;

public class PCTerminal : MonoBehaviour, IInteractable
{
    [Header("Setup")]
    [Tooltip("Точка куда камера должна подлететь (позиция + rotation)")]
    public Transform cameraViewPoint;

    [Tooltip("Ссылка на контроллер UI билдера")]
    public BuildUIController buildUIController;

    private bool used = false;

    public void Interact(PlayerController player)
    {
        if (used) return;
        if (buildUIController == null)
        {
            Debug.LogWarning("BuildUIController not assigned on PCTerminal.");
            return;
        }
        buildUIController.Open(player, cameraViewPoint);
    }

    public bool GetUsed()
    {
        return used;
    }

    // опционально: пометка как использованного (если нужно)
    public void SetUsed(bool v) => used = v;
}
