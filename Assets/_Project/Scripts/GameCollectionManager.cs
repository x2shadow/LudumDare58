using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GameCollectionManager : MonoBehaviour
{
    [System.Serializable]
    public class GameSlot
    {
        public string gameName;
        public Sprite gameSprite;
        public GameObject gameObj;
    }

    public Sprite lockImage;
    public List<GameSlot> slots = new List<GameSlot>();
    private Dictionary<string, GameSlot> map;
    private HashSet<string> unlocked = new HashSet<string>();

    public BuildManager buildManager; // optional: чтобы можно было автозаполнить иконки на старте

    // Событие — вызывается когда игра была успешно разблокирована (новая)
    public event Action<string> OnGameUnlocked;

    public int storySubmittedCount = 0; // общее число сданных сюжетных дисков
    private int pendingSleepPermits = 0; // сколько раз игрок ещё может поспать (разрешения, полученные сдачей сюжетных дисков)
    public event Action<int> OnStorySubmitted; // передаёт новый count

    private HashSet<string> submittedStoryGames = new HashSet<string>();

    private void Awake()
    {
        map = new Dictionary<string, GameSlot>();
        foreach (var s in slots)
        {
            if (string.IsNullOrEmpty(s.gameName)) continue;
            map[s.gameName] = s;
            //if (s.lockImage != null) s.lockImage.gameObject.SetActive(true);
            s.gameObj.GetComponent<MeshRenderer>().material.mainTexture = lockImage.texture;
        }
    }

    private void Start()
    {
        if (buildManager != null)
        {
            foreach (var combo in buildManager.validCombos)
            {
                if (!string.IsNullOrEmpty(combo.gameName))
                    SetIconFor(combo.gameName, combo.gameIcon);
            }
        }
    }

    // Возвращает true если было новое разблокирование, false если уже было разблокировано или нет слота
    public bool UnlockGame(string gameName)
    {
        if (string.IsNullOrEmpty(gameName)) return false;
        if (unlocked.Contains(gameName)) return false;

        if (!map.TryGetValue(gameName, out var slot))
        {
            Debug.LogWarning($"UnlockGame: нет слота для {gameName}");
            // Даже если слота нет — считаем, что игра "разблокирована" логически, чтобы остальной код мог реагировать.
            unlocked.Add(gameName);
            OnGameUnlocked?.Invoke(gameName);
            return true;
        }

        unlocked.Add(gameName);

        //if (slot.lockImage != null) slot.lockImage.gameObject.SetActive(false);
        if (slot.gameObj != null)
        {
            if (slot.gameSprite != null) slot.gameObj.GetComponent<MeshRenderer>().material.mainTexture = slot.gameSprite.texture;
        }

        OnGameUnlocked?.Invoke(gameName);
        return true;
    }

    // Проверка: разблокирована ли игра
    public bool IsUnlocked(string gameName)
    {
        if (string.IsNullOrEmpty(gameName)) return false;
        return unlocked.Contains(gameName);
    }

    public void SetIconFor(string gameName, Sprite icon)
    {
        if (string.IsNullOrEmpty(gameName)) return;
        if (!map.TryGetValue(gameName, out var slot)) return;
        slot.gameSprite = icon;
        if (slot.gameSprite != null) slot.gameObj.GetComponent<MeshRenderer>().material.mainTexture = slot.gameSprite.texture;
    }

    public bool RecordStoryDiscSubmitted(string gameName)
    {
        if (string.IsNullOrEmpty(gameName)) return false;

        if (submittedStoryGames.Contains(gameName))
        {
            // уже сдали этот сюжетный диск — игнорируем
            Debug.Log($"Story disc '{gameName}' already submitted — ignoring.");
            return false;
        }

        // защита: уже может быть разблокировано — но считаем сданным
        submittedStoryGames.Add(gameName);
        storySubmittedCount++;
        pendingSleepPermits++;
        OnStorySubmitted?.Invoke(storySubmittedCount);

        // Если достигли 6 — конец игры
        if (storySubmittedCount >= 6)
        {
            EndGameManager.Instance?.EndGame(); // см. Singleton EndGameManager ниже
        }

        Debug.Log($"Story disc '{gameName}' recorded. total story submitted: {storySubmittedCount}");
        return true;
    }

    public bool ConsumeSleepPermit()
    {
        if (pendingSleepPermits > 0)
        {
            pendingSleepPermits--;
            return true;
        }
        return false;
}
}
