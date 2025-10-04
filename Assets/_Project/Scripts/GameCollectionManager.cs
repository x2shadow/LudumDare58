using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GameCollectionManager : MonoBehaviour
{
    [System.Serializable]
    public class GameSlot
    {
        public string gameName;    // должен совпадать с MechanicCombo.gameName
        public Image lockImage;    // картинка замка (показывать/скрывать)
        public Image gameIconImage;// изображение для иконки игры (под замком)
        public Sprite gameSprite;  // спрайт готовой игры (назначаем в инспекторе либо из combo)
    }

    public List<GameSlot> slots = new List<GameSlot>();

    private Dictionary<string, GameSlot> map;

    private void Awake()
    {
        map = new Dictionary<string, GameSlot>();
        foreach (var s in slots)
        {
            if (string.IsNullOrEmpty(s.gameName)) continue;
            map[s.gameName] = s;

            // Изначально показываем lock, скрываем иконку
            if (s.lockImage != null) s.lockImage.gameObject.SetActive(true);
            if (s.gameIconImage != null)
            {
                s.gameIconImage.gameObject.SetActive(false);
                if (s.gameSprite != null) s.gameIconImage.sprite = s.gameSprite;
            }
        }
    }

    // Разблокировать игру по имени — удаляем замок и показываем иконку
    public void UnlockGame(string gameName)
    {
        if (string.IsNullOrEmpty(gameName)) return;
        if (!map.TryGetValue(gameName, out var slot))
        {
            Debug.LogWarning($"GameCollectionManager.UnlockGame: no slot for '{gameName}'");
            return;
        }

        if (slot.lockImage != null) slot.lockImage.gameObject.SetActive(false);

        if (slot.gameIconImage != null)
        {
            if (slot.gameSprite != null) slot.gameIconImage.sprite = slot.gameSprite;
            slot.gameIconImage.gameObject.SetActive(true);
        }
    }

    // Для удобства — можно программно добавить/обновить иконку (например, брать из BuildManager.MechCombo.gameIcon)
    public void SetIconFor(string gameName, Sprite icon)
    {
        if (string.IsNullOrEmpty(gameName)) return;
        if (!map.TryGetValue(gameName, out var slot)) return;
        slot.gameSprite = icon;
        if (slot.gameIconImage != null)
        {
            slot.gameIconImage.sprite = icon;
            slot.gameIconImage.gameObject.SetActive(false); // пока что остаётся заблокированной
        }
    }
}
