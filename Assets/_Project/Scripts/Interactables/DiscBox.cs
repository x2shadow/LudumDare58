using UnityEngine;

public class DiscBox : MonoBehaviour, IInteractable
{
    public GameCollectionManager collectionManager;
    public Transform depositPoint; // куда "помещать" визуально диск (опционально)
    public bool destroyOnDeposit = true;

    public void Interact(PlayerController player)
    {
        var hold = player.GetComponent<PlayerHoldItem>();
        if (hold == null) return;
        if (!hold.HasDisc) return;

        var disc = hold.GetHeldDisc();
        if (disc == null) return;

        // приём диска: обновляем коллекцию
        collectionManager.UnlockGame(disc.gameName);

        // если сюжетный — помечаем и блокируем взаимодействия кроме сна
        if (disc.isStory)
        {
            // флаг у игрока: может только идти спать
            player.SetInteractionOnlySleepMode(true); // ниже опишем метод в PlayerController
        }

        // визуально положим диск в коробку (опционально)
        if (depositPoint != null && hold.GetHeldGameObject() != null)
        {
            var go = hold.GetHeldGameObject();
            go.transform.SetParent(depositPoint, false);
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.identity;
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
    }

    public bool GetUsed() => false;
}
