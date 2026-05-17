using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class LV2 : MonoBehaviour
{
    [Header("References")]
    public List<Image> hairImages = new List<Image>(); // multiple hair images
    public Slider forceSlider; // visualizes simulated force based on hold duration (0..1)
    public Slider progressSlider; // visualizes pull progress (0..1)
    int currentIndex = 0;
    List<Vector3> baseScales = new List<Vector3>();

    [Header("Settings")]
    public float maxHold = 5f; // seconds required to remove hair
    public float maxStretchMultiplier = 2f; // max scale multiplier when fully stretched
    public float recoverTime = 0.5f; // time to animate to final stretch on release
    public float fallDistance = 200f; // pixels to move down when removed
    public float fallDuration = 1f; // total duration of fall+fade
    public float semiAlpha = 0.5f; // intermediate semi-transparent alpha
    public float recoilDuration = 0.15f; // duration of quick recoil (seconds)
    public float recoilMultiplier = 1.3f; // how much to overscale on recoil (relative to base Y)
    public float immediateRecoverTime = 0.08f; // quick recover after release
    public float decayDuration = 2f; // time (seconds) for sliders/hold to decay to zero after release

    Coroutine decayCoroutine = null;

    bool isHolding = false;
    float holdTime = 0f;
    Vector3 baseScale;
    bool removed = false;

    void Start()
    {
        // cache base scales for all hair images
        baseScales.Clear();
        for (int i = 0; i < hairImages.Count; i++)
        {
            var img = hairImages[i];
            if (img != null)
                baseScales.Add(img.rectTransform.localScale);
            else
                baseScales.Add(Vector3.one);
        }
        // set baseScale to first hair if exists
        if (GetCurrentHairImage() != null)
            baseScale = GetCurrentHairImage().rectTransform.localScale;
        if (progressSlider != null)
        {
            progressSlider.maxValue = 1f;
            progressSlider.value = 0f;
        }
        if (forceSlider != null)
        {
            forceSlider.maxValue = 1f;
            forceSlider.value = 0f;
        }
    }

    void Update()
    {
        if (removed) return;

        // keyboard control: space to hold/release
        if (Input.GetKeyDown(KeyCode.Space)) StartHold();
        if (Input.GetKeyUp(KeyCode.Space)) EndHold();

        // always update slider visuals based on current holdTime
        float currentT = Mathf.Clamp01(holdTime / maxHold);
        if (progressSlider != null) progressSlider.value = currentT;
        if (forceSlider != null) forceSlider.value = currentT;

        if (isHolding)
        {
            holdTime += Time.deltaTime;
            float t = Mathf.Clamp01(holdTime / maxHold);
            if (holdTime >= maxHold)
            {
                RemoveHair();
                isHolding = false;
            }
            else
            {
                UpdateHairStretchLive(t);
            }
        }
    }

    void UpdateHairStretchLive(float t)
    {
        Image cur = GetCurrentHairImage();
        if (cur == null) return;
        float sliderVal = (forceSlider != null) ? Mathf.Clamp01(forceSlider.value) : t;
        float multiplier = 1f + (maxStretchMultiplier - 1f) * (sliderVal * t);
        cur.rectTransform.localScale = new Vector3(baseScale.x, baseScale.y * multiplier, baseScale.z);
    }

    public void StartHold()
    {
        if (removed) return;
        if (isHolding) return;
        isHolding = true;
        // stop any ongoing decay when starting a new hold
        if (decayCoroutine != null)
        {
            StopCoroutine(decayCoroutine);
            decayCoroutine = null;
        }
        // do NOT reset holdTime so force can accumulate across presses
        if (GetCurrentHairImage() != null)
            baseScale = GetCurrentHairImage().rectTransform.localScale;
        // sliders reflect current holdTime; no zeroing so user can resume
    }

    public void EndHold()
    {
        if (removed) return;
        if (!isHolding && holdTime <= 0f) return;
        isHolding = false;
        float t = Mathf.Clamp01(holdTime / maxHold);
        if (holdTime >= maxHold)
        {
            RemoveHair();
            return;
        }

        float sliderVal = (forceSlider != null) ? Mathf.Clamp01(forceSlider.value) : t;
        float finalMultiplier = 1f + (maxStretchMultiplier - 1f) * (sliderVal * t);
        StopAllCoroutines();
        // immediate rebound effect: set scale immediately, then quickly recover to base
        Image cur = GetCurrentHairImage();
        if (cur != null)
        {
            cur.rectTransform.localScale = new Vector3(baseScale.x, baseScale.y * finalMultiplier, baseScale.z);
            StartCoroutine(QuickRecover(cur, immediateRecoverTime));
        }
        // do NOT reset holdTime so user can resume building force

        // start decay coroutine to slowly lower holdTime over time
        if (decayDuration > 0f && holdTime > 0f)
        {
            if (decayCoroutine != null) StopCoroutine(decayCoroutine);
            decayCoroutine = StartCoroutine(DecayHold());
        }
    }

    IEnumerator DecayHold()
    {
        float start = holdTime;
        float elapsed = 0f;
        while (elapsed < decayDuration && holdTime > 0f && !removed)
        {
            elapsed += Time.deltaTime;
            float u = Mathf.Clamp01(elapsed / decayDuration);
            holdTime = Mathf.Lerp(start, 0f, u);
            float t = Mathf.Clamp01(holdTime / maxHold);
            if (progressSlider != null) progressSlider.value = t;
            if (forceSlider != null) forceSlider.value = t;
            UpdateHairStretchLive(t);
            yield return null;
        }
        holdTime = 0f;
        if (progressSlider != null) progressSlider.value = 0f;
        if (forceSlider != null) forceSlider.value = 0f;
        // ensure visual reset
        Image cur = GetCurrentHairImage();
        if (cur != null) cur.rectTransform.localScale = baseScale;
        decayCoroutine = null;
    }

    IEnumerator QuickRecover(Image img, float duration)
    {
        if (img == null) yield break;
        float startY = img.rectTransform.localScale.y;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float v = Mathf.Lerp(startY, baseScale.y, elapsed / duration);
            img.rectTransform.localScale = new Vector3(baseScale.x, v, baseScale.z);
            yield return null;
        }
        img.rectTransform.localScale = new Vector3(baseScale.x, baseScale.y, baseScale.z);
    }

    IEnumerator AnimateToScale(float targetY)
    {
        Image cur = GetCurrentHairImage();
        if (cur == null) yield break;
        float startY = cur.rectTransform.localScale.y;
        float elapsed = 0f;
        while (elapsed < recoverTime)
        {
            elapsed += Time.deltaTime;
            float v = Mathf.Lerp(startY, targetY, elapsed / recoverTime);
            cur.rectTransform.localScale = new Vector3(baseScale.x, v, baseScale.z);
            yield return null;
        }
        cur.rectTransform.localScale = new Vector3(baseScale.x, targetY, baseScale.z);
    }

    void RemoveHair()
    {
        if (removed) return;
        removed = true;
        StopAllCoroutines();
        if (decayCoroutine != null)
        {
            StopCoroutine(decayCoroutine);
            decayCoroutine = null;
        }
        if (progressSlider != null) progressSlider.value = 0f;
        if (forceSlider != null) forceSlider.value = 0f;
        StartCoroutine(RemoveHairAnimation());
    }

    IEnumerator RemoveHairAnimation()
    {
        Image cur = GetCurrentHairImage();
        if (cur == null)
        {
            yield break;
        }
        RectTransform rt = cur.rectTransform;
        // --- Recoil phase: quick scale up then back to base ---
        float originalY = rt.localScale.y;
        float targetY = baseScale.y * recoilMultiplier;
        float half = Mathf.Max(0.001f, recoilDuration * 0.5f);

        float e = 0f;
        // scale up
        while (e < half)
        {
            e += Time.deltaTime;
            float u = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(e / half));
            float y = Mathf.Lerp(originalY, targetY, u);
            rt.localScale = new Vector3(baseScale.x, y, baseScale.z);
            yield return null;
        }
        // scale back to base
        e = 0f;
        while (e < half)
        {
            e += Time.deltaTime;
            float u = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(e / half));
            float y = Mathf.Lerp(targetY, baseScale.y, u);
            rt.localScale = new Vector3(baseScale.x, y, baseScale.z);
            yield return null;
        }

        // --- Fall + fade phase ---
        Vector2 startPos = rt.anchoredPosition;
        Vector2 targetPos = startPos + Vector2.down * fallDistance;

        Color startColor = cur.color;
        float startAlpha = startColor.a;

        float elapsed = 0f;
        while (elapsed < fallDuration)
        {
            elapsed += Time.deltaTime;
            float u = Mathf.Clamp01(elapsed / fallDuration);

            // position: move down over full duration
            rt.anchoredPosition = Vector2.Lerp(startPos, targetPos, u);

            // two-phase fade: first half to semiAlpha, second half to transparent
            float alpha;
            if (u < 0.5f)
            {
                alpha = Mathf.Lerp(startAlpha, semiAlpha, u * 2f);
            }
            else
            {
                alpha = Mathf.Lerp(semiAlpha, 0f, (u - 0.5f) * 2f);
            }

            Color c = startColor;
            c.a = alpha;
            cur.color = c;

            yield return null;
        }

        // ensure final state
        rt.anchoredPosition = targetPos;
        Color endColor = startColor; endColor.a = 0f;
        cur.color = endColor;
        cur.gameObject.SetActive(false);

        // move to next hair if any
        currentIndex++;
        if (currentIndex < hairImages.Count)
        {
            // prepare next hair
            removed = false;
            holdTime = 0f;
            Image next = GetCurrentHairImage();
            if (next != null)
            {
                next.gameObject.SetActive(true);
                baseScale = baseScales[Mathf.Clamp(currentIndex, 0, baseScales.Count - 1)];
                next.rectTransform.localScale = baseScale;
                Color c = next.color; c.a = 1f; next.color = c;
            }
        }
    }

    Image GetCurrentHairImage()
    {
        if (currentIndex >= 0 && currentIndex < hairImages.Count)
            return hairImages[currentIndex];
        return null;
    }
}
