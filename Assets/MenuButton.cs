using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(Button))]
public class MenuButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Hover")]
    public float hoverScaleMultiplier = 1.08f;
    public float hoverScaleDuration = 0.12f;

    [Header("Press")]
    public float pressScaleDownMultiplier = 0.92f;
    public float pressScaleDownDuration = 0.06f;
    public float scaleMultiplier = 1.25f;
    public float scaleUpDuration = 0.2f;
    public float holdDuration = 0.5f;

    [Header("Bounce")]
    public float bounceOvershoot = 1.08f;
    public float bounceSettleDuration = 0.12f;

    [Header("Transition")]
    public GameObject activateOnComplete;
    public string nextSceneName = "LV2";

    Button button;
    RectTransform rectTransform;
    Vector3 originalScale;
    bool isAnimating;
    bool overlayLocked;
    Coroutine scaleCoroutine;

    void Awake()
    {
        button = GetComponent<Button>();
        rectTransform = transform as RectTransform;
        originalScale = rectTransform.localScale;

        SetOverlayVisible(false);
    }

    void Start()
    {
        button.onClick.AddListener(OnClick);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (isAnimating || !button.interactable)
            return;

        SetOverlayVisible(true);
        StartScale(originalScale * hoverScaleMultiplier, hoverScaleDuration);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (isAnimating)
            return;

        if (!overlayLocked)
            SetOverlayVisible(false);

        StartScale(originalScale, hoverScaleDuration);
    }

    void OnClick()
    {
        if (isAnimating)
            return;

        StartCoroutine(ClickSequence());
    }

    IEnumerator ClickSequence()
    {
        isAnimating = true;
        button.interactable = false;
        overlayLocked = true;
        StopScaleCoroutine();
        SetOverlayVisible(true);

        yield return ScaleTo(originalScale * pressScaleDownMultiplier, pressScaleDownDuration);
        yield return BounceTo(originalScale * scaleMultiplier);
        yield return new WaitForSeconds(holdDuration);

        if (!string.IsNullOrEmpty(nextSceneName))
            SceneManager.LoadScene(nextSceneName);
    }

    void SetOverlayVisible(bool visible)
    {
        if (activateOnComplete == null)
            return;

        if (!visible && overlayLocked)
            return;

        activateOnComplete.SetActive(visible);
    }

    IEnumerator BounceTo(Vector3 targetScale)
    {
        Vector3 overshoot = targetScale * bounceOvershoot;
        float upDuration = scaleUpDuration * 0.55f;
        float downDuration = scaleUpDuration * 0.25f;

        yield return ScaleTo(overshoot, upDuration);
        yield return ScaleTo(targetScale * 0.96f, downDuration);
        yield return ScaleTo(targetScale, bounceSettleDuration);
    }

    void StartScale(Vector3 target, float duration)
    {
        StopScaleCoroutine();
        scaleCoroutine = StartCoroutine(ScaleTo(target, duration));
    }

    void StopScaleCoroutine()
    {
        if (scaleCoroutine == null)
            return;

        StopCoroutine(scaleCoroutine);
        scaleCoroutine = null;
    }

    IEnumerator ScaleTo(Vector3 target, float duration)
    {
        Vector3 start = rectTransform.localScale;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = duration > 0f ? Mathf.Clamp01(elapsed / duration) : 1f;
            rectTransform.localScale = Vector3.Lerp(start, target, t);
            yield return null;
        }

        rectTransform.localScale = target;
        scaleCoroutine = null;
    }

    void OnDestroy()
    {
        if (button != null)
            button.onClick.RemoveListener(OnClick);
    }
}
