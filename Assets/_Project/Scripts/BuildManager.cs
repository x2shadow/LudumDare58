using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Build/BuildManager")]
public class BuildManager : ScriptableObject
{
    [Tooltip("Список корректных комбинаций.")]
    public List<MechanicCombo> validCombos = new List<MechanicCombo>();

    // Возвращает gameName если найдена комбинация, иначе null
    public string Evaluate(List<string> current)
    {
        var cur = new List<string>(current);
        cur.RemoveAll(s => string.IsNullOrEmpty(s));

        foreach (var combo in validCombos)
        {
            if (combo.Matches(cur)) return combo.gameName;
        }
        return null;
    }
}

[System.Serializable]
public class MechanicCombo
{
    [Tooltip("Имена механик, которые составляют комбинацию.")]
    public List<string> mechanics = new List<string>();

    [Tooltip("Уникальный идентификатор / название получаемой игры (используется для коллекции)")]
    public string gameName;

    [Tooltip("Иконка игры, которая появится в коллекции после разблокировки")]
    public Sprite gameIcon;

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
