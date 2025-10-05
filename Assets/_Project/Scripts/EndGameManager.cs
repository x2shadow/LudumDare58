using System.Collections;
using UnityEngine;

public class EndGameManager : MonoBehaviour
{
    public static EndGameManager Instance;
    public GameObject endGameWords; // UI для финала
    public GameObject credits; // UI для финала
    private ScreenFader screenFader;

    private void Awake()
    {
        if (Instance != null) Destroy(this.gameObject);
        else Instance = this;
        if (screenFader == null) screenFader = FindObjectOfType<ScreenFader>();
    }

    public void EndGame()
    {
        Debug.Log("END GAME triggered");
        StartCoroutine(EndGameCoroutine());
        // блокируем ввод игрока
        //var player = FindObjectOfType<PlayerController>();
        //if (player != null) player.SetInputBlocked(true);

        // здесь можно: показать credits, сохранить прогресс, загрузить сцену и т.д.
    }

    private IEnumerator EndGameCoroutine()
    {
        yield return new WaitForSecondsRealtime(3f);

        yield return screenFader.FadeIn(0.25f);

        endGameWords.SetActive(true);

        yield return new WaitForSecondsRealtime(1f);

        endGameWords.SetActive(false);

        yield return new WaitForSecondsRealtime(1f);

        credits.SetActive(true);
    }
}
