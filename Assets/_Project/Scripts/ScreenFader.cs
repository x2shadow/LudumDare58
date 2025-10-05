using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class ScreenFader : MonoBehaviour
{
    public Image fadeImage;
    public float fadeSpeed = 1f;
    
    private void Awake()
    {
        // Автоматически находим Image если не назначен
        if (fadeImage == null)
            fadeImage = GetComponentInChildren<Image>();
    }
    
    public IEnumerator FadeIn(float duration)
    {
        float elapsed = 0f;
        Color color = fadeImage.color;
        
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            color.a = Mathf.Clamp01(elapsed / duration);
            fadeImage.color = color;
            yield return null;
        }
        
        color.a = 1f;
        fadeImage.color = color;
    }
    
    public IEnumerator FadeOut(float duration)
    {
        float elapsed = 0f;
        Color color = fadeImage.color;
        
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            color.a = 1f - Mathf.Clamp01(elapsed / duration);
            fadeImage.color = color;
            yield return null;
        }
        
        color.a = 0f;
        fadeImage.color = color;
    }
}