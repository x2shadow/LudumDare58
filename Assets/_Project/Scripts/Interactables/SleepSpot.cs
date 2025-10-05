using UnityEngine;

public class SleepSpot : MonoBehaviour, IInteractable
{
    public Transform sleepCameraView; // куда подлетаем камерой на sleep, опционально
    public float sleepDuration = 3f;

    public void Interact(PlayerController player)
    {
        // разрешаем спать только если collectionManager.storySubmittedCount > 0 или player.onlyAllowSleep == true
        var coll = FindObjectOfType<GameCollectionManager>();
        if (coll == null || coll.storySubmittedCount <= 0)
        {
            Debug.Log("You cannot sleep yet — submit a story disc first.");
            return;
        }

        // если всё ок — запускаем корутину сна (можно в PlayerController или тут)
        player.SetInputBlocked(true);
        // Запустить анимацию сна / затем снять флаги
        StartCoroutine(DoSleep(player));
    }

    private System.Collections.IEnumerator DoSleep(PlayerController player)
    {
        // можно плывущую анимацию / камера / затем установить флажок, что игрок снова активен
        yield return new WaitForSecondsRealtime(sleepDuration);

        // после сна снимаем onlyAllowSleep
        player.SetInteractionOnlySleepMode(false);
        player.SetInputBlocked(false);

        // опционально: продвинуть день, восстановить здоровье и т.д.
        Debug.Log("You slept.");
    }

    public bool GetUsed()
    {
        return false;
    }
}
