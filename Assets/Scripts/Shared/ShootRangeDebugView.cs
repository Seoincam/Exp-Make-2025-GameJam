using UnityEngine;

namespace Combat.Shoot
{
    [DisallowMultipleComponent]
    public class ShootRangeDebugView : MonoBehaviour
    {
        [Header("Reference")]
        [SerializeField] private ShootComponent shootComponent;

        [Header("Runtime Draw")]
        [SerializeField] private bool showInGame = true;
        [SerializeField] private int segments = 64;
        [SerializeField] private float lineWidth = 0.05f;
        [SerializeField] private Color drawColor = Color.red;

        [Header("Editor Draw")]
        [SerializeField] private bool showGizmoWhenSelected = true;

        private const string RendererObjectName = "ShootRangeRenderer";
        private LineRenderer _lineRenderer;

        private void Awake()
        {
            if (!shootComponent)
            {
                TryGetComponent(out shootComponent);
            }

            EnsureRenderer();
            UpdateRenderer();
        }

        private void LateUpdate()
        {
            UpdateRenderer();
        }

        private void OnDisable()
        {
            if (_lineRenderer)
            {
                _lineRenderer.enabled = false;
            }
        }

        private void OnDrawGizmosSelected()
        {
            if (!showGizmoWhenSelected)
            {
                return;
            }

            float radius = GetRadius();
            if (radius <= 0f)
            {
                return;
            }

            Gizmos.color = drawColor;
            Gizmos.DrawWireSphere(transform.position, radius);
        }

        private float GetRadius()
        {
            return shootComponent ? shootComponent.SearchRadius : 0f;
        }

        private void EnsureRenderer()
        {
            if (_lineRenderer)
            {
                return;
            }

            Transform rendererTransform = transform.Find(RendererObjectName);
            if (!rendererTransform)
            {
                var go = new GameObject(RendererObjectName);
                go.transform.SetParent(transform, false);
                rendererTransform = go.transform;
            }

            if (!rendererTransform.TryGetComponent(out _lineRenderer))
            {
                _lineRenderer = rendererTransform.gameObject.AddComponent<LineRenderer>();
            }

            _lineRenderer.useWorldSpace = false;
            _lineRenderer.loop = true;
            _lineRenderer.textureMode = LineTextureMode.Stretch;
            _lineRenderer.numCapVertices = 2;
            _lineRenderer.numCornerVertices = 2;
            _lineRenderer.alignment = LineAlignment.TransformZ;
            _lineRenderer.sortingOrder = 1000;

            Shader shader = Shader.Find("Sprites/Default");
            if (shader != null)
            {
                _lineRenderer.material = new Material(shader);
            }
        }

        private void UpdateRenderer()
        {
            if (!_lineRenderer)
            {
                EnsureRenderer();
            }

            if (!_lineRenderer)
            {
                return;
            }

            float radius = GetRadius();
            if (!showInGame || radius <= 0f)
            {
                _lineRenderer.enabled = false;
                return;
            }

            _lineRenderer.enabled = true;
            _lineRenderer.startWidth = lineWidth;
            _lineRenderer.endWidth = lineWidth;
            _lineRenderer.startColor = drawColor;
            _lineRenderer.endColor = drawColor;

            int segmentCount = Mathf.Max(8, segments);
            _lineRenderer.positionCount = segmentCount;

            float step = Mathf.PI * 2f / segmentCount;
            for (int i = 0; i < segmentCount; i++)
            {
                float angle = step * i;
                float x = Mathf.Cos(angle) * radius;
                float y = Mathf.Sin(angle) * radius;
                _lineRenderer.SetPosition(i, new Vector3(x, y, 0f));
            }
        }
    }
}
