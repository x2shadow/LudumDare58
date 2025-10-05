using UnityEngine;
using System.Collections;

public class SleepSpot : MonoBehaviour, IInteractable
{
    public Transform sleepCameraView; // необязательно
    public float sleepDuration = 1f;

    public void Interact(PlayerController player)
    {
        // проверяем менеджер коллекции
        var coll = FindObjectOfType<GameCollectionManager>();
        if (coll == null)
        {
            Debug.LogWarning("SleepSpot: no GameCollectionManager found.");
            return;
        }

        // проверка — есть ли разрешение на сон (и одновременно забираем его)
        bool allowed = coll.ConsumeSleepPermit();
        if (!allowed)
        {
            Debug.Log("You cannot sleep yet — submit a story disc first.");
            // можно показать UI-подсказку
            return;
        }

        // если разрешение есть — разрешаем спать (и снимаем режим "только сон" после сна)
        StartCoroutine(DoSleepCoroutine(player));
    }

    private IEnumerator DoSleepCoroutine(PlayerController player)
    {
        // блокируем управление на время сна
        player.SetInputBlocked(true);
        // тут можно сделать подлёт камеры к sleepCameraView, проиграть анимацию и т.д.

        // ждём реального времени (не Time.timeScale)
        yield return new WaitForSecondsRealtime(sleepDuration);

        // после сна — снимаем специальный режим, чтобы игрок снова мог взаимодействовать нормально
        player.SetInteractionOnlySleepMode(false); // добавь этот метод в PlayerController если ещё нет
        player.SetInputBlocked(false);

        Debug.Log("You slept.");
    }

    public bool GetUsed() => false;
}
