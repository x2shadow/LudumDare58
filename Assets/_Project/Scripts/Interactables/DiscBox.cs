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

        // Если дисk сюжетный - регистрируем сдачу, но только если это ПЕРВАЯ сдача данного диска
        if (disc.isStory)
        {
            bool firstTime = collectionManager.RecordStoryDiscSubmitted(disc.gameName);
            if (firstTime)
            {
                // только при первой сдаче даём игроку блокировку ПК до следующего сна
                player.SetPCBlocked(true);
                // (не даём "onlyAllowSleep", игрок может взаимодействовать со всем кроме ПК)
                if (disc.dialogueScript) dialogueRunner.StartDialogue(disc.dialogueScript, 0);
            }
            else
            {
                // повторная сдача — не меняем сюжет и не даём ещё один permit
                Debug.Log("Story disc already submitted before — not advancing story or giving extra sleep permit.");
            }
        }

        // если есть диалог
        if (disc.dialogueScript && !disc.isStory)
        {
            dialogueRunner.StartDialogue(disc.dialogueScript, 0);
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
