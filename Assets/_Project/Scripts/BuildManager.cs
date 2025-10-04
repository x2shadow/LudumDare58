using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Build/BuildManager")]
public class BuildManager : ScriptableObject
{
    [Tooltip("Список корректных комбинаций. Каждая комбинация — список названий механик (order-insensitive)")]
    public List<MechanicCombo> validCombos = new List<MechanicCombo>();

    // Простая проверка: вернёт true если текущий список совпадает с любой комбинацией (порядок не важен)
    public bool Evaluate(List<string> current)
    {
        // Normalize
        var cur = new List<string>(current);
        cur.RemoveAll(s => string.IsNullOrEmpty(s));

        foreach (var combo in validCombos)
        {
            if (combo.Matches(cur)) return true;
        }
        return false;
    }
}

[System.Serializable]
public class MechanicCombo
{
    public List<string> mechanics = new List<string>();

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
