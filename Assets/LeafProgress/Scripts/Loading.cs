using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class Loading : MonoBehaviour
{
    private const int StageCount = 8;
    private const float LeafRotationWiggleDegrees = 5f;

    [Header("UI References")]
    [SerializeField] private Image _fillImage;
    [SerializeField] private LeafStageSlot[] _leafStages = new LeafStageSlot[StageCount];
    [SerializeField] private TextMeshProUGUI _loadingText;

    [Header("Animation")]
    [SerializeField] private float _fillLerpDuration = 0.15f;
    [SerializeField] private float _leafGrowDuration = 0.45f;

    [SerializeField] private AnimationCurve _leafGrowCurve = new AnimationCurve(
        new Keyframe(0f, 0f, 0f, 2.5f),
        new Keyframe(0.7f, 1.08f, 0f, 0f),
        new Keyframe(1f, 1f, -1f, 0f));

    [Header("Loading")]
    [SerializeField] private string _loadingBaseText = "Loading";
    [SerializeField] private int _maxDots = 4;
    [SerializeField] private float _dotInterval = 0.35f;
    [SerializeField] private float _loadingTotalDuration = 8f;

    [Header("Scene Transition")]
    [SerializeField] private string _nextSceneName = "LV2";

    private float _targetProgress;
    private float _displayedProgress;
    private bool[] _unlockedStages;
    private Vector3[] _leafTargetScales;
    private Quaternion[] _leafTargetRotations;

    private Coroutine _fillLerpCoroutine;
    private Coroutine _loadingCoroutine;
    private Coroutine _dotAnimationCoroutine;

    [Serializable]
    public struct LeafStageSlot
    {
        public Image LeafImage;
        [Range(0f, 1f)] public float UnlockThreshold;
    }

    private void Awake()
    {
        EnsureStageCount();
        _unlockedStages = new bool[StageCount];
        _leafTargetScales = new Vector3[StageCount];
        _leafTargetRotations = new Quaternion[StageCount];

        ConfigureFillImage();
        CacheLeafTargets();
        ResetVisualState();
    }

    private void Start()
    {
        _loadingCoroutine = StartCoroutine(LoadingProgressCoroutine());
        _dotAnimationCoroutine = StartCoroutine(DotAnimationCoroutine());
    }

    private void OnDestroy()
    {
        if (_fillLerpCoroutine != null)
        {
            StopCoroutine(_fillLerpCoroutine);
        }

        if (_loadingCoroutine != null)
        {
            StopCoroutine(_loadingCoroutine);
        }

        if (_dotAnimationCoroutine != null)
        {
            StopCoroutine(_dotAnimationCoroutine);
        }
    }

    private void OnValidate()
    {
        if (_leafStages == null || _leafStages.Length != StageCount)
        {
            ResizeLeafStages();
        }

        for (int i = 0; i < StageCount; i++)
        {
            if (_leafStages[i].UnlockThreshold <= 0f)
            {
                _leafStages[i].UnlockThreshold = (i + 1) / (float)StageCount;
            }
        }
    }

    public void SetProgress(float normalizedProgress)
    {
        _targetProgress = Mathf.Clamp01(normalizedProgress);

        if (_fillLerpCoroutine != null)
        {
            StopCoroutine(_fillLerpCoroutine);
        }

        _fillLerpCoroutine = StartCoroutine(LerpFillCoroutine());
        RefreshUnlockedStages(_targetProgress);
    }

    private void ApplyProgressImmediate(float normalizedProgress)
    {
        _targetProgress = Mathf.Clamp01(normalizedProgress);
        _displayedProgress = _targetProgress;

        if (_fillImage != null)
        {
            _fillImage.fillAmount = _displayedProgress;
        }

        RefreshUnlockedStages(_targetProgress);
    }

    public void ResetProgress()
    {
        if (_loadingCoroutine != null)
        {
            StopCoroutine(_loadingCoroutine);
            _loadingCoroutine = null;
        }

        if (_fillLerpCoroutine != null)
        {
            StopCoroutine(_fillLerpCoroutine);
            _fillLerpCoroutine = null;
        }

        _targetProgress = 0f;
        _displayedProgress = 0f;

        if (_fillImage != null)
        {
            _fillImage.fillAmount = 0f;
        }

        for (int i = 0; i < StageCount; i++)
        {
            _unlockedStages[i] = false;
        }

        ResetVisualState();
    }

    private void EnsureStageCount()
    {
        if (_leafStages == null || _leafStages.Length != StageCount)
        {
            ResizeLeafStages();
        }
    }

    private void ResizeLeafStages()
    {
        var resized = new LeafStageSlot[StageCount];
        if (_leafStages != null)
        {
            int copyCount = Mathf.Min(_leafStages.Length, StageCount);
            Array.Copy(_leafStages, resized, copyCount);
        }

        for (int i = 0; i < StageCount; i++)
        {
            if (resized[i].UnlockThreshold <= 0f)
            {
                resized[i].UnlockThreshold = (i + 1) / (float)StageCount;
            }
        }

        _leafStages = resized;
    }

    private void ConfigureFillImage()
    {
        if (_fillImage == null)
        {
            Debug.LogError("Loading: Fill Image is not assigned.");
            return;
        }

        _fillImage.type = Image.Type.Filled;
        _fillImage.fillMethod = Image.FillMethod.Vertical;
        _fillImage.fillOrigin = (int)Image.OriginVertical.Bottom;
        _fillImage.fillAmount = 0f;
    }

    private void CacheLeafTargets()
    {
        for (int i = 0; i < StageCount; i++)
        {
            Image leafImage = _leafStages[i].LeafImage;
            if (leafImage == null)
            {
                continue;
            }

            RectTransform rectTransform = leafImage.rectTransform;
            _leafTargetScales[i] = rectTransform.localScale;
            _leafTargetRotations[i] = rectTransform.localRotation;

            if (_leafTargetScales[i] == Vector3.zero)
            {
                _leafTargetScales[i] = Vector3.one;
            }
        }
    }

    private void ResetVisualState()
    {
        for (int i = 0; i < StageCount; i++)
        {
            Image leafImage = _leafStages[i].LeafImage;
            if (leafImage == null)
            {
                continue;
            }

            RectTransform rectTransform = leafImage.rectTransform;
            rectTransform.localScale = Vector3.zero;
            rectTransform.localRotation = _leafTargetRotations[i];
            leafImage.enabled = true;
        }
    }

    private IEnumerator LerpFillCoroutine()
    {
        float startFill = _displayedProgress;
        float elapsed = 0f;

        while (elapsed < _fillLerpDuration)
        {
            elapsed += Time.deltaTime;
            float t = _fillLerpDuration > 0f ? Mathf.Clamp01(elapsed / _fillLerpDuration) : 1f;
            _displayedProgress = Mathf.Lerp(startFill, _targetProgress, t);

            if (_fillImage != null)
            {
                _fillImage.fillAmount = _displayedProgress;
            }

            yield return null;
        }

        _displayedProgress = _targetProgress;

        if (_fillImage != null)
        {
            _fillImage.fillAmount = _displayedProgress;
        }

        _fillLerpCoroutine = null;
    }

    private void RefreshUnlockedStages(float normalizedProgress)
    {
        for (int i = 0; i < StageCount; i++)
        {
            if (_unlockedStages[i])
            {
                continue;
            }

            if (normalizedProgress < _leafStages[i].UnlockThreshold)
            {
                continue;
            }

            _unlockedStages[i] = true;
            Image leafImage = _leafStages[i].LeafImage;

            if (leafImage != null)
            {
                StartCoroutine(GrowLeafCoroutine(i, leafImage.rectTransform));
            }
        }
    }

    private IEnumerator GrowLeafCoroutine(int stageIndex, RectTransform leafTransform)
    {
        Vector3 targetScale = _leafTargetScales[stageIndex];
        Quaternion targetRotation = _leafTargetRotations[stageIndex];
        Quaternion startRotation = targetRotation * Quaternion.Euler(0f, 0f, -LeafRotationWiggleDegrees);
        leafTransform.localScale = Vector3.zero;
        leafTransform.localRotation = startRotation;

        float elapsed = 0f;

        while (elapsed < _leafGrowDuration)
        {
            elapsed += Time.deltaTime;
            float t = _leafGrowDuration > 0f ? Mathf.Clamp01(elapsed / _leafGrowDuration) : 1f;
            float curveT = _leafGrowCurve != null ? _leafGrowCurve.Evaluate(t) : t;

            leafTransform.localScale = Vector3.LerpUnclamped(Vector3.zero, targetScale, curveT);
            leafTransform.localRotation = Quaternion.Slerp(startRotation, targetRotation, curveT);
            yield return null;
        }

        leafTransform.localScale = targetScale;
        leafTransform.localRotation = targetRotation;
    }

    private IEnumerator DotAnimationCoroutine()
    {
        if (_loadingText == null)
        {
            yield break;
        }

        int dotCount = 0;

        while (true)
        {
            dotCount = dotCount >= _maxDots ? 1 : dotCount + 1;
            _loadingText.text = _loadingBaseText + new string('.', dotCount);
            yield return new WaitForSeconds(_dotInterval);
        }
    }

    private IEnumerator LoadingProgressCoroutine()
    {
        if (_fillLerpCoroutine != null)
        {
            StopCoroutine(_fillLerpCoroutine);
            _fillLerpCoroutine = null;
        }

        _targetProgress = 0f;
        _displayedProgress = 0f;

        if (_fillImage != null)
        {
            _fillImage.fillAmount = 0f;
        }

        for (int i = 0; i < StageCount; i++)
        {
            _unlockedStages[i] = false;
        }

        ResetVisualState();

        float elapsed = 0f;

        while (elapsed < _loadingTotalDuration)
        {
            elapsed += Time.deltaTime;
            float normalized = _loadingTotalDuration > 0f ? Mathf.Clamp01(elapsed / _loadingTotalDuration) : 1f;
            ApplyProgressImmediate(normalized);
            yield return null;
        }

        ApplyProgressImmediate(1f);

        if (!string.IsNullOrEmpty(_nextSceneName))
        {
            SceneManager.LoadScene(_nextSceneName);
        }
    }
}
