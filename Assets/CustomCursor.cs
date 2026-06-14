using UnityEngine;
using UnityEngine.UI;

public class CustomCursor : MonoBehaviour
{
    static CustomCursor instance;

    public Sprite defaultSprite;
    public Sprite clickSprite;

    Image image;
    RectTransform rectTransform;
    Canvas canvas;

    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        Cursor.visible = false;
        EnsurePersistentCanvas();
        SetupImage();
    }

    void EnsurePersistentCanvas()
    {
        canvas = GetComponentInParent<Canvas>();
        if (canvas != null && canvas.gameObject.scene.name == "DontDestroyOnLoad")
            return;

        GameObject canvasGo = new GameObject("CustomCursorCanvas");
        canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = short.MaxValue;

        CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        transform.SetParent(canvasGo.transform, false);
        DontDestroyOnLoad(canvasGo);
    }

    void SetupImage()
    {
        image = GetComponent<Image>();
        if (image == null)
            image = gameObject.AddComponent<Image>();

        rectTransform = transform as RectTransform;
        canvas = GetComponentInParent<Canvas>();
    }

    void Start()
    {
        image.raycastTarget = false;
        image.sprite = defaultSprite;
    }

    void Update()
    {
        Cursor.visible = false;
        UpdatePosition();

        if (Input.GetMouseButtonDown(0))
            image.sprite = clickSprite;
        else if (Input.GetMouseButtonUp(0))
            image.sprite = defaultSprite;
    }

    void UpdatePosition()
    {
        if (canvas == null)
        {
            transform.position = Input.mousePosition;
            return;
        }

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvas.transform as RectTransform,
            Input.mousePosition,
            canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera,
            out Vector2 localPoint);

        rectTransform.localPosition = localPoint;
    }

    void OnDestroy()
    {
        if (instance != this)
            return;

        instance = null;
        Cursor.visible = true;
    }
}
