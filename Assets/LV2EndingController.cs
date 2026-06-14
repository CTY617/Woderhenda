using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class LV2EndingController : MonoBehaviour
{
    [SerializeField] Image faceImage;
    [SerializeField] Image backImage;
    [SerializeField] RectTransform cardRoot;
    [SerializeField] Button button;
    [SerializeField] float introDuration = 0.5f;
    [SerializeField] float flipDuration = 0.6f;
    [SerializeField] string endingSceneName = "Loading";

    CanvasGroup canvasGroup;
    RectTransform rectTransform;
    Vector3 targetScale;
    bool isFlipped;
    bool isAnimating;

    void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();

        rectTransform = transform as RectTransform;
        targetScale = rectTransform.localScale;
        EnsureCardRoot();

        if (faceImage != null)
            faceImage.gameObject.SetActive(true);
        if (backImage != null)
            backImage.gameObject.SetActive(false);
        if (cardRoot != null)
            cardRoot.localRotation = Quaternion.identity;
        ApplyCardSideRotations();

        if (button != null)
            button.onClick.AddListener(OnButtonClick);
    }

    void EnsureCardRoot()
    {
        if (cardRoot != null || faceImage == null || backImage == null)
            return;

        var rootGo = new GameObject("CardRoot", typeof(RectTransform));
        cardRoot = rootGo.GetComponent<RectTransform>();
        RectTransform faceRect = faceImage.rectTransform;
        cardRoot.SetParent(transform, false);
        cardRoot.anchorMin = faceRect.anchorMin;
        cardRoot.anchorMax = faceRect.anchorMax;
        cardRoot.anchoredPosition = faceRect.anchoredPosition;
        cardRoot.sizeDelta = faceRect.sizeDelta;
        cardRoot.pivot = faceRect.pivot;
        cardRoot.SetSiblingIndex(faceRect.GetSiblingIndex());

        faceRect.SetParent(cardRoot, true);
        backImage.rectTransform.SetParent(cardRoot, true);
        ApplyCardSideRotations();
    }

    void ApplyCardSideRotations()
    {
        if (faceImage != null)
            faceImage.rectTransform.localRotation = Quaternion.identity;
        if (backImage != null)
            backImage.rectTransform.localRotation = Quaternion.Euler(0f, 180f, 0f);
    }

    public void Show()
    {
        gameObject.SetActive(true);
        isFlipped = false;
        isAnimating = true;

        if (faceImage != null)
            faceImage.gameObject.SetActive(true);
        if (backImage != null)
            backImage.gameObject.SetActive(false);
        if (cardRoot != null)
            cardRoot.localRotation = Quaternion.identity;
        ApplyCardSideRotations();
        if (button != null)
            button.interactable = true;

        rectTransform.localScale = Vector3.zero;
        canvasGroup.alpha = 0f;
        StartCoroutine(IntroRoutine());
    }

    IEnumerator IntroRoutine()
    {
        float elapsed = 0f;
        while (elapsed < introDuration)
        {
            elapsed += Time.deltaTime;
            float t = introDuration > 0f ? Mathf.Clamp01(elapsed / introDuration) : 1f;
            float eased = 1f - Mathf.Pow(1f - t, 3f);
            rectTransform.localScale = Vector3.LerpUnclamped(Vector3.zero, targetScale, eased);
            canvasGroup.alpha = eased;
            yield return null;
        }

        rectTransform.localScale = targetScale;
        canvasGroup.alpha = 1f;
        isAnimating = false;
    }

    void OnButtonClick()
    {
        if (isAnimating)
            return;

        if (!isFlipped)
        {
            StartCoroutine(FlipRoutine());
            return;
        }

        if (!string.IsNullOrEmpty(endingSceneName))
            SceneManager.LoadScene(endingSceneName);
    }

    IEnumerator FlipRoutine()
    {
        isAnimating = true;
        if (button != null)
            button.interactable = false;

        float elapsed = 0f;
        bool swapped = false;

        while (elapsed < flipDuration)
        {
            elapsed += Time.deltaTime;
            float t = flipDuration > 0f ? Mathf.Clamp01(elapsed / flipDuration) : 1f;
            float y = Mathf.Lerp(0f, 180f, t);

            if (cardRoot != null)
                cardRoot.localRotation = Quaternion.Euler(0f, y, 0f);

            if (!swapped && t >= 0.5f)
            {
                swapped = true;
                if (faceImage != null)
                    faceImage.gameObject.SetActive(false);
                if (backImage != null)
                    backImage.gameObject.SetActive(true);
            }

            yield return null;
        }

        if (cardRoot != null)
            cardRoot.localRotation = Quaternion.Euler(0f, 180f, 0f);

        isFlipped = true;
        isAnimating = false;
        if (button != null)
            button.interactable = true;
    }

    void OnDestroy()
    {
        if (button != null)
            button.onClick.RemoveListener(OnButtonClick);
    }
}
