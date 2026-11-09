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
    bool _runicPulse;
    HealthSystem _ownerHealth;

    SpriteRenderer _runeRing;
    SpriteRenderer _hex;
    SpriteRenderer _core;
    SpriteRenderer _shock;
    SpriteRenderer _flash;
    SpriteRenderer _packPulse;
    Sprite[] _packFrames;

    public static void Detonate(Vector3 center, float radius, float damage, Color color,
        bool runicPulse = false, Transform owner = null)
    {
        var go = new GameObject("NucleoArcano");
        go.transform.position = center;
        var nova = go.AddComponent<ArcaneNova>();
        nova._radius = Mathf.Max(1f, radius);
        nova._damage = damage;
        nova._color = color;
        nova._runicPulse = runicPulse;
        nova._armAt = runicPulse ? .34f : ArmDelay;
        nova._ownerHealth = owner != null ? owner.GetComponent<HealthSystem>() : null;
        nova.BuildVisuals();
        // Charge spark no centro — mago já está no clip special (17–20).
        PixelBurst.Spawn(center, color, 4);
        PixelBurst.Spawn(center, new Color(0.7f, 0.45f, 1f), 3);
    }

    void BuildVisuals()
    {
        _packFrames = _runicPulse
            ? TrioAnimationFrames.Vfx("pulso_runico", 64, 24, 7, new Vector2(.5f, 16f / 24f)) : null;
        if (_packFrames != null)
        {
            _packPulse = CreateSprite("PulsoRunicoV1", _packFrames[0], 24);
            _packPulse.transform.localScale = new Vector3(2.2f, 2.2f, 1f);
        }
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
        if (_ownerHealth != null && _ownerHealth.IsDead) { Destroy(gameObject); return; }
        _age += Time.deltaTime;
        if (_packPulse != null)
        {
            int frame = _age < _armAt
                ? Mathf.Min(3, Mathf.FloorToInt(_age / _armAt * 4f))
                : Mathf.Min(6, 4 + Mathf.FloorToInt((_age - _armAt) * 14f));
            _packPulse.sprite = _packFrames[frame];
        }

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
            if (_runicPulse) enemy.ApplySlow(1.5f);
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

/// <summary>
/// Especial do Mago (L/Q) — Zona Rúnica no chão:
/// - Posicionada à frente do Mago (facing * 2.3 + up * 0.55)
/// - Raio: 2.8 | Duração: 3.5s
/// - 1º tick após 0.25s, depois a cada 0.45s enquanto a zona existir
/// - Dano por tick: Power * 0.16 (100 -> 16)
/// - Lentidão por tick: ApplySlow(0.60s) em todos os inimigos dentro do raio (Golem recebe o dano e ignora slow)
/// - Apenas 1 zona ativa por vez (nova conjuração substitui a anterior)
/// </summary>
public class ArcaneZone : MonoBehaviour
{
    public const float DefaultRadius = 2.8f;
    public const float DefaultDuration = 3.5f;
    public const float FirstTickDelay = 0.25f;
    public const float TickInterval = 0.45f;
    public const float TickDamageScale = 0.16f;
    public const float SlowDurationPerTick = 0.60f;

    static ArcaneZone _activeZone;
    static Sprite _groundRingSprite;
    static Sprite _innerHexSprite;
    static Sprite _pillarGlyphSprite;

    float _radius = DefaultRadius;
    float _duration = DefaultDuration;
    float _tickDamage = 16f;
    float _age;
    float _nextTickAt = FirstTickDelay;
    int _tickCount;
    Color _color;
    HealthSystem _ownerHealth;

    SpriteRenderer _outerRing;
    SpriteRenderer _innerHex;
    SpriteRenderer _groundPulse;
    SpriteRenderer _leftGlyph;
    SpriteRenderer _rightGlyph;
    Sprite[] _packFrames;

    public static ArcaneZone ActiveZone => _activeZone;
    public int TickCount => _tickCount;
    public float Radius => _radius;

    public static ArcaneZone Spawn(
        Vector3 center,
        float radius,
        float tickDamage,
        Color color,
        Transform owner = null,
        float duration = DefaultDuration)
    {
        // Só uma zona por vez: se já existir uma ativa, remove imediatamente
        if (_activeZone != null)
        {
            Destroy(_activeZone.gameObject);
            _activeZone = null;
        }

        var go = new GameObject("ZonaRunica");
        go.transform.position = center;
        var zone = go.AddComponent<ArcaneZone>();
        zone._radius = Mathf.Max(1f, radius);
        zone._tickDamage = Mathf.Max(1f, tickDamage);
        zone._duration = Mathf.Max(0.5f, duration);
        zone._color = color;
        zone._ownerHealth = owner != null ? owner.GetComponent<HealthSystem>() : null;
        zone.BuildVisuals();

        _activeZone = zone;

        // Burst curto de abertura no centro (o dano real vem nos ticks)
        PixelBurst.Spawn(center, color, 8);
        PixelBurst.Spawn(center, new Color(0.72f, 0.45f, 1f), 6);
        return zone;
    }

    void OnDestroy()
    {
        if (_activeZone == this)
            _activeZone = null;
    }

    void BuildVisuals()
    {
        float diameter = _radius * 2f;

        // Anel rúnico elíptico no chão (perspectiva 2D side-view)
        _outerRing = CreateChildSprite("AnelChaoRunico", GroundRingSprite(), 17);
        _outerRing.color = new Color(_color.r, _color.g, _color.b, 0.92f);
        _outerRing.transform.localPosition = new Vector3(0f, -0.32f, 0f);
        _outerRing.transform.localScale = new Vector3(diameter, diameter * 0.42f, 1f);

        // Hexagrama rúnico interno no solo
        _innerHex = CreateChildSprite("HexChaoRunico", InnerHexSprite(), 18);
        _innerHex.color = new Color(0.72f, 0.45f, 1f, 0.85f);
        _innerHex.transform.localPosition = new Vector3(0f, -0.30f, 0f);
        _innerHex.transform.localScale = new Vector3(diameter * 0.72f, diameter * 0.32f, 1f);

        // Spritesheet pulso_runico na base da zona para reforçar o selo arcano 16-bit
        _packFrames = TrioAnimationFrames.Vfx("pulso_runico", 64, 24, 7, new Vector2(0.5f, 16f / 24f));
        if (_packFrames != null && _packFrames.Length > 0)
        {
            _groundPulse = CreateChildSprite("SeloPulsoRunico", _packFrames[0], 19);
            _groundPulse.transform.localPosition = new Vector3(0f, -0.18f, 0f);
            _groundPulse.transform.localScale = new Vector3(1.55f, 1.15f, 1f);
            _groundPulse.color = new Color(0.85f, 0.95f, 1f, 0.88f);
        }

        // Glifos verticais discretos nas bordas da zona para leitura clara do raio (2.8)
        _leftGlyph = CreateChildSprite("GlifoEsquerdo", PillarGlyphSprite(), 18);
        _leftGlyph.transform.localPosition = new Vector3(-_radius * 0.82f, 0.05f, 0f);
        _leftGlyph.color = new Color(0.55f, 0.90f, 1f, 0.85f);

        _rightGlyph = CreateChildSprite("GlifoDireito", PillarGlyphSprite(), 18);
        _rightGlyph.transform.localPosition = new Vector3(_radius * 0.82f, 0.05f, 0f);
        _rightGlyph.color = new Color(0.72f, 0.48f, 1f, 0.85f);
    }

    SpriteRenderer CreateChildSprite(string childName, Sprite sprite, int sortingOrder)
    {
        var child = new GameObject(childName);
        child.transform.SetParent(transform, false);
        var sr = child.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingOrder = sortingOrder;
        return sr;
    }

    void Update()
    {
        if (_ownerHealth != null && _ownerHealth.IsDead)
        {
            Destroy(gameObject);
            return;
        }

        _age += Time.deltaTime;

        while (_age >= _nextTickAt && _nextTickAt <= _duration + 0.01f)
        {
            ExecuteTick();
            _nextTickAt += TickInterval;
        }

        AnimateZone();

        if (_age >= _duration)
            Destroy(gameObject);
    }

    void ExecuteTick()
    {
        _tickCount++;

        // Pulso visual leve na zona a cada tick
        Vector3 basePos = transform.position + Vector3.down * 0.25f;
        PixelBurst.Spawn(basePos + Vector3.left * (_radius * 0.45f), _color, 3);
        PixelBurst.Spawn(basePos + Vector3.right * (_radius * 0.45f), new Color(0.72f, 0.45f, 1f), 3);

        var enemies = Object.FindObjectsByType<EnemyController>(FindObjectsSortMode.None);
        foreach (var enemy in enemies)
        {
            if (enemy == null || enemy.Health == null || enemy.Health.IsDead)
                continue;

            Vector2 enemyCenter = (Vector2)enemy.transform.position + Vector2.up * 0.55f;
            if (Vector2.Distance(transform.position, enemyCenter) > _radius)
                continue;

            // Aplica o tick de dano (16) e renova a lentidão curta (0.60s).
            // Golem (AttackKind == "charge") recebe o dano normalmente e ignora lentidão dentro de ApplySlow.
            enemy.ReceiveDamage(_tickDamage);
            enemy.ApplySlow(SlowDurationPerTick);
            PixelBurst.Spawn(enemyCenter, _color, 4);
            PixelBurst.Spawn(enemyCenter, new Color(0.72f, 0.45f, 1f), 2);
        }
    }

    void AnimateZone()
    {
        float fadeOut = _age > _duration - 0.35f
            ? Mathf.Clamp01((_duration - _age) / 0.35f)
            : 1f;
        float pulse = 0.94f + Mathf.Sin(_age * 10f) * 0.06f;
        float diameter = _radius * 2f;

        if (_outerRing != null)
        {
            _outerRing.transform.localScale = new Vector3(diameter * pulse, diameter * 0.42f * pulse, 1f);
            var c = _outerRing.color;
            c.a = 0.90f * fadeOut;
            _outerRing.color = c;
        }

        if (_innerHex != null)
        {
            float hexPulse = 0.92f + Mathf.Cos(_age * 12f) * 0.08f;
            _innerHex.transform.localScale = new Vector3(diameter * 0.72f * hexPulse, diameter * 0.32f * hexPulse, 1f);
            var c = _innerHex.color;
            c.a = 0.82f * fadeOut;
            _innerHex.color = c;
        }

        if (_groundPulse != null && _packFrames != null && _packFrames.Length > 0)
        {
            // Cicla nos frames centrais (1..5) do pulso rúnico para manter o selo vivo no chão
            int frameIdx = 1 + (Mathf.FloorToInt(_age * 10f) % Mathf.Max(1, _packFrames.Length - 2));
            _groundPulse.sprite = _packFrames[Mathf.Clamp(frameIdx, 0, _packFrames.Length - 1)];
            var c = _groundPulse.color;
            c.a = (0.72f + Mathf.Sin(_age * 14f) * 0.16f) * fadeOut;
            _groundPulse.color = c;
        }

        if (_leftGlyph != null && _rightGlyph != null)
        {
            float bob = Mathf.Sin(_age * 8f) * 0.08f;
            _leftGlyph.transform.localPosition = new Vector3(-_radius * 0.82f, 0.05f + bob, 0f);
            _rightGlyph.transform.localPosition = new Vector3(_radius * 0.82f, 0.05f - bob, 0f);
            var cl = _leftGlyph.color;
            cl.a = 0.85f * fadeOut;
            _leftGlyph.color = cl;
            var cr = _rightGlyph.color;
            cr.a = 0.85f * fadeOut;
            _rightGlyph.color = cr;
        }
    }

    static Sprite GroundRingSprite()
    {
        if (_groundRingSprite != null)
            return _groundRingSprite;

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
            float ang = Mathf.Atan2(dy, dx);

            Color32 c = default;
            if (d > 0.80f && d <= 0.98f)
            {
                c = (d > 0.93f || d < 0.84f)
                    ? new Color32(107, 247, 255, 255)
                    : new Color32(58, 175, 240, 215);
            }
            else if (d > 0.54f && d <= 0.64f)
            {
                c = new Color32(168, 96, 255, 210);
            }
            else if (d > 0.64f && d <= 0.80f)
            {
                float spokes = Mathf.Abs(Mathf.Sin(ang * 6f));
                if (spokes > 0.84f)
                    c = new Color32(225, 250, 255, 240);
                else if (spokes > 0.55f)
                    c = new Color32(120, 75, 220, 120);
            }
            else if (d <= 0.54f)
            {
                // Preenchimento translúcido discreto em xadrez/dither 16-bit
                if (((x + y) & 1) == 0 && d > 0.18f)
                    c = new Color32(48, 95, 195, 65);
            }

            pixels[y * size + x] = c;
        }

        texture.SetPixels32(pixels);
        texture.Apply();
        texture.filterMode = FilterMode.Point;
        texture.wrapMode = TextureWrapMode.Clamp;
        _groundRingSprite = Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), size);
        return _groundRingSprite;
    }

    static Sprite InnerHexSprite()
    {
        if (_innerHexSprite != null)
            return _innerHexSprite;

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

        int h = art.Length;
        int w = art[0].Length;
        var texture = new Texture2D(w, h, TextureFormat.RGBA32, false);
        var pixels = new Color32[w * h];
        for (int y = 0; y < h; y++)
        {
            string row = art[h - 1 - y];
            for (int x = 0; x < w; x++)
            {
                char ch = x < row.Length ? row[x] : '.';
                pixels[y * w + x] = ch switch
                {
                    'w' => new Color32(220, 250, 255, 255),
                    'c' => new Color32(90, 210, 255, 240),
                    'p' => new Color32(165, 90, 255, 225),
                    'y' => new Color32(215, 180, 255, 255),
                    _ => new Color32(0, 0, 0, 0),
                };
            }
        }
        texture.SetPixels32(pixels);
        texture.Apply();
        texture.filterMode = FilterMode.Point;
        texture.wrapMode = TextureWrapMode.Clamp;
        _innerHexSprite = Sprite.Create(texture, new Rect(0f, 0f, w, h), new Vector2(0.5f, 0.5f), w);
        return _innerHexSprite;
    }

    static Sprite PillarGlyphSprite()
    {
        if (_pillarGlyphSprite != null)
            return _pillarGlyphSprite;

        string[] art =
        {
            "...w...",
            "..cwc..",
            ".cpwpc.",
            "..cwc..",
            "..pwp..",
            "..cwc..",
            ".cpwpc.",
            "..cwc..",
            "...c...",
        };

        int h = art.Length;
        int w = art[0].Length;
        var texture = new Texture2D(w, h, TextureFormat.RGBA32, false);
        var pixels = new Color32[w * h];
        for (int y = 0; y < h; y++)
        {
            string row = art[h - 1 - y];
            for (int x = 0; x < w; x++)
            {
                char ch = x < row.Length ? row[x] : '.';
                pixels[y * w + x] = ch switch
                {
                    'w' => new Color32(230, 252, 255, 255),
                    'c' => new Color32(107, 247, 255, 255),
                    'p' => new Color32(168, 96, 255, 255),
                    _ => new Color32(0, 0, 0, 0),
                };
            }
        }
        texture.SetPixels32(pixels);
        texture.Apply();
        texture.filterMode = FilterMode.Point;
        texture.wrapMode = TextureWrapMode.Clamp;
        _pillarGlyphSprite = Sprite.Create(texture, new Rect(0f, 0f, w, h), new Vector2(0.5f, 0.5f), 16f);
        return _pillarGlyphSprite;
    }
}
