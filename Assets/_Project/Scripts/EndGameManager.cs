using UnityEngine;

public class EndGameManager : MonoBehaviour
{
    public static EndGameManager Instance;
    //public GameObject endGameCanvas; // UI для финала

    private void Awake()
    {
        if (Instance != null) Destroy(this.gameObject);
        else Instance = this;
    }

    public void EndGame()
    {
        Debug.Log("END GAME triggered");
        //if (endGameCanvas != null) endGameCanvas.SetActive(true);
        // блокируем ввод игрока
        //var player = FindObjectOfType<PlayerController>();
        //if (player != null) player.SetInputBlocked(true);

        // здесь можно: показать credits, сохранить прогресс, загрузить сцену и т.д.
    }
}
