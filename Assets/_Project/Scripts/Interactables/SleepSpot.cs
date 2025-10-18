using UnityEngine;
using System.Collections;

public class SleepSpot : MonoBehaviour, IInteractable
{
    public float sleepDuration = 0.5f;
    public float fadeDuration = 0.25f; // Длительность затемнения/разтемнения

    private ScreenFader screenFader;

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

        if (screenFader == null) screenFader = FindObjectOfType<ScreenFader>();

        // если разрешение есть — разрешаем спать (и снимаем режим "только сон" после сна)
        StartCoroutine(DoSleepCoroutine(player));
    }

    private IEnumerator DoSleepCoroutine(PlayerController player)
    {
        // блокируем управление на время сна
        player.SetInputBlocked(true);
        // тут можно сделать подлёт камеры к sleepCameraView, проиграть анимацию и т.д.
        yield return screenFader.FadeIn(fadeDuration);

        // ждём реального времени (не Time.timeScale)
        yield return new WaitForSecondsRealtime(sleepDuration);

        yield return screenFader.FadeOut(fadeDuration);

        // после сна — снимаем специальный режим, чтобы игрок снова мог взаимодействовать нормально
        player.SetPCBlocked(false);
        player.SetInputBlocked(false);

        Debug.Log("You slept.");
    }

    public bool GetUsed() => false;
}
