using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class FadeManager : MonoBehaviour
{
    public static FadeManager Instance;

    public Image fadeImage;
    public float fadeDuration = 0.3f;
    public float afterFadeOutDelay = 0.05f;

    Coroutine fadeRoutine;

    void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        // 🔥 กันจอดำตอนเริ่ม
        if (fadeImage)
        {
            Color c = fadeImage.color;
            fadeImage.color = new Color(c.r, c.g, c.b, 0f);
        }
    }

    public IEnumerator FadeOut()
    {
        if (!fadeImage)
        {
            Debug.LogError("[FadeManager] fadeImage = null");
            yield break;
        }

        // 🔥 หยุด Fade เก่าทุกครั้ง
        if (fadeRoutine != null)
            StopCoroutine(fadeRoutine);

        fadeRoutine = StartCoroutine(Fade(0f, 1f));
        yield return fadeRoutine;
    }

    public IEnumerator FadeIn()
    {
        if (!fadeImage)
        {
            Debug.LogError("[FadeManager] fadeImage = null");
            yield break;
        }

        if (fadeRoutine != null)
            StopCoroutine(fadeRoutine);

        fadeRoutine = StartCoroutine(Fade(1f, 0f));
        yield return fadeRoutine;
    }

    IEnumerator Fade(float from, float to)
    {
        float t = 0f;
        Color c = fadeImage.color;

        // 🔥 บังคับค่าเริ่ม
        fadeImage.color = new Color(c.r, c.g, c.b, from);

        yield return null; // ให้ UI update 1 frame

        while (t < fadeDuration)
        {
            t += Time.unscaledDeltaTime;
            float a = Mathf.Lerp(from, to, t / fadeDuration);
            fadeImage.color = new Color(c.r, c.g, c.b, a);
            yield return null;
        }

        fadeImage.color = new Color(c.r, c.g, c.b, to);
    }
}
