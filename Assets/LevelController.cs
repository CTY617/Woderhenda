using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class LevelController : MonoBehaviour
{
    [SerializeField] Image image;
    [SerializeField] float moveDistance = 200f;
    [SerializeField] float duration = 1f;

    void Start()
    {
        if (image == null)
            return;

        StartCoroutine(ExitRoutine());
    }

    IEnumerator ExitRoutine()
    {
        RectTransform rectTransform = image.rectTransform;
        Vector2 startPosition = rectTransform.anchoredPosition;
        Vector2 targetPosition = startPosition + Vector2.right * moveDistance;
        Color startColor = image.color;
        float startAlpha = startColor.a;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = duration > 0f ? Mathf.Clamp01(elapsed / duration) : 1f;
            float eased = 1f - Mathf.Pow(1f - t, 3f);

            rectTransform.anchoredPosition = Vector2.LerpUnclamped(startPosition, targetPosition, eased);

            Color c = startColor;
            c.a = Mathf.Lerp(startAlpha, 0f, eased);
            image.color = c;

            yield return null;
        }

        rectTransform.anchoredPosition = targetPosition;
        Color endColor = startColor;
        endColor.a = 0f;
        image.color = endColor;
        image.gameObject.SetActive(false);
    }
}
