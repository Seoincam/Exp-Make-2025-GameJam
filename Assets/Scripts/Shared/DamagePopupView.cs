using UnityEngine;
using TMPro;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class DamagePopupView : MonoBehaviour
{
    [Header("Text")]
    [SerializeField] private bool useCanvasSpace = true;
    [SerializeField] private TextMesh textMesh;
    [SerializeField] private TMP_Text tmpText;
    [SerializeField] private Text uiText;

    [Header("Motion")]
    [SerializeField, Min(0.01f)] private float duration = 0.7f;
    [SerializeField] private Vector3 riseVelocity = new Vector3(0f, 1.2f, 0f);

    private RectTransform _rectTransform;
    private Canvas _canvas;
    private DamagePopupPool _pool;
    private float _elapsed;
    private Color _baseColor = Color.white;
    private Vector3 _worldPosition;

    public bool UsesCanvasSpace => useCanvasSpace;

    private void Reset()
    {
        _rectTransform = transform as RectTransform;

        if (!textMesh)
        {
            textMesh = GetComponentInChildren<TextMesh>(true);
        }
        if (!tmpText)
        {
            tmpText = GetComponentInChildren<TMP_Text>(true);
        }
        if (!uiText)
        {
            uiText = GetComponentInChildren<Text>(true);
        }
    }

    private void Awake()
    {
        _rectTransform = transform as RectTransform;

        if (!textMesh)
        {
            textMesh = GetComponentInChildren<TextMesh>(true);
        }
        if (!tmpText)
        {
            tmpText = GetComponentInChildren<TMP_Text>(true);
        }
        if (!uiText)
        {
            uiText = GetComponentInChildren<Text>(true);
        }
    }

    private void OnEnable()
    {
        _elapsed = 0f;
    }

    private void Update()
    {
        float dt = Time.deltaTime;
        _elapsed += dt;

        _worldPosition += riseVelocity * dt;
        if (useCanvasSpace)
        {
            UpdateCanvasPositionFromWorld();
        }
        else
        {
            transform.position = _worldPosition;
        }

        float t = Mathf.Clamp01(_elapsed / Mathf.Max(0.01f, duration));
        SetColorWithAlpha(Mathf.Lerp(_baseColor.a, 0f, t));

        if (_elapsed >= duration)
        {
            ReturnToPool();
        }
    }

    public void Play(DamagePopupPool pool, int damage, Vector3 worldPosition, Canvas canvas)
    {
        _pool = pool;
        _canvas = canvas;
        _worldPosition = worldPosition;
        _elapsed = 0f;

        SetText(damage.ToString());
        CaptureBaseColor();
        SetColorWithAlpha(_baseColor.a);

        if (useCanvasSpace)
        {
            UpdateCanvasPositionFromWorld();
        }
        else
        {
            transform.position = _worldPosition;
        }

        gameObject.SetActive(true);
    }

    public void ReturnToPool()
    {
        if (_pool)
        {
            _pool.Release(this);
            return;
        }

        gameObject.SetActive(false);
    }

    private void SetText(string value)
    {
        if (tmpText)
        {
            tmpText.text = value;
            return;
        }
        if (uiText)
        {
            uiText.text = value;
            return;
        }

        if (textMesh)
        {
            textMesh.text = value;
        }
    }

    private void CaptureBaseColor()
    {
        if (tmpText)
        {
            _baseColor = tmpText.color;
            return;
        }
        if (uiText)
        {
            _baseColor = uiText.color;
            return;
        }
        if (textMesh)
        {
            _baseColor = textMesh.color;
            return;
        }

        _baseColor = Color.white;
    }

    private void SetColorWithAlpha(float alpha)
    {
        if (tmpText)
        {
            var color = _baseColor;
            color.a = alpha;
            tmpText.color = color;
            return;
        }
        if (uiText)
        {
            var color = _baseColor;
            color.a = alpha;
            uiText.color = color;
            return;
        }
        if (textMesh)
        {
            var color = _baseColor;
            color.a = alpha;
            textMesh.color = color;
        }
    }

    private void UpdateCanvasPositionFromWorld()
    {
        if (!_canvas || !_rectTransform)
        {
            return;
        }

        var canvasRect = _canvas.transform as RectTransform;
        if (!canvasRect)
        {
            return;
        }

        Camera cam = _canvas.renderMode == RenderMode.ScreenSpaceOverlay
            ? null
            : (_canvas.worldCamera ? _canvas.worldCamera : Camera.main);

        Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(cam, _worldPosition);
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect,
            screenPoint,
            cam,
            out Vector2 localPoint))
        {
            _rectTransform.anchoredPosition = localPoint;
        }
    }
}
