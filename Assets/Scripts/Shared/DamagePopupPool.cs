using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class DamagePopupPool : MonoBehaviour
{
    [Header("Pool")]
    [SerializeField] private DamagePopupView popupPrefab;
    [SerializeField, Min(0)] private int prewarmCount = 16;
    [SerializeField] private Vector3 spawnOffset = new Vector3(0f, 1.2f, 0f);
    [SerializeField] private Canvas targetCanvas;
    [SerializeField] private Camera worldCamera;

    private static DamagePopupPool _instance;
    public static DamagePopupPool Instance => _instance;

    private readonly Queue<DamagePopupView> _inactive = new();
    private Transform _poolRoot;
    private bool _warnedMissingCanvas;

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        EnsurePoolRoot();
        Prewarm();
    }

    private void OnDestroy()
    {
        if (_instance == this)
        {
            _instance = null;
        }
    }

    public static bool TrySpawn(int damage, Vector3 worldAnchorPosition)
    {
        if (damage <= 0)
        {
            return false;
        }

        if (_instance == null)
        {
            _instance = FindObjectOfType<DamagePopupPool>();
        }

        if (_instance == null || _instance.popupPrefab == null)
        {
            return false;
        }

        _instance.SpawnInternal(damage, worldAnchorPosition + _instance.spawnOffset);
        return true;
    }

    public void Release(DamagePopupView popup)
    {
        if (!popup)
        {
            return;
        }

        EnsurePoolRoot();
        popup.transform.SetParent(_poolRoot, false);
        popup.gameObject.SetActive(false);
        _inactive.Enqueue(popup);
    }

    private void SpawnInternal(int damage, Vector3 worldPosition)
    {
        var popup = GetOrCreate();
        var canvas = ResolveCanvas();

        if (popup.UsesCanvasSpace)
        {
            if (!canvas)
            {
                if (!_warnedMissingCanvas)
                {
                    _warnedMissingCanvas = true;
                    Debug.LogWarning("DamagePopupView uses CanvasSpace but no Canvas was found. Assign targetCanvas on DamagePopupPool.");
                }

                return;
            }

            popup.transform.SetParent(canvas.transform, false);
        }
        else
        {
            popup.transform.SetParent(null, true);
        }

        popup.Play(this, damage, worldPosition, canvas, ResolveWorldCamera(canvas));
    }

    private DamagePopupView GetOrCreate()
    {
        while (_inactive.Count > 0)
        {
            var cached = _inactive.Dequeue();
            if (cached)
            {
                return cached;
            }
        }

        return Instantiate(popupPrefab, _poolRoot);
    }

    private void EnsurePoolRoot()
    {
        if (_poolRoot)
        {
            return;
        }

        var root = new GameObject("DamagePopupPoolRoot");
        root.transform.SetParent(transform, false);
        _poolRoot = root.transform;
    }

    private void Prewarm()
    {
        if (!popupPrefab || prewarmCount <= 0)
        {
            return;
        }

        for (int i = 0; i < prewarmCount; i++)
        {
            var popup = Instantiate(popupPrefab, _poolRoot);
            popup.gameObject.SetActive(false);
            _inactive.Enqueue(popup);
        }
    }

    private Canvas ResolveCanvas()
    {
        if (targetCanvas && targetCanvas.gameObject.activeInHierarchy)
        {
            return targetCanvas;
        }

        targetCanvas = FindObjectOfType<Canvas>();
        return targetCanvas;
    }

    private Camera ResolveWorldCamera(Canvas canvas)
    {
        if (worldCamera)
        {
            return worldCamera;
        }

        if (canvas && canvas.worldCamera)
        {
            return canvas.worldCamera;
        }

        if (Camera.main)
        {
            return Camera.main;
        }

        return FindObjectOfType<Camera>();
    }
}
