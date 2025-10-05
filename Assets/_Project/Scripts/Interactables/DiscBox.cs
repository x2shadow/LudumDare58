using UnityEngine;

public class DiscBox : MonoBehaviour, IInteractable
{
    [Header("Refs")]
    public BuildManager buildManager; // чтобы найти combo по имени
    public BuildUIController buildUIController; // чтобы создать иконку новой механики внизу ( RevealNewMechanic )
    public GameCollectionManager collectionManager;
    public Transform depositPoint; // куда "помещать" визуально диск (опционально)
    public bool destroyOnDeposit = true;
    public DialogueRunner dialogueRunner;   // Ссылка на DialogueRunner

    void Awake()
    {
        if (dialogueRunner == null) dialogueRunner = GameObject.FindObjectOfType<DialogueRunner>();
    }

    public void Interact(PlayerController player)
    {
        var hold = player.GetComponent<PlayerHoldItem>();
        if (hold == null) return;
        if (!hold.HasDisc) return;

        var disc = hold.GetHeldDisc();
        if (disc == null) return;

        // приём диска: обновляем коллекцию
        bool newlyUnlocked = collectionManager?.UnlockGame(disc.gameName) ?? false;

        // если сюжетный — помечаем и блокируем взаимодействия кроме сна
        if (disc.isStory)
        {
            // флаг у игрока: может только идти спать
            player.SetInteractionOnlySleepMode(true); // ниже опишем метод в PlayerController
        }

        // если есть диалог
        if (disc.dialogueScript)
        {
            dialogueRunner.StartDialogue(disc.dialogueScript, 0);
        }

        // визуально положим диск в коробку (опционально)
        if (depositPoint != null && hold.GetHeldGameObject() != null)
        {
            var go = hold.GetHeldGameObject();
            go.transform.SetParent(depositPoint, false);
            //go.transform.localPosition = Vector3.zero;
            //go.transform.localRotation = Quaternion.identity;
        }

        // удаляем у игрока-held объект (если destroyOnDeposit) и очищаем
        if (destroyOnDeposit) hold.RemoveHeldDisc();
        else
        {
            // если не удаляем — просто отвязываем (уже установлено parent=depositPoint)
            hold.RemoveHeldDisc(); // либо не удалять вовсе — тут выбор дизайна
        }

        // Сообщаем collectionManager, что был сдан сюжетный диск (если он сюжетный)
        if (disc.isStory)
        {
            collectionManager.RecordStoryDiscSubmitted(disc.gameName);
        }

        // если это была 6-я сдача — collectionManager сам вызовет EndGame

        // 5) --- РАСКРЫТИЕ НОВОЙ МЕХАНИКИ (если combo содержит unlockMechanicName)
        if (newlyUnlocked && buildManager != null && buildUIController != null)
        {
            var combo = buildManager.GetComboByGameName(disc.gameName);
            if (combo != null && !string.IsNullOrEmpty(combo.unlockMechanicName))
            {
                // RevealNewMechanic принимает (name, sprite)
                buildUIController.RevealNewMechanic(combo.unlockMechanicName, combo.unlockMechanicIcon);
            }
        }
    }

    public bool GetUsed() => false;
}
