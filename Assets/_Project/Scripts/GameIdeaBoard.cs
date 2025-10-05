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
        [Tooltip("Если оставить пустым — заголовок будет генерироваться как 'Game Idea N'")]
        public string title;

        [TextArea(3, 10)]
        public string description;

        [Tooltip("Если указано — эта идея переключится на следующую только когда пользователь соберёт игру с этим именем.\nОставьте пустым, чтобы любое успешное создание билда переключало идею (если allowAnyBuild=true).")]
        public string unlockGameName;
    }

    [Header("UI References (World Space Canvas)")]
    [SerializeField] private TextMeshProUGUI titleText = null;
    [SerializeField] private TextMeshProUGUI bodyText  = null;

    [Header("Ideas")]
    [SerializeField] private List<Idea> ideas = new List<Idea>();

    [Header("Behaviour")]
    [Tooltip("Если true — любое успешное создание билда переключает на следующую идею (если unlockGameName не задан).")]
    [SerializeField] private bool allowAnyBuild = true;

    [Tooltip("Если true — заголовки будут 'Game Idea 1/2/3' независимо от поля title в Idea.")]
    [SerializeField] private bool autoNumberTitles = true;

    [SerializeField] private int startIndex = 0; // для дебага/настроек

    private int currentIndex = 0;

    private void Awake()
    {
        // Ensure TMP refs are set
        if (titleText == null || bodyText == null)
            Debug.LogWarning($"{nameof(GameIdeaBoard)}: assign titleText and bodyText in inspector.");

        currentIndex = Mathf.Clamp(startIndex, 0, Math.Max(0, ideas.Count - 1));
        RefreshUI();
    }

    /// <summary>
    /// Должно вызываться, когда билд успешно собран.
    /// builtGameName — название игры, которое возвращает BuildManager (MechanicCombo.gameName).
    /// </summary>
    public void OnBuildCompleted(string builtGameName)
    {
        if (ideas == null || ideas.Count == 0) return;

        // Если текущая идея имеет требование unlockGameName — сравниваем
        var curr = ideas[currentIndex];
        bool shouldAdvance = false;

        if (!string.IsNullOrEmpty(curr.unlockGameName))
        {
            if (string.Equals(curr.unlockGameName, builtGameName, StringComparison.Ordinal))
                shouldAdvance = true;
        }
        else
        {
            // если требование не задано — полагаемся на allowAnyBuild
            shouldAdvance = allowAnyBuild;
        }

        if (shouldAdvance)
        {
            AdvanceToNext();
        }
    }

    /// <summary>Перейти к следующей идее (если есть)</summary>
    public void AdvanceToNext()
    {
        if (ideas == null || ideas.Count == 0) return;
        if (currentIndex < ideas.Count - 1)
        {
            currentIndex++;
            RefreshUI();
            // тут можно добавить эффект (звук/анимация) при переключении
        }
        else
        {
            // опционально: если дошли до конца — ничего не делаем или сбрасываем
            Debug.Log($"{name}: reached last idea (index {currentIndex}).");
        }
    }

    /// <summary>Принудительно установить индекс (например для редактора)</summary>
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
            if (titleText != null) titleText.text = "No Game Ideas";
            if (bodyText  != null) bodyText.text  = "";
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
    // для удобства: показать текущую идею прямо в редакторе при изменении
    private void OnValidate()
    {
        if (!Application.isPlaying)
        {
            currentIndex = Mathf.Clamp(startIndex, 0, Math.Max(0, ideas.Count - 1));
            // Delay refresh to avoid errors when recompiling; use EditorApplication.delayCall if needed.
            UnityEditor.EditorApplication.delayCall += () =>
            {
                if (this != null) RefreshUI();
            };
        }
    }
#endif
}
