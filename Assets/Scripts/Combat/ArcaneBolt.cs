using UnityEngine;

/// <summary>
/// Tiro básico do Mago — Pulso Rúnico largo na facing (sem homing, 1 alvo, sem aplicar lentidão).
/// Visual: núcleo arcano azul-ciano + halo rúnico roxo/ciano giratório + rastro duplo + impacto forte.
/// Dano (100 * 0.52 = 52) e tamanho (0.90, 0.90) vêm de <see cref="AbilityData.CreateOrbe"/>.
/// </summary>
public class ArcaneBolt : MonoBehaviour
{
    // Atraso curto para alinhar o disparo com o thrust do cast (frames 14–15 @ ~12 fps).
    const float MuzzleDelay = 0.24f;
    const float TrailInterval = 0.038f;

    static readonly string[] BoltArt =
    {
        "....ccc....",
        "..ccwwwcc..",
        ".cbpwwwpbc.",
        "cbwwwwwwwbc",
        "cwwwwwwwwwc",
        "cbwwwwwwwbc",
        ".cbpwwwpbc.",
        "..ccwwwcc..",
        "....ccc....",
    };

    static readonly string[] HaloArt =
    {
        ".....ppp.....",
        "...ppcccpp...",
        "..pc.....cp..",
        ".pc..www..cp.",
        ".pc.w...w.cp.",
        "pc..w...w..cp",
        "pc..w...w..cp",
        "pc..w...w..cp",
        ".pc.w...w.cp.",
        ".pc..www..cp.",
        "..pc.....cp..",
        "...ppcccpp...",
        ".....ppp.....",
    };

    static Sprite _boltSprite;
    static Sprite _haloSprite;
    static Sprite[] _orbFrames;

    Vector2 _direction = Vector2.right;
    float _speed;
    float _lifetime;
    float _damage;
    public float Damage => _damage;
    float _age;
    float _trailClock;
    Color _fxColor;
    bool _armed;
    bool _hit;
    SpriteRenderer _renderer;
    SpriteRenderer _haloRenderer;
    Collider2D _hitbox;
    HealthSystem _ownerHealth;

    public static void Launch(Transform owner, Vector2 direction, AbilityData ability, float damage)
    {
        if (ability == null || owner == null)
            return;
        if (direction.sqrMagnitude < 0.01f)
            direction = Vector2.right;
        direction.Normalize();

        var go = new GameObject("OrbeArcano");
        go.transform.position = owner.position + (Vector3)direction * 0.60f + Vector3.up * 0.36f;

        var bolt = go.AddComponent<ArcaneBolt>();
        bolt._direction = direction;
        bolt._speed = ability.ProjectileSpeed;
        bolt._lifetime = ability.Lifetime;
        bolt._damage = damage;
        bolt._ownerHealth = owner.GetComponent<HealthSystem>();
        bolt._fxColor = ability.Color;

        bolt._renderer = go.AddComponent<SpriteRenderer>();
        bolt._renderer.sprite = BoltSprite();
        bolt._renderer.sortingOrder = 21;
        bolt._renderer.color = Color.white;
        bolt._renderer.enabled = false;

        // Halo rúnico externo (roxo/ciano) para dar presença de Pulso Rúnico largo
        var haloGo = new GameObject("PulsoHalo");
        haloGo.transform.SetParent(go.transform, false);
        haloGo.transform.localScale = new Vector3(1.28f, 1.28f, 1f);
        bolt._haloRenderer = haloGo.AddComponent<SpriteRenderer>();
        bolt._haloRenderer.sprite = HaloSprite();
        bolt._haloRenderer.sortingOrder = 20;
        bolt._haloRenderer.color = new Color(0.75f, 0.52f, 1f, 0.88f);
        bolt._haloRenderer.enabled = false;

        go.transform.localScale = new Vector3(
            ability.ProjectileSize.x,
            ability.ProjectileSize.y,
            1f);
        go.transform.right = direction;

        var body = go.AddComponent<Rigidbody2D>();
        body.bodyType = RigidbodyType2D.Kinematic;
        body.gravityScale = 0f;
        body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        body.interpolation = RigidbodyInterpolation2D.Interpolate;
        body.freezeRotation = true;

        var circle = go.AddComponent<CircleCollider2D>();
        circle.isTrigger = true;
        circle.radius = 0.46f;
        circle.enabled = false;
        bolt._hitbox = circle;

        var ownerCollider = owner.GetComponent<Collider2D>();
        if (ownerCollider != null)
            Physics2D.IgnoreCollision(circle, ownerCollider, true);
    }

    void Update()
    {
        if (_ownerHealth != null && _ownerHealth.IsDead) { Destroy(gameObject); return; }
        if (_hit)
            return;

        _age += Time.deltaTime;

        if (!_armed)
        {
            if (_age < MuzzleDelay)
                return;
            Arm();
        }

        Vector2 start = transform.position;
        float distance = _speed * Time.deltaTime;
        var hits = Physics2D.RaycastAll(start, _direction, distance + 0.12f);
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
        foreach (var hit in hits)
        {
            if (hit.collider == null || hit.collider == _hitbox) continue;
            if (EnemyTraversal.IsSurface(hit.collider))
            {
                Impact(hit.point);
                return;
            }
            TryHit(hit.collider);
            if (_hit) return;
        }
        transform.position += (Vector3)(_direction * distance);
        if (_orbFrames != null)
            _renderer.sprite = _orbFrames[Mathf.FloorToInt(_age * 14f) % _orbFrames.Length];
        if (_haloRenderer != null)
        {
            _haloRenderer.transform.localRotation = Quaternion.Euler(0f, 0f, _age * 220f);
            float pulse = 1.20f + Mathf.Sin(_age * 24f) * 0.14f;
            _haloRenderer.transform.localScale = new Vector3(pulse, pulse, 1f);
        }
        SpawnTrail();

        if (_age - MuzzleDelay >= _lifetime)
            Destroy(gameObject);
    }

    void Arm()
    {
        _armed = true;
        if (_renderer != null)
            _renderer.enabled = true;
        if (_haloRenderer != null)
            _haloRenderer.enabled = true;
        if (_hitbox != null)
            _hitbox.enabled = true;
        PixelBurst.Spawn(transform.position, _fxColor, 5);
        PixelBurst.Spawn(transform.position, new Color(0.72f, 0.45f, 1f), 3);
    }

    void SpawnTrail()
    {
        _trailClock += Time.deltaTime;
        if (_trailClock < TrailInterval)
            return;
        _trailClock = 0f;

        Vector3 back = transform.position - (Vector3)(_direction * 0.22f);
        PixelBurst.Spawn(back, Color.Lerp(_fxColor, Color.white, 0.35f), 2);
        PixelBurst.Spawn(back - (Vector3)(_direction * 0.10f), new Color(0.72f, 0.45f, 1f, 0.85f), 1);
    }

    void OnTriggerEnter2D(Collider2D other) => TryHit(other);

    void OnTriggerStay2D(Collider2D other) => TryHit(other);

    void TryHit(Collider2D other)
    {
        if (_hit || !_armed || other == null)
            return;

        if (EnemyTraversal.IsSurface(other))
        {
            Impact(transform.position);
            return;
        }

        var enemy = other.GetComponent<EnemyController>();
        if (enemy == null)
            enemy = other.GetComponentInParent<EnemyController>();
        if (enemy == null || enemy.Health == null || enemy.Health.IsDead)
            return;

        // Pulso Rúnico básico: alto impacto (52), SEM aplicar lentidão (lentidão é exclusiva da Zona Rúnica).
        enemy.ReceiveDamage(_damage);
        Impact(transform.position);
    }

    void Impact(Vector2 at)
    {
        if (_hit)
            return;
        _hit = true;
        TrioVfxPlayer.Spawn("orbe_arcano_impacto", at, 24, 24, 5, new Vector2(.5f, .5f), 18f, 1.35f);
        TrioVfxPlayer.Spawn("pulso_runico", at, 64, 24, 7, new Vector2(.5f, 16f / 24f), 22f, 0.95f);
        PixelBurst.Spawn(at, _fxColor, 8);
        PixelBurst.Spawn(at, new Color(0.72f, 0.45f, 1f), 6);
        PixelBurst.Spawn(at, Color.Lerp(_fxColor, Color.white, 0.55f), 4);
        Destroy(gameObject);
    }

    static Sprite BoltSprite()
    {
        if (_boltSprite != null)
            return _boltSprite;

        _orbFrames = TrioAnimationFrames.Vfx("orbe_arcano_loop", 16, 16, 4, new Vector2(9f / 16f, .5f));
        if (_orbFrames != null) return _boltSprite = _orbFrames[0];

        int height = BoltArt.Length;
        int width = BoltArt[0].Length;
        var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
        var pixels = new Color32[width * height];
        for (int y = 0; y < height; y++)
        {
            string row = BoltArt[height - 1 - y];
            for (int x = 0; x < width; x++)
            {
                char ch = x < row.Length ? row[x] : '.';
                pixels[y * width + x] = ch switch
                {
                    'w' => new Color32(230, 252, 255, 255),
                    'c' => new Color32(90, 215, 255, 255),
                    'b' => new Color32(48, 128, 230, 255),
                    'p' => new Color32(168, 96, 255, 255),
                    _ => new Color32(0, 0, 0, 0),
                };
            }
        }

        texture.SetPixels32(pixels);
        texture.Apply();
        texture.filterMode = FilterMode.Point;
        texture.wrapMode = TextureWrapMode.Clamp;
        _boltSprite = Sprite.Create(
            texture,
            new Rect(0f, 0f, width, height),
            new Vector2(0.5f, 0.5f),
            14f,
            0,
            SpriteMeshType.FullRect);
        return _boltSprite;
    }

    static Sprite HaloSprite()
    {
        if (_haloSprite != null)
            return _haloSprite;

        int height = HaloArt.Length;
        int width = HaloArt[0].Length;
        var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
        var pixels = new Color32[width * height];
        for (int y = 0; y < height; y++)
        {
            string row = HaloArt[height - 1 - y];
            for (int x = 0; x < width; x++)
            {
                char ch = x < row.Length ? row[x] : '.';
                pixels[y * width + x] = ch switch
                {
                    'w' => new Color32(220, 250, 255, 220),
                    'c' => new Color32(90, 215, 255, 235),
                    'p' => new Color32(170, 96, 255, 235),
                    _ => new Color32(0, 0, 0, 0),
                };
            }
        }

        texture.SetPixels32(pixels);
        texture.Apply();
        texture.filterMode = FilterMode.Point;
        texture.wrapMode = TextureWrapMode.Clamp;
        _haloSprite = Sprite.Create(
            texture,
            new Rect(0f, 0f, width, height),
            new Vector2(0.5f, 0.5f),
            16f,
            0,
            SpriteMeshType.FullRect);
        return _haloSprite;
    }
}
