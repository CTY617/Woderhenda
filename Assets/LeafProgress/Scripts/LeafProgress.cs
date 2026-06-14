using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 藤蔓進度條控制器：中央 Fill 進度條隨進度填充，達到各階段門檻時播放葉子生長動畫。
/// </summary>
public class LeafProgress : MonoBehaviour
{
    // 葉子階段總數（葉子.png + 葉子-1 ~ 葉子-7）
    private const int StageCount = 8;

    // 葉子長出時的初始旋轉偏移，製造彈出感
    private const float LeafRotationWiggleDegrees = 5f;

    [Header("UI References")]
    [SerializeField] private Image _fillImage;
    [SerializeField] private LeafStageSlot[] _leafStages = new LeafStageSlot[StageCount];

    [Header("Animation")]
    [SerializeField] private float _fillLerpDuration = 0.15f;
    [SerializeField] private float _leafGrowDuration = 0.45f;

    // 葉子縮放曲線，略為超調後回彈以模擬長出效果
    [SerializeField] private AnimationCurve _leafGrowCurve = new AnimationCurve(
        new Keyframe(0f, 0f, 0f, 2.5f),
        new Keyframe(0.7f, 1.08f, 0f, 0f),
        new Keyframe(1f, 1f, -1f, 0f));

    [Header("Auto Demo")]
    [SerializeField] private bool _playAutoDemoOnStart = true;
    [SerializeField] private float _demoTotalDuration = 8f;
    [SerializeField] private bool _demoLoop;

    // 目標進度（0~1），由 SetProgress 設定
    private float _targetProgress;

    // 目前顯示於 Fill Image 的進度值
    private float _displayedProgress;

    // 各階段葉子是否已解鎖，避免重複播放生長動畫
    private bool[] _unlockedStages;

    // 各葉子在場景中預設的目標縮放，Awake 時快取
    private Vector3[] _leafTargetScales;

    // 各葉子在場景中預設的目標旋轉，Awake 時快取
    private Quaternion[] _leafTargetRotations;

    private Coroutine _fillLerpCoroutine;
    private Coroutine _autoDemoCoroutine;

    /// <summary>單一進度階段：對應葉子 Image 與解鎖門檻。</summary>
    [Serializable]
    public struct LeafStageSlot
    {
        /// <summary>此階段的葉子 UI Image。</summary>
        public Image LeafImage;

        /// <summary>解鎖門檻（0~1），進度達此值時觸發生長動畫。</summary>
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
        if (_playAutoDemoOnStart)
        {
            _autoDemoCoroutine = StartCoroutine(AutoDemoCoroutine());
        }
    }

    private void OnDestroy()
    {
        if (_fillLerpCoroutine != null)
        {
            StopCoroutine(_fillLerpCoroutine);
        }

        if (_autoDemoCoroutine != null)
        {
            StopCoroutine(_autoDemoCoroutine);
        }
    }

    // Inspector 變更時自動補齊階段陣列長度與預設門檻
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

    /// <summary>
    /// 設定正規化進度（0~1），Fill 條平滑過渡並檢查葉子階段解鎖。
    /// </summary>
    /// <param name="normalizedProgress">0 為空，1 為滿。</param>
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

    // 立即套用進度，不經 Fill 插值；自動演示每幀呼叫，避免重啟 Lerp Coroutine
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

    /// <summary>
    /// 重置進度與所有葉子至初始隱藏狀態，並停止進行中的 Coroutine。
    /// </summary>
    public void ResetProgress()
    {
        if (_autoDemoCoroutine != null)
        {
            StopCoroutine(_autoDemoCoroutine);
            _autoDemoCoroutine = null;
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

    // 確保葉子階段陣列長度固定為 StageCount
    private void EnsureStageCount()
    {
        if (_leafStages == null || _leafStages.Length != StageCount)
        {
            ResizeLeafStages();
        }
    }

    // 調整陣列大小並保留既有設定，補上預設解鎖門檻
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

    // 設定 Fill Image 為自下而上的垂直填充模式
    private void ConfigureFillImage()
    {
        if (_fillImage == null)
        {
            Debug.LogError("LeafProgress: Fill Image is not assigned.");
            return;
        }

        _fillImage.type = Image.Type.Filled;
        _fillImage.fillMethod = Image.FillMethod.Vertical;
        _fillImage.fillOrigin = (int)Image.OriginVertical.Bottom;
        _fillImage.fillAmount = 0f;
    }

    // 在 ResetVisualState 將縮放歸零前，快取各葉子的設計尺寸與旋轉
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

    // 將所有葉子隱藏（縮放為 0），等待進度解鎖後再生長
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

    // 平滑插值 Fill Image 的 fillAmount 至目標進度
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

    // 檢查進度是否跨越各階段門檻，首次達標時啟動葉子生長動畫
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

    // 播放單片葉子的縮放與旋轉生長動畫
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

    // 自動演示：在指定時間內將進度由 0 推至 1，可選循環播放
    private IEnumerator AutoDemoCoroutine()
    {
        do
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

            while (elapsed < _demoTotalDuration)
            {
                elapsed += Time.deltaTime;
                float normalized = _demoTotalDuration > 0f ? Mathf.Clamp01(elapsed / _demoTotalDuration) : 1f;
                ApplyProgressImmediate(normalized);
                yield return null;
            }

            ApplyProgressImmediate(1f);
            yield return new WaitForSeconds(0.75f);
        }
        while (_demoLoop);
    }
}
