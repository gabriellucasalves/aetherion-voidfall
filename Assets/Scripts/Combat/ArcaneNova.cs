using UnityEngine;

/// <summary>
/// Especial do Mago — Núcleo Arcano: explosão em área azul/arcana.
/// Distinto do Orbe básico (projétil teleguiado). CD fica em PlayerCombat.
/// </summary>
public class ArcaneNova : MonoBehaviour
{
    static Sprite _ringSprite;

    float _radius;
    float _damage;
    float _life = 0.42f;
    float _age;
    Color _color;
    bool _damaged;
    SpriteRenderer _ring;
    SpriteRenderer _core;

    public static void Detonate(Vector3 center, float radius, float damage, Color color)
    {
        var go = new GameObject("NucleoArcano");
        go.transform.position = center;
        var nova = go.AddComponent<ArcaneNova>();
        nova._radius = Mathf.Max(1f, radius);
        nova._damage = damage;
        nova._color = color;
        nova.BuildVisuals();
        nova.ApplyDamage();
        PixelBurst.Spawn(center, color, 14);
        PixelBurst.Spawn(center + Vector3.up * 0.4f, Color.Lerp(color, Color.white, 0.45f), 8);
    }

    void BuildVisuals()
    {
        _ring = CreateSprite("Anel", RingSprite(), 18);
        _ring.color = new Color(_color.r, _color.g, _color.b, 0.85f);
        _ring.transform.localScale = Vector3.one * 0.2f;

        _core = CreateSprite("Nucleo", RingSprite(), 19);
        _core.color = new Color(0.75f, 0.9f, 1f, 0.95f);
        _core.transform.localScale = Vector3.one * 0.35f;
    }

    SpriteRenderer CreateSprite(string name, Sprite sprite, int order)
    {
        var child = new GameObject(name);
        child.transform.SetParent(transform, false);
        var renderer = child.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.sortingOrder = order;
        return renderer;
    }

    void ApplyDamage()
    {
        if (_damaged)
            return;
        _damaged = true;

        var enemies = Object.FindObjectsByType<EnemyController>(FindObjectsSortMode.None);
        foreach (var enemy in enemies)
        {
            if (enemy == null || enemy.Health == null || enemy.Health.IsDead)
                continue;

            Vector2 enemyPoint = (Vector2)enemy.transform.position + Vector2.up * 0.55f;
            if (Vector2.Distance(transform.position, enemyPoint) > _radius)
                continue;

            enemy.ReceiveDamage(_damage);
            PixelBurst.Spawn(enemyPoint, _color, 5);
        }
    }

    void Update()
    {
        _age += Time.deltaTime;
        float t = Mathf.Clamp01(_age / _life);
        float size = Mathf.Lerp(0.25f, _radius * 2.1f, Mathf.SmoothStep(0f, 1f, t));
        if (_ring != null)
        {
            _ring.transform.localScale = Vector3.one * size;
            var c = _ring.color;
            c.a = Mathf.Lerp(0.9f, 0f, t);
            _ring.color = c;
        }

        if (_core != null)
        {
            _core.transform.localScale = Vector3.one * Mathf.Lerp(0.55f, 0.05f, t);
            var c = _core.color;
            c.a = Mathf.Lerp(1f, 0f, t);
            _core.color = c;
        }

        if (_age >= _life)
            Destroy(gameObject);
    }

    static Sprite RingSprite()
    {
        if (_ringSprite != null)
            return _ringSprite;

        const int size = 64;
        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        var pixels = new Color32[size * size];
        float cx = (size - 1) * 0.5f;
        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            float d = Vector2.Distance(new Vector2(x, y), new Vector2(cx, cx)) / (size * 0.5f);
            Color32 color = default;
            if (d > 0.72f && d <= 0.98f)
                color = new Color32(120, 190, 255, 230);
            else if (d > 0.45f && d <= 0.72f)
                color = new Color32(70, 130, 255, 70);
            else if (d <= 0.45f)
                color = new Color32(180, 230, 255, 40);
            pixels[y * size + x] = color;
        }

        texture.SetPixels32(pixels);
        texture.Apply();
        texture.filterMode = FilterMode.Point;
        _ringSprite = Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), size);
        return _ringSprite;
    }
}
