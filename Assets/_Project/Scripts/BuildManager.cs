using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Build/BuildManager")]
public class BuildManager : ScriptableObject
{
    public List<MechanicCombo> validCombos = new List<MechanicCombo>();

    // возвращает подходящую комбинацию (объект) или null
    public MechanicCombo Evaluate(List<string> current)
    {
        var cur = new List<string>(current);
        cur.RemoveAll(s => string.IsNullOrEmpty(s));

        foreach (var combo in validCombos)
        {
            if (combo.Matches(cur)) return combo;
        }
        return null;
    }

    public MechanicCombo GetComboByGameName(string gameName)
    {
        if (string.IsNullOrEmpty(gameName)) return null;
        foreach (var combo in validCombos)
        {
            if (string.Equals(combo.gameName, gameName, System.StringComparison.Ordinal))
                return combo;
        }
        return null;
    }
}

[System.Serializable]
public class MechanicCombo
{
    public List<string> mechanics = new List<string>();
    public string gameName;
    public Sprite gameIcon;

    // в MechanicCombo (BuildManager)
    [Header("Disc output")]
    public bool isStoryDisc = false;        // является ли собранная игра сюжетным диском
    public Sprite discIcon;                // иконка/спрайт диска (опционально)
    public GameObject discPrefab;          // (опционально) prefab диска, если хочешь разный внешний вид
    public DialogueScript dialogueScript;


    [Header("Unlock a new mechanic when this game is built for the first time")]
    [Tooltip("Имя механики, которое появится в нижней панели")]
    public string unlockMechanicName;
    [Tooltip("Иконка для новой механики (можно оставить пустой — тогда будет назначаться вручную)")]
    public Sprite unlockMechanicIcon;

    public bool Matches(List<string> other)
    {
        if (other == null) return false;
        if (mechanics.Count != other.Count) return false;
        var temp = new List<string>(other);
        foreach (var s in mechanics)
        {
            if (!temp.Remove(s)) return false;
        }
        return temp.Count == 0;
    }
}
