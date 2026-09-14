using UnityEngine;

/// <summary>
/// Especial do Mago — Núcleo Arcano: explosão em área azul/ciano/roxo (dark fantasy pixel).
/// Distinto do Orbe básico (ArcaneBolt). CD próprio fica em PlayerCombat (~6.5s).
/// VFX FASE 3: anel rúnico + hex + núcleo + shockwave + PixelBurst (não é círculo branco simples).
/// </summary>
public class ArcaneNova : MonoBehaviour
{
    // Atraso para alinhar o pico da explosão com frames 18–19 do special (@ ~11 fps ≈ 0.18s).
    const float ArmDelay = 0.18f;
    const float LifeAfterArm = 0.58f;

    static Sprite _runeRingSprite;
    static Sprite _hexSprite;
    static Sprite _coreSprite;
    static Sprite _shockSprite;

    float _radius;
    float _damage;
    float _age;
    float _armAt = ArmDelay;
    Color _color;
    bool _armed;
    bool _damaged;

    SpriteRenderer _runeRing;
    SpriteRenderer _hex;
    SpriteRenderer _core;
    SpriteRenderer _shock;
    SpriteRenderer _flash;

    public static void Detonate(Vector3 center, float radius, float damage, Color color)
    {
        var go = new GameObject("NucleoArcano");
        go.transform.position = center;
        var nova = go.AddComponent<ArcaneNova>();
        nova._radius = Mathf.Max(1f, radius);
        nova._damage = damage;
        nova._color = color;
        nova.BuildVisuals();
        // Charge spark no centro — mago já está no clip special (17–20).
        PixelBurst.Spawn(center, color, 4);
        PixelBurst.Spawn(center, new Color(0.7f, 0.45f, 1f), 3);
    }

    void BuildVisuals()
    {
        // Flash inicial (pulso de carga).
        _flash = CreateSprite("Flash", CoreSprite(), 22);
        _flash.color = new Color(0.85f, 0.95f, 1f, 0.7f);
        _flash.transform.localScale = Vector3.one * 0.25f;

        _hex = CreateSprite("HexRunico", HexSprite(), 18);
        _hex.color = new Color(0.65f, 0.4f, 1f, 0.75f);
        _hex.transform.localScale = Vector3.one * 0.35f;

        _runeRing = CreateSprite("AnelRunico", RuneRingSprite(), 19);
        _runeRing.color = new Color(_color.r, _color.g, _color.b, 0.9f);
        _runeRing.transform.localScale = Vector3.one * 0.4f;

        _core = CreateSprite("Nucleo", CoreSprite(), 21);
        _core.color = new Color(0.8f, 0.95f, 1f, 0.95f);
        _core.transform.localScale = Vector3.one * 0.3f;

        _shock = CreateSprite("Onda", ShockSprite(), 17);
        _shock.color = new Color(0.4f, 0.85f, 1f, 0f);
        _shock.transform.localScale = Vector3.one * 0.2f;
        _shock.enabled = false;
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

    void Update()
    {
        _age += Time.deltaTime;

        if (!_armed)
        {
            AnimateWindup();
            if (_age >= _armAt)
                Arm();
            return;
        }

        float t = Mathf.Clamp01((_age - _armAt) / LifeAfterArm);
        AnimateExplosion(t);

        if (_age - _armAt >= LifeAfterArm)
            Destroy(gameObject);
    }

    void AnimateWindup()
    {
        float u = Mathf.Clamp01(_age / _armAt);
        float pulse = 0.85f + Mathf.Sin(_age * 28f) * 0.15f;

        if (_hex != null)
        {
            _hex.transform.localRotation = Quaternion.Euler(0f, 0f, _age * 90f);
            _hex.transform.localScale = Vector3.one * Mathf.Lerp(0.3f, 0.85f, u) * pulse;
            var c = _hex.color;
            c.a = Mathf.Lerp(0.4f, 0.9f, u);
            _hex.color = c;
        }

        if (_runeRing != null)
        {
            _runeRing.transform.localRotation = Quaternion.Euler(0f, 0f, -_age * 70f);
            _runeRing.transform.localScale = Vector3.one * Mathf.Lerp(0.35f, 1.0f, u);
            var c = _runeRing.color;
            c.a = Mathf.Lerp(0.5f, 1f, u);
            _runeRing.color = c;
        }

        if (_core != null)
        {
            _core.transform.localScale = Vector3.one * Mathf.Lerp(0.2f, 0.55f, u) * pulse;
            var c = _core.color;
            c.a = Mathf.Lerp(0.5f, 1f, u);
            _core.color = c;
        }

        if (_flash != null)
        {
            _flash.transform.localScale = Vector3.one * Mathf.Lerp(0.2f, 0.7f, u);
            var c = _flash.color;
            c.a = Mathf.Lerp(0.3f, 0.85f, u) * pulse;
            _flash.color = c;
        }
    }

    void Arm()
    {
        _armed = true;
        ApplyDamage();

        if (_shock != null)
            _shock.enabled = true;

        // Explosão legível à distância: camadas ciano + roxo + branco.
        PixelBurst.Spawn(transform.position, _color, 18);
        PixelBurst.Spawn(transform.position, new Color(0.7f, 0.4f, 1f), 12);
        PixelBurst.Spawn(transform.position + Vector3.up * 0.35f, Color.Lerp(_color, Color.white, 0.55f), 10);
        PixelBurst.Spawn(transform.position + Vector3.left * 0.4f, new Color(0.45f, 0.8f, 1f), 5);
        PixelBurst.Spawn(transform.position + Vector3.right * 0.4f, new Color(0.7f, 0.45f, 1f), 5);
    }

    void AnimateExplosion(float t)
    {
        float ease = Mathf.SmoothStep(0f, 1f, t);
        float ringSize = Mathf.Lerp(0.9f, _radius * 2.15f, ease);

        if (_runeRing != null)
        {
            _runeRing.transform.localScale = Vector3.one * ringSize;
            _runeRing.transform.localRotation = Quaternion.Euler(0f, 0f, -t * 120f);
            var c = _runeRing.color;
            c.a = Mathf.Lerp(1f, 0f, Mathf.Pow(t, 0.7f));
            _runeRing.color = c;
        }

        if (_hex != null)
        {
            _hex.transform.localScale = Vector3.one * (ringSize * 0.72f);
            _hex.transform.localRotation = Quaternion.Euler(0f, 0f, t * 160f);
            var c = _hex.color;
            c.a = Mathf.Lerp(0.95f, 0f, t);
            _hex.color = c;
        }

        if (_shock != null)
        {
            _shock.transform.localScale = Vector3.one * Mathf.Lerp(0.4f, _radius * 2.35f, ease);
            var c = _shock.color;
            c.a = t < 0.35f ? Mathf.Lerp(0.7f, 0.35f, t / 0.35f) : Mathf.Lerp(0.35f, 0f, (t - 0.35f) / 0.65f);
            _shock.color = c;
        }

        if (_core != null)
        {
            // Núcleo explode rápido e some.
            float coreT = Mathf.Clamp01(t / 0.35f);
            _core.transform.localScale = Vector3.one * Mathf.Lerp(0.7f, 0.05f, coreT);
            var c = _core.color;
            c.a = Mathf.Lerp(1f, 0f, coreT);
            _core.color = c;
        }

        if (_flash != null)
        {
            float flashT = Mathf.Clamp01(t / 0.22f);
            _flash.transform.localScale = Vector3.one * Mathf.Lerp(0.8f, _radius * 1.6f, flashT);
            var c = _flash.color;
            c.a = Mathf.Lerp(0.9f, 0f, flashT);
            _flash.color = c;
        }
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
            PixelBurst.Spawn(enemyPoint, _color, 6);
            PixelBurst.Spawn(enemyPoint, new Color(0.75f, 0.5f, 1f), 3);
        }
    }

    // --- Sprites procedurais (Point filter) ---

    static Sprite RuneRingSprite()
    {
        if (_runeRingSprite != null)
            return _runeRingSprite;

        const int size = 96;
        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        var pixels = new Color32[size * size];
        float cx = (size - 1) * 0.5f;
        float maxR = size * 0.5f;

        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            float dx = x - cx;
            float dy = y - cx;
            float d = Mathf.Sqrt(dx * dx + dy * dy) / maxR;
            float ang = Mathf.Atan2(dy, dx); // -pi..pi

            Color32 c = default;

            // Anel externo espesso (legível à distância).
            if (d > 0.78f && d <= 0.98f)
            {
                byte a = (byte)(d > 0.94f || d < 0.82f ? 255 : 200);
                c = new Color32(90, 200, 255, a);
            }
            // Anel interno fino.
            else if (d > 0.58f && d <= 0.68f)
            {
                c = new Color32(160, 100, 255, 180);
            }
            // Marcas rúnicas / "dentes" no anel externo.
            else if (d > 0.68f && d <= 0.78f)
            {
                float spokes = Mathf.Abs(Mathf.Sin(ang * 6f));
                if (spokes > 0.82f)
                    c = new Color32(210, 240, 255, 230);
                else if (spokes > 0.55f)
                    c = new Color32(120, 80, 220, 90);
            }
            // Pontos rúnicos (8 glifos).
            if (d > 0.84f && d < 0.92f)
            {
                float spoke = Mathf.Abs(Mathf.Sin(ang * 4f));
                if (spoke > 0.92f)
                    c = new Color32(230, 250, 255, 255);
            }

            pixels[y * size + x] = c;
        }

        texture.SetPixels32(pixels);
        texture.Apply();
        texture.filterMode = FilterMode.Point;
        texture.wrapMode = TextureWrapMode.Clamp;
        _runeRingSprite = Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), size);
        return _runeRingSprite;
    }

    static Sprite HexSprite()
    {
        if (_hexSprite != null)
            return _hexSprite;

        string[] art =
        {
            "........cc........",
            "......ccwwcc......",
            "....ccwppppwcc....",
            "...cwpp....ppwc...",
            "..cwp...yy...pwc..",
            ".cwp....yy....pwc.",
            "cwp.....yy.....pwc",
            "cwp.....yy.....pwc",
            ".cwp....yy....pwc.",
            "..cwp...yy...pwc..",
            "...cwpp....ppwc...",
            "....ccwppppwcc....",
            "......ccwwcc......",
            "........cc........",
        };

        _hexSprite = PixelArtSprite(art, ch => ch switch
        {
            'w' => new Color32(210, 245, 255, 255),
            'c' => new Color32(70, 190, 255, 240),
            'p' => new Color32(150, 80, 255, 210),
            'y' => new Color32(200, 160, 255, 255),
            _ => new Color32(0, 0, 0, 0),
        }, 14f);
        return _hexSprite;
    }

    static Sprite CoreSprite()
    {
        if (_coreSprite != null)
            return _coreSprite;

        string[] art =
        {
            "....c....",
            "...cwc...",
            "..cwwwc..",
            ".cbwwwwb.",
            "cbwwwwwbc",
            ".cbwwwwb.",
            "..cwwwc..",
            "...cbc...",
            "....b....",
        };

        _coreSprite = PixelArtSprite(art, ch => ch switch
        {
            'w' => new Color32(230, 250, 255, 255),
            'c' => new Color32(100, 210, 255, 255),
            'b' => new Color32(90, 70, 220, 240),
            _ => new Color32(0, 0, 0, 0),
        }, 12f);
        return _coreSprite;
    }

    static Sprite ShockSprite()
    {
        if (_shockSprite != null)
            return _shockSprite;

        const int size = 80;
        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        var pixels = new Color32[size * size];
        float cx = (size - 1) * 0.5f;
        float maxR = size * 0.5f;
        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            float d = Vector2.Distance(new Vector2(x, y), new Vector2(cx, cx)) / maxR;
            Color32 c = default;
            // Aro fino de onda de choque.
            if (d > 0.86f && d <= 0.98f)
                c = new Color32(160, 230, 255, 220);
            else if (d > 0.78f && d <= 0.86f)
                c = new Color32(120, 90, 255, 100);
            pixels[y * size + x] = c;
        }

        texture.SetPixels32(pixels);
        texture.Apply();
        texture.filterMode = FilterMode.Point;
        _shockSprite = Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), size);
        return _shockSprite;
    }

    static Sprite PixelArtSprite(string[] art, System.Func<char, Color32> paint, float ppu)
    {
        int h = art.Length;
        int w = art[0].Length;
        var texture = new Texture2D(w, h, TextureFormat.RGBA32, false);
        var pixels = new Color32[w * h];
        for (int y = 0; y < h; y++)
        {
            string row = art[h - 1 - y];
            for (int x = 0; x < w; x++)
                pixels[y * w + x] = paint(x < row.Length ? row[x] : '.');
        }

        texture.SetPixels32(pixels);
        texture.Apply();
        texture.filterMode = FilterMode.Point;
        texture.wrapMode = TextureWrapMode.Clamp;
        return Sprite.Create(texture, new Rect(0f, 0f, w, h), new Vector2(0.5f, 0.5f), ppu);
    }
}
