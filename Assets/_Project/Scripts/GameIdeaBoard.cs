using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

[DisallowMultipleComponent]
public class GameIdeaBoard : MonoBehaviour
{
    [Serializable]
    public class Idea
    {
        public string title;
        [TextArea(3, 10)]
        public string description;
        [Tooltip("Если задано — эта идея считается 'связанной' с игрой с таким именем")]
        public string unlockGameName;
    }

    [Header("UI References (World Space Canvas)")]
    [SerializeField] private TextMeshProUGUI titleText = null;
    [SerializeField] private TextMeshProUGUI bodyText  = null;

    [Header("Ideas")]
    [SerializeField] private List<Idea> ideas = new List<Idea>();

    [Header("Behaviour")]
    [SerializeField] private bool allowAnyBuild = true;
    [SerializeField] private bool autoNumberTitles = true;
    [SerializeField] private int startIndex = 0;

    [Header("Integration")]
    [Tooltip("Ссылка на GameCollectionManager — используется чтобы пропускать уже разблокированные игры")]
    public GameCollectionManager collectionManager;

    private int currentIndex = 0;

    private void Awake()
    {
        if (titleText == null || bodyText == null)
            Debug.LogWarning($"{nameof(GameIdeaBoard)}: assign titleText and bodyText in inspector.");

        // Подписываемся на событие разблокировки (если доступно)
        if (collectionManager != null)
        {
            collectionManager.OnGameUnlocked += OnExternalGameUnlocked;
        }

        // стартовый индекс: первый неразблокированный (если есть), иначе startIndex
        currentIndex = FindFirstAvailableIndex();
        if (currentIndex == -1) currentIndex = 0; // если всё заблокировано и нет критериев — просто 0
        RefreshUI();
    }

    private void OnDestroy()
    {
        if (collectionManager != null)
            collectionManager.OnGameUnlocked -= OnExternalGameUnlocked;
    }

    // Это вызывается извне, когда билд успешно собран (buildManager возвращает combo.gameName)
    public void OnBuildCompleted(string builtGameName)
    {
        // Всегда синхронизируемся с коллекцией: если игрок разблокировал игру, пропускаем уже показанные
        SyncToCollection();

        // Если текущая идея требует конкретного имени, и имя совпало — продвигаемся
        if (ideas != null && currentIndex >= 0 && currentIndex < ideas.Count)
        {
            var curr = ideas[currentIndex];
            bool shouldAdvance = false;

            if (!string.IsNullOrEmpty(curr.unlockGameName))
            {
                if (string.Equals(curr.unlockGameName, builtGameName, StringComparison.Ordinal))
                    shouldAdvance = true;
            }
            else
            {
                // если требование не задано — опираемся на allowAnyBuild
                shouldAdvance = allowAnyBuild;
            }

            if (shouldAdvance)
            {
                AdvanceToNext();
            }
        }
        else
        {
            // если currentIndex вне диапазона — попробуем синхронизировать и обновить UI
            SyncToCollection();
        }
    }

    // Если кто-то разблокировал игру (через GameCollectionManager), приходим сюда и пересчитываем какую идею показывать
    private void OnExternalGameUnlocked(string gameName)
    {
        // Пересчитать индекс: пропускаем идеи, связанные с уже разблокированными играми
        SyncToCollection();
    }

    // Нахождение первого индекса идеи, которая ещё НЕ ассоциирована с уже разблокированной игрой
    // Возвращает -1 если не найдено (все идеи либо не имеют unlockGameName, либо их unlockGameName уже разблокированы).
    private int FindFirstAvailableIndex()
    {
        if (ideas == null || ideas.Count == 0) return -1;

        for (int i = 0; i < ideas.Count; i++)
        {
            var idea = ideas[i];
            if (string.IsNullOrEmpty(idea.unlockGameName))
            {
                // у идеи нет привязки к имени игры => считаем её показанной/доступной (не пропускаем)
                // Возвращаем её как первая доступная
                return i;
            }
            else
            {
                // есть привязка — проверяем, разблокирована ли соответствующая игра
                if (collectionManager != null)
                {
                    if (collectionManager.IsUnlocked(idea.unlockGameName))
                    {
                        // уже разблокирована — пропускаем
                        continue;
                    }
                    else
                    {
                        // ещё НЕ разблокирована — показываем эту идею
                        return i;
                    }
                }
                else
                {
                    // Нет collectionManager — не можем проверить, поэтому считаем идею доступной
                    return i;
                }
            }
        }

        // ничего не найдено
        return -1;
    }

    // Подгоняем currentIndex к первой непоказанной/неразблокированной идее
    private void SyncToCollection()
    {
        int idx = FindFirstAvailableIndex();
        if (idx == -1)
        {
            // Если ничего не доступно — выставим special state (-1) и RefreshUI займетсь этим случаем.
            currentIndex = -1;
        }
        else
        {
            currentIndex = idx;
        }
        RefreshUI();
    }

    public void AdvanceToNext()
    {
        if (ideas == null || ideas.Count == 0)
        {
            currentIndex = -1;
            RefreshUI();
            return;
        }

        // сдвигаем вперёд пока не найдём первый непоказанный/неразблокированный
        int start = (currentIndex < 0) ? 0 : currentIndex + 1;
        int found = -1;
        for (int i = start; i < ideas.Count; i++)
        {
            var idea = ideas[i];
            if (string.IsNullOrEmpty(idea.unlockGameName))
            {
                found = i;
                break;
            }
            else
            {
                if (collectionManager != null)
                {
                    if (!collectionManager.IsUnlocked(idea.unlockGameName))
                    {
                        found = i;
                        break;
                    }
                }
                else
                {
                    found = i;
                    break;
                }
            }
        }

        if (found == -1)
        {
            // дошли до конца — помечаем, что идей нет
            currentIndex = -1;
        }
        else
        {
            currentIndex = found;
        }

        RefreshUI();
    }

    public void SetIndex(int idx)
    {
        if (ideas == null || ideas.Count == 0) return;
        currentIndex = Mathf.Clamp(idx, 0, ideas.Count - 1);
        RefreshUI();
    }

    private void RefreshUI()
    {
        if (ideas == null || ideas.Count == 0)
        {
            if (titleText != null) titleText.text = "Game Idea ???";
            if (bodyText  != null) bodyText.text  = "???";
            return;
        }

        if (currentIndex < 0 || currentIndex >= ideas.Count)
        {
            // специальный случай — идей больше нет для показа
            if (titleText != null) titleText.text = "Game Idea ???";
            if (bodyText != null) bodyText.text = "- ???\n- ???\n- ???";
            return;
        }

        var curr = ideas[currentIndex];

        if (titleText != null)
        {
            if (autoNumberTitles)
                titleText.text = $"Game Idea {currentIndex + 1}";
            else
                titleText.text = string.IsNullOrEmpty(curr.title) ? $"Game Idea {currentIndex + 1}" : curr.title;
        }

        if (bodyText != null)
            bodyText.text = curr.description ?? "";
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (!Application.isPlaying)
        {
            currentIndex = Mathf.Clamp(startIndex, 0, Math.Max(0, ideas.Count - 1));
            UnityEditor.EditorApplication.delayCall += () =>
            {
                if (this != null) RefreshUI();
            };
        }
    }
#endif
}
