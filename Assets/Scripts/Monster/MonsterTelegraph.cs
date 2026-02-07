using UnityEngine;

public sealed class AttackTelegraphRect
{
    GameObject _go;
    SpriteRenderer _sr;

    public bool IsValid => _go != null;

    public void Ensure(string name = "TelegraphRect", int sortingOrder = 1000)
    {
        if (_go) return;

        _go = new GameObject(name);
        _sr = _go.AddComponent<SpriteRenderer>();

        // 1x1 흰색 텍스처 생성
        var tex = new Texture2D(1, 1);
        tex.SetPixel(0, 0, Color.white);
        tex.Apply();

        _sr.sprite = Sprite.Create(
            tex,
            new Rect(0, 0, 1, 1),
            new Vector2(0.5f, 0.5f),
            1f
        );

        _sr.sortingOrder = sortingOrder;
        _sr.enabled = false;
    }

    public void Show(Vector2 center, float length, float width, float angleDeg, Color color)
    {
        Ensure();

        _go.transform.position = new Vector3(center.x, center.y, 0f);
        _go.transform.rotation = Quaternion.Euler(0f, 0f, angleDeg);

        // 기본 스프라이트는 1x1 이라고 가정하고 scale로 크기 조절
        _go.transform.localScale = new Vector3(length, width, 1f);

        _sr.color = color;
        _sr.enabled = true;
    }

    public void Hide()
    {
        if (_sr) _sr.enabled = false;
    }

    public void Destroy()
    {
        if (_go) Object.Destroy(_go);
        _go = null;
        _sr = null;
    }
}
