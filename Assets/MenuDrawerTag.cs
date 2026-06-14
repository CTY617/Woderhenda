using System.Collections;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
[RequireComponent(typeof(Image))]
public class MenuDrawerTag : MonoBehaviour
{
    public static readonly Color FocusedColor = new Color(0.40392157f, 0.7411765f, 0.94509804f);
    public static readonly Color UnfocusedColor = new Color(0.23137255f, 0.62352943f, 0.8509804f);

    [SerializeField] float focusScaleMultiplier = 1.16f;
    [SerializeField] float focusLeftOffset = 42f;
    [SerializeField] float focusOvershoot = 1.08f;
    [SerializeField] float transitionDuration = 0.28f;
    [SerializeField] float unfocusDuration = 0.18f;

    RectTransform rectTransform;
    Image image;
    Button button;
    Vector2 restAnchoredPosition;
    Vector3 restScale;
    Coroutine transitionCoroutine;
    int tabIndex = -1;
    MenuController controller;

    public int TabIndex => tabIndex;

    void Awake()
    {
        EnsureInitialized();
    }

    void EnsureInitialized()
    {
        if (button != null)
            return;

        rectTransform = transform as RectTransform;
        image = GetComponent<Image>();
        button = GetComponent<Button>();
        restAnchoredPosition = rectTransform.anchoredPosition;
        restScale = rectTransform.localScale;

        button.transition = Selectable.Transition.None;
        button.targetGraphic = image;
    }

    public void Bind(MenuController owner, int index)
    {
        EnsureInitialized();
        controller = owner;
        tabIndex = index;
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(OnClicked);
    }

    void OnClicked()
    {
        if (controller != null)
            controller.Select(tabIndex);
    }

    public void SetFocused(bool focused, bool animate)
    {
        EnsureInitialized();
        Vector3 targetScale = focused ? restScale * focusScaleMultiplier : restScale;
        Vector2 targetPosition = focused
            ? restAnchoredPosition + Vector2.left * focusLeftOffset
            : restAnchoredPosition;
        Color targetColor = focused ? FocusedColor : UnfocusedColor;

        if (transitionCoroutine != null)
        {
            StopCoroutine(transitionCoroutine);
            transitionCoroutine = null;
        }

        if (!animate || transitionDuration <= 0f)
        {
            rectTransform.localScale = targetScale;
            rectTransform.anchoredPosition = targetPosition;
            image.color = targetColor;
            return;
        }

        transitionCoroutine = StartCoroutine(TransitionRoutine(focused, targetScale, targetPosition, targetColor));
    }

    IEnumerator TransitionRoutine(bool focusing, Vector3 targetScale, Vector2 targetPosition, Color targetColor)
    {
        if (focusing && focusOvershoot > 1f)
        {
            Vector3 overshootScale = targetScale * focusOvershoot;
            yield return AnimateTo(overshootScale, targetPosition, targetColor, transitionDuration * 0.55f, true);
            yield return AnimateTo(targetScale, targetPosition, targetColor, transitionDuration * 0.45f, false);
        }
        else
        {
            float duration = focusing ? transitionDuration : unfocusDuration;
            yield return AnimateTo(targetScale, targetPosition, targetColor, duration, !focusing);
        }

        transitionCoroutine = null;
    }

    IEnumerator AnimateTo(Vector3 targetScale, Vector2 targetPosition, Color targetColor, float duration, bool easeOutBack)
    {
        Vector3 startScale = rectTransform.localScale;
        Vector2 startPosition = rectTransform.anchoredPosition;
        Color startColor = image.color;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = duration > 0f ? Mathf.Clamp01(elapsed / duration) : 1f;
            float eased = easeOutBack ? EaseOutBack(t) : EaseOutCubic(t);
            rectTransform.localScale = Vector3.LerpUnclamped(startScale, targetScale, eased);
            rectTransform.anchoredPosition = Vector2.LerpUnclamped(startPosition, targetPosition, eased);
            image.color = Color.LerpUnclamped(startColor, targetColor, eased);
            yield return null;
        }

        rectTransform.localScale = targetScale;
        rectTransform.anchoredPosition = targetPosition;
        image.color = targetColor;
    }

    static float EaseOutCubic(float t)
    {
        return 1f - Mathf.Pow(1f - t, 3f);
    }

    static float EaseOutBack(float t)
    {
        const float c1 = 1.70158f;
        const float c3 = c1 + 1f;
        return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
    }

    void OnDestroy()
    {
        if (button != null)
            button.onClick.RemoveAllListeners();
    }
}
