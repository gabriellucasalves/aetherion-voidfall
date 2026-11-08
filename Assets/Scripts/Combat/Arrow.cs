using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Tiro básico do Arqueiro — flecha reta na facing.
/// Visual: ponta + haste + pena (pixel art Point) com rotação na direção do tiro.
/// Spawn sincronizado com o frame de soltar (draw→release) via <see cref="BasicMuzzleDelay"/>.
/// Dano e velocidade vêm de <see cref="AbilityData.CreateFlecha"/>.
/// </summary>
public class Arrow : MonoBehaviour
{
    // Básico: draw hold (13×2 @ ~12 fps) → soltar no frame 14 (~0.16s).
    // Especial: delay maior sync com frame 18 (leque) — ver Launch(muzzleDelay).
    public const float BasicMuzzleDelay = 0.27f;
    public const float SpecialMuzzleDelay = 0.44f;
    const float TrailInterval = 0.055f;

    // Flecha horizontal apontando para +X (transform.right = direção do tiro).
    // m = ponta de aço · w/a = haste · o/f = pena · k = outline · y = brilho
    static readonly string[] ArrowArt =
    {
        "......................",
        "......k...............",
        ".....kmk..............",
        "....kmmmk.............",
        "...kmmmmmk............",
        "kkkkwwwwwwwwwwwaoofffk",
        "...kmmmmmk............",
        "....kmmmk.............",
        ".....kmk..............",
        "......k...............",
        "......................",
    };

    static Sprite _arrowSprite;
    static Sprite[] _piercingFrames;

    Vector2 _direction = Vector2.right;
    float _speed;
    float _lifetime;
    float _damage;
    float _age;
    float _trailClock;
    Color _fxColor;
    float _muzzleDelay = BasicMuzzleDelay;
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
        int pierce = 1)
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
        arrow._speed = ability.ProjectileSpeed;
        arrow._lifetime = ability.Lifetime;
        arrow._damage = damage;
        arrow._ownerHealth = owner.GetComponent<HealthSystem>();
        arrow._fxColor = color;
        arrow._muzzleDelay = muzzleDelay >= 0f ? muzzleDelay : BasicMuzzleDelay;
        arrow._hitsRemaining = Mathf.Clamp(pierce, 1, 3);

        arrow._renderer = go.AddComponent<SpriteRenderer>();
        if (pierce > 1)
            _piercingFrames ??= TrioAnimationFrames.Vfx("perfurante_disparo", 40, 9, 3, new Vector2(39f / 40f, 4f / 9f));
        arrow._renderer.sprite = pierce > 1 && _piercingFrames != null ? _piercingFrames[0] : ArrowSprite();
        arrow._renderer.sortingOrder = 20;
        arrow._renderer.color = color;
        arrow._renderer.enabled = false; // aparece no muzzle delay (sync soltar)
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

        var box = go.AddComponent<BoxCollider2D>();
        box.isTrigger = true;
        box.size = new Vector2(0.95f, 0.28f);
        box.enabled = false;
        arrow._hitbox = box;

        var ownerCollider = owner.GetComponent<Collider2D>();
        if (ownerCollider != null)
            Physics2D.IgnoreCollision(box, ownerCollider, true);
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
        // Mantém a flecha alinhada à direção (rajada em leque / facing).
        transform.right = _direction;
        if (_hitsRemaining > 1 && _piercingFrames != null)
            _renderer.sprite = _piercingFrames[Mathf.FloorToInt(_age * 16f) % _piercingFrames.Length];
        SpawnTrail();

        if (_age - _muzzleDelay >= _lifetime)
            Destroy(gameObject);
    }

    void Arm()
    {
        _armed = true;
        if (_renderer != null)
            _renderer.enabled = true;
        if (_hitbox != null)
            _hitbox.enabled = true;
        PixelBurst.Spawn(transform.position, _fxColor, 3);
    }

    void SpawnTrail()
    {
        _trailClock += Time.deltaTime;
        if (_trailClock < TrailInterval)
            return;
        _trailClock = 0f;

        Vector3 back = transform.position - (Vector3)(_direction * 0.22f);
        PixelBurst.Spawn(back, Color.Lerp(_fxColor, Color.white, 0.25f), 1);
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
            TrioVfxPlayer.Spawn("perfurante_impacto", transform.position, 14, 14, 4,
                new Vector2(.5f, .5f), 18f);
        PixelBurst.Spawn(transform.position, _fxColor, 6);
        PixelBurst.Spawn(transform.position, Color.Lerp(_fxColor, Color.white, 0.45f), 3);
        if (--_hitsRemaining <= 0) Impact(transform.position);
    }

    void Impact(Vector2 at)
    {
        if (_hit) return;
        _hit = true;
        if (_hitEnemies.Count == 0)
            TrioVfxPlayer.Spawn("flecha_cravada", at, 12, 7, 3,
                new Vector2(9f / 12f, 3f / 7f), 8f);
        PixelBurst.Spawn(at, _fxColor, 5);
        Destroy(gameObject);
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
                    'k' => new Color32(18, 12, 8, 255),
                    'm' => new Color32(220, 230, 240, 255),
                    'w' => new Color32(196, 148, 72, 255),
                    'a' => new Color32(230, 190, 110, 255),
                    'o' => new Color32(90, 230, 140, 255),
                    'f' => new Color32(48, 158, 92, 255),
                    'y' => new Color32(255, 230, 120, 255),
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
