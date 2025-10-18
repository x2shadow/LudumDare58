using System;
using UnityEngine;

public class InteractionIndicatorManager : MonoBehaviour
{
    public static InteractionIndicatorManager Instance { get; private set; }

    // nested suppression counter — поддерживает вложенные вызовы Suppress()
    private int suppressionCount = 0;

    /// <summary>
    /// Вызывается при изменении suppression (true -> скрывать, false -> показывать)
    /// Подписывайтесь, чтобы обновлять UI сразу.
    /// </summary>
    public event Action<bool> OnSuppressionChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this) Destroy(this.gameObject);
        else Instance = this;
    }

    /// <summary>
    /// Пометить, что индикатор должен быть скрыт (внутри можно вызывать несколько раз).
    /// </summary>
    public void Suppress()
    {
        int prev = suppressionCount;
        suppressionCount = Mathf.Max(0, suppressionCount + 1);
        if (prev == 0 && suppressionCount > 0)
        {
            OnSuppressionChanged?.Invoke(true);
        }
    }

    /// <summary>
    /// Отменить одно скрытие. Если всё скрытия сняты — OnSuppressionChanged(false) сработает.
    /// </summary>
    public void Release()
    {
        int prev = suppressionCount;
        suppressionCount = Mathf.Max(0, suppressionCount - 1);
        if (prev > 0 && suppressionCount == 0)
        {
            OnSuppressionChanged?.Invoke(false);
        }
    }

    /// <summary>
    /// Принудительно сбросить все suppression (очищает счётчик).
    /// </summary>
    public void ResetAll()
    {
        bool was = suppressionCount > 0;
        suppressionCount = 0;
        if (was) OnSuppressionChanged?.Invoke(false);
    }

    /// <summary>
    /// true если в данный момент индикатор должен быть скрыт
    /// </summary>
    public bool IsSuppressed => suppressionCount > 0;
}
