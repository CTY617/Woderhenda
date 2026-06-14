using UnityEngine;

public class LoopMovement : MonoBehaviour
{
    public enum Direction
    {
        Up,
        Down,
        Left,
        Right
    }

    [SerializeField] Direction direction = Direction.Up;
    [SerializeField] float distance = 100f;
    [SerializeField] float duration = 2f;
    [SerializeField] bool useUnscaledTime;

    RectTransform rectTransform;
    Vector2 startAnchoredPosition;
    Vector3 startLocalPosition;
    bool isUI;

    void Awake()
    {
        rectTransform = transform as RectTransform;
        isUI = rectTransform != null;

        if (isUI)
            startAnchoredPosition = rectTransform.anchoredPosition;
        else
            startLocalPosition = transform.localPosition;
    }

    void Update()
    {
        float time = useUnscaledTime ? Time.unscaledTime : Time.time;
        float t = Mathf.PingPong(time / duration, 1f);
        Vector2 offset2D = GetOffset2D() * t;
        Vector3 offset3D = GetOffset3D() * t;

        if (isUI)
            rectTransform.anchoredPosition = startAnchoredPosition + offset2D;
        else
            transform.localPosition = startLocalPosition + offset3D;
    }

    Vector2 GetOffset2D()
    {
        switch (direction)
        {
            case Direction.Up: return Vector2.up * distance;
            case Direction.Down: return Vector2.down * distance;
            case Direction.Left: return Vector2.left * distance;
            default: return Vector2.right * distance;
        }
    }

    Vector3 GetOffset3D()
    {
        switch (direction)
        {
            case Direction.Up: return Vector3.up * distance;
            case Direction.Down: return Vector3.down * distance;
            case Direction.Left: return Vector3.left * distance;
            default: return Vector3.right * distance;
        }
    }
}
