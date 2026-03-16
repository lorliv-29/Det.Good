using System.Collections;
using TMPro;
using UnityEngine;

public class FadeInTMPText : MonoBehaviour
{
    public float fadeDuration = 3f;

    TMP_Text textComponent;
    Coroutine fadeRoutine;

    void Awake()
    {
        textComponent = GetComponent<TMP_Text>();
    }

    void OnEnable()
    {
        if (textComponent == null)
            return;

        Color c = textComponent.color;
        c.a = 0f;
        textComponent.color = c;

        if (fadeRoutine != null)
            StopCoroutine(fadeRoutine);

        fadeRoutine = StartCoroutine(FadeInRoutine());
    }

    IEnumerator FadeInRoutine()
    {
        float t = 0f;
        Color c = textComponent.color;

        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            c.a = Mathf.Clamp01(t / fadeDuration);
            textComponent.color = c;
            yield return null;
        }

        c.a = 1f;
        textComponent.color = c;
    }
}