using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CountdownSlider : MonoBehaviour
{
    [Header("UI")]
    public Slider slider;
    public TextMeshProUGUI timeText;

    [Header("Countdown Settings")]
    [Tooltip("Total number of countdowns. If durations list is shorter, it will cycle.")]
    public int repeatCount = 3;
    [Tooltip("Seconds for each countdown. Edit in inspector to set different durations.")]
    public List<float> durations = new List<float>() { 5f, 3f, 7f };
    [Tooltip("Fallback duration when list is empty.")]
    public float defaultDuration = 5f;
    public bool autoStart = false;

    int currentRepeat = 0;
    float timer = 0f;
    bool running = false;

    void Start()
    {
        if (autoStart)
            StartCountdown();
        UpdateUIIdle();
    }

    public void StartCountdown()
    {
        if (running) return;
        StartCoroutine(RunCountdowns());
    }

    public void StopCountdown()
    {
        StopAllCoroutines();
        running = false;
        UpdateUIIdle();
    }

    IEnumerator RunCountdowns()
    {
        running = true;
        currentRepeat = 0;

        while (currentRepeat < repeatCount)
        {
            float duration = GetDurationForIndex(currentRepeat);
            timer = duration;
            if (slider) slider.maxValue = duration;

            while (timer > 0f)
            {
                timer -= UnityEngine.Time.deltaTime;
                if (slider) slider.value = Mathf.Max(0f, timer);
                if (timeText) timeText.text = Mathf.Ceil(Mathf.Max(0f, timer)).ToString();
                yield return null;
            }

            if (slider) slider.value = 0f;
            if (timeText) timeText.text = "0";

            currentRepeat++;
            yield return null;
        }

        running = false;
        UpdateUIIdle();
    }

    float GetDurationForIndex(int idx)
    {
        if (durations != null && durations.Count > 0)
        {
            if (idx < durations.Count) return Mathf.Max(0f, durations[idx]);
            return Mathf.Max(0f, durations[idx % durations.Count]);
        }
        return Mathf.Max(0f, defaultDuration);
    }

    void UpdateUIIdle()
    {
        if (slider)
        {
            slider.maxValue = (durations != null && durations.Count > 0) ? Mathf.Max(0.01f, durations[0]) : Mathf.Max(0.01f, defaultDuration);
            slider.value = 0f;
        }
        if (timeText) timeText.text = "--";
    }

    // Inspector / UI helpers
    public void SetRepeatCount(int n)
    {
        repeatCount = Mathf.Max(0, n);
    }

    public void SetDurationAt(int index, float seconds)
    {
        if (index < 0) return;
        while (durations.Count <= index) durations.Add(defaultDuration);
        durations[index] = Mathf.Max(0f, seconds);
    }
}
