using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Tiro do Arqueiro Arcano — 3 níveis de flecha carregada (N1, N2, N3) + Especial Perfurante (pierce = 3).
/// No tiro carregado básico, nasce somente no instante de soltar (muzzleDelay = 0f), sem acumular projéteis invisíveis.
/// No especial perfurante, mantém SpecialMuzzleDelay sincronizado com a animação de disparo especial.
/// </summary>
public class Arrow : MonoBehaviour
{
    public const float BasicMuzzleDelay = 0f;
    public const float SpecialMuzzleDelay = 0.44f;
    const float DefaultTrailInterval = 0.055f;
    const float MaxChargeTrailInterval = 0.028f;

    // Fallback procedural em estilo Arqueiro Arcano (cristal ciano + haste dourada)
    static readonly string[] ArrowArt =
    {
        "......................",
        "......k...............",
        ".....kck..............",
        "....kccck.............",
        "...kccccck............",
        "kkkkwwwwwwwwwwwaaoccck",
        "...kccccck............",
        "....kccck.............",
        ".....kck..............",
        "......k...............",
        "......................",
    };

    static Sprite _arrowSprite;
    static Sprite _arrowTier1Sprite;
    static Sprite _arrowTier2Sprite;
    static Sprite _arrowTier3Sprite;
    static Sprite[] _piercingFrames;

    Vector2 _direction = Vector2.right;
    float _speed;
    float _lifetime;
    float _damage;
    public float Damage => _damage;
    float _age;
    float _trailClock;
    Color _fxColor;
    float _muzzleDelay;
    int _chargeTier = 2;
    bool _armed;
    bool _hit;
    int _hitsRemaining = 1;
    readonly HashSet<EnemyController> _hitEnemies = new HashSet<EnemyController>();
    SpriteRenderer _renderer;
    Collider2D _hitbox;
    HealthSystem _ownerHealth;

    public static void Launch(
        Transform owner,
        Vector2 direction,
        AbilityData ability,
        float damage,
        Vector3 localOffset,
        Color color,
        float muzzleDelay = -1f,
        int pierce = 1,
        int chargeTier = 2,
        float speedOverride = -1f,
        float lifetimeOverride = -1f)
    {
        if (ability == null || owner == null)
            return;
        if (direction.sqrMagnitude < 0.01f)
            direction = Vector2.right;
        direction.Normalize();

        var go = new GameObject("Flecha");
        go.transform.position = owner.position
            + (Vector3)direction * 0.55f
            + Vector3.up * 0.32f
            + localOffset;

        var arrow = go.AddComponent<Arrow>();
        arrow._direction = direction;
        arrow._chargeTier = Mathf.Clamp(chargeTier, 1, 3);
        arrow._speed = speedOverride > 0f ? speedOverride : ability.ProjectileSpeed;
        arrow._lifetime = lifetimeOverride > 0f ? lifetimeOverride : ability.Lifetime;
        arrow._damage = damage;
        arrow._ownerHealth = owner.GetComponent<HealthSystem>();
        arrow._fxColor = color;
        arrow._muzzleDelay = muzzleDelay >= 0f ? muzzleDelay : BasicMuzzleDelay;
        arrow._hitsRemaining = Mathf.Clamp(pierce, 1, 3);

        arrow._renderer = go.AddComponent<SpriteRenderer>();
        if (pierce > 1)
        {
            _piercingFrames ??= TrioAnimationFrames.Vfx("perfurante_disparo", 40, 9, 3, new Vector2(39f / 40f, 4f / 9f));
            arrow._renderer.sprite = _piercingFrames != null ? _piercingFrames[0] : ArrowSpriteForTier(3);
            arrow._renderer.color = color;
        }
        else
        {
            arrow._renderer.sprite = ArrowSpriteForTier(arrow._chargeTier);
            arrow._renderer.color = Color.white;
        }
        arrow._renderer.sortingOrder = 20;
        arrow._renderer.enabled = false;

        float scaleMult = pierce > 1 ? 1f : (arrow._chargeTier == 1 ? 0.92f : (arrow._chargeTier == 3 ? 1.12f : 1.0f));
        go.transform.localScale = new Vector3(
            ability.ProjectileSize.x * scaleMult,
            ability.ProjectileSize.y * scaleMult,
            1f);
        go.transform.right = direction;

        var body = go.AddComponent<Rigidbody2D>();
        body.bodyType = RigidbodyType2D.Kinematic;
        body.gravityScale = 0f;
        body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        body.interpolation = RigidbodyInterpolation2D.Interpolate;
        body.freezeRotation = true;

        var box = go.AddComponent<BoxCollider2D>();
        box.isTrigger = true;
        box.size = pierce > 1
            ? new Vector2(1.10f, 0.32f)
            : (arrow._chargeTier == 1
                ? new Vector2(0.76f, 0.22f)
                : (arrow._chargeTier == 3 ? new Vector2(1.16f, 0.34f) : new Vector2(0.95f, 0.28f)));
        box.enabled = false;
        arrow._hitbox = box;

        var ownerCollider = owner.GetComponent<Collider2D>();
        if (ownerCollider != null)
            Physics2D.IgnoreCollision(box, ownerCollider, true);

        // No tiro básico carregado (muzzleDelay == 0), arma no próprio frame de soltar!
        if (arrow._muzzleDelay <= 0f)
            arrow.Arm();
    }

    void Update()
    {
        if (_ownerHealth != null && _ownerHealth.IsDead) { Destroy(gameObject); return; }
        if (_hit)
            return;

        _age += Time.deltaTime;

        if (!_armed)
        {
            if (_age < _muzzleDelay)
                return;
            Arm();
        }

        float activeTime = _age - _muzzleDelay;
        // Nível 1 (< 0.25s): leve queda balística na segunda metade da trajetória curta
        if (_hitsRemaining == 1 && _chargeTier == 1 && _lifetime > 0.01f)
        {
            float progress = Mathf.Clamp01(activeTime / _lifetime);
            if (progress > 0.42f)
            {
                _direction.y -= 1.55f * Time.deltaTime;
                _direction.Normalize();
            }
        }

        Vector2 start = transform.position;
        float distance = _speed * Time.deltaTime;
        var hits = Physics2D.RaycastAll(start, _direction, distance + 0.1f);
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
        transform.right = _direction;
        if (_hitsRemaining > 1 && _piercingFrames != null)
            _renderer.sprite = _piercingFrames[Mathf.FloorToInt(_age * 16f) % _piercingFrames.Length];
        SpawnTrail();

        if (activeTime >= _lifetime)
            Destroy(gameObject);
    }

    void Arm()
    {
        if (_armed)
            return;
        _armed = true;
        if (_renderer != null)
            _renderer.enabled = true;
        if (_hitbox != null)
            _hitbox.enabled = true;
        int burstCount = _chargeTier >= 3 ? 6 : (_chargeTier == 2 ? 4 : 2);
        PixelBurst.Spawn(transform.position, _fxColor, burstCount);
    }

    void SpawnTrail()
    {
        if (_chargeTier == 1 && _hitsRemaining == 1)
            return; // Nível 1 é um traço curto sem rastro longo

        float interval = (_chargeTier >= 3 || _hitsRemaining > 1) ? MaxChargeTrailInterval : DefaultTrailInterval;
        _trailClock += Time.deltaTime;
        if (_trailClock < interval)
            return;
        _trailClock = 0f;

        Vector3 back = transform.position - (Vector3)(_direction * 0.24f);
        if (_chargeTier >= 3 && _hitsRemaining == 1)
        {
            // Rastro mágico em pixel art (partículas ciano e ouro) na carga máxima (N3)
            PixelBurst.Spawn(back, new Color(0.42f, 0.97f, 1f), 2);
            PixelBurst.Spawn(back - (Vector3)(_direction * 0.14f), new Color(1f, 0.95f, 0.62f), 1);
        }
        else
        {
            PixelBurst.Spawn(back, Color.Lerp(_fxColor, Color.white, 0.25f), 1);
        }
    }

    void OnTriggerEnter2D(Collider2D other) => TryHit(other);

    void OnTriggerStay2D(Collider2D other) => TryHit(other);

    void TryHit(Collider2D other)
    {
        if (_hit || !_armed || other == null)
            return;

        if (EnemyTraversal.IsSurface(other)) { Impact(transform.position); return; }

        var enemy = other.GetComponent<EnemyController>();
        if (enemy == null)
            enemy = other.GetComponentInParent<EnemyController>();
        if (enemy == null || enemy.Health == null || enemy.Health.IsDead || !_hitEnemies.Add(enemy))
            return;

        enemy.ReceiveDamage(_damage);
        if (_hitsRemaining > 1)
        {
            TrioVfxPlayer.Spawn("perfurante_impacto", transform.position, 14, 14, 4,
                new Vector2(.5f, .5f), 18f);
        }
        else
        {
            SpawnTierImpactVfx(transform.position);
        }

        int hitParticles = _chargeTier >= 3 ? 9 : (_chargeTier == 2 ? 6 : 4);
        PixelBurst.Spawn(transform.position, _fxColor, hitParticles);
        PixelBurst.Spawn(transform.position, Color.Lerp(_fxColor, Color.white, 0.45f), 3);
        if (--_hitsRemaining <= 0) Impact(transform.position);
    }

    void SpawnTierImpactVfx(Vector2 at)
    {
        if (_chargeTier == 1)
            TrioVfxPlayer.Spawn("flecha_impacto_n1", at, 12, 12, 4, new Vector2(0.5f, 0.5f), 18f);
        else if (_chargeTier == 2)
            TrioVfxPlayer.Spawn("flecha_impacto_n2", at, 18, 18, 4, new Vector2(0.5f, 0.5f), 18f);
        else
            TrioVfxPlayer.Spawn("flecha_impacto_n3", at, 24, 24, 4, new Vector2(0.5f, 0.5f), 16f);
    }

    void Impact(Vector2 at)
    {
        if (_hit) return;
        _hit = true;
        if (_hitEnemies.Count == 0)
        {
            SpawnTierImpactVfx(at);
            TrioVfxPlayer.Spawn("flecha_cravada", at, 12, 7, 3,
                new Vector2(9f / 12f, 3f / 7f), 8f);
        }
        PixelBurst.Spawn(at, _fxColor, _chargeTier >= 3 ? 8 : 5);
        Destroy(gameObject);
    }

    static Sprite ArrowSpriteForTier(int tier)
    {
        if (tier <= 1)
        {
            if (_arrowTier1Sprite != null) return _arrowTier1Sprite;
            var s1 = TrioAnimationFrames.Vfx("flecha_n1", 16, 7, 1, new Vector2(14f / 16f, 3f / 7f));
            if (s1 != null && s1.Length > 0) return _arrowTier1Sprite = s1[0];
        }
        else if (tier == 2)
        {
            if (_arrowTier2Sprite != null) return _arrowTier2Sprite;
            var s2 = TrioAnimationFrames.Vfx("flecha_n2", 20, 9, 1, new Vector2(17f / 20f, 4f / 9f));
            if (s2 != null && s2.Length > 0) return _arrowTier2Sprite = s2[0];
        }
        else
        {
            if (_arrowTier3Sprite != null) return _arrowTier3Sprite;
            var s3 = TrioAnimationFrames.Vfx("flecha_n3", 24, 11, 1, new Vector2(21f / 24f, 5f / 11f));
            if (s3 != null && s3.Length > 0) return _arrowTier3Sprite = s3[0];
        }
        return ArrowSprite();
    }

    static Sprite ArrowSprite()
    {
        if (_arrowSprite != null)
            return _arrowSprite;

        var art = TrioAnimationFrames.Vfx("flecha", 16, 7, 1, new Vector2(14f / 16f, 3f / 7f));
        if (art != null) return _arrowSprite = art[0];

        int height = ArrowArt.Length;
        int width = ArrowArt[0].Length;
        var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
        var pixels = new Color32[width * height];
        for (int y = 0; y < height; y++)
        {
            string row = ArrowArt[height - 1 - y];
            for (int x = 0; x < width; x++)
            {
                char ch = x < row.Length ? row[x] : '.';
                pixels[y * width + x] = ch switch
                {
                    'k' => new Color32(18, 16, 32, 255),
                    'c' => new Color32(107, 247, 255, 255),
                    'w' => new Color32(214, 154, 58, 255),
                    'a' => new Color32(248, 206, 92, 255),
                    'o' => new Color32(230, 255, 255, 255),
                    _ => new Color32(0, 0, 0, 0),
                };
            }
        }

        texture.SetPixels32(pixels);
        texture.Apply();
        texture.filterMode = FilterMode.Point;
        texture.wrapMode = TextureWrapMode.Clamp;
        _arrowSprite = Sprite.Create(
            texture,
            new Rect(0f, 0f, width, height),
            new Vector2(0.35f, 0.5f),
            16f,
            0,
            SpriteMeshType.FullRect);
        return _arrowSprite;
    }
}
