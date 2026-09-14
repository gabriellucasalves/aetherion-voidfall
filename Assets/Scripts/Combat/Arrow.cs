using UnityEngine;

/// <summary>
/// Tiro básico do Arqueiro — flecha reta na facing.
/// Visual: ponta + haste + pena (pixel art Point) com rotação na direção do tiro.
/// Spawn sincronizado com o frame de soltar (draw→release) via <see cref="MuzzleDelay"/>.
/// Dano e velocidade vêm de <see cref="AbilityData.CreateFlecha"/>.
/// </summary>
public class Arrow : MonoBehaviour
{
    // Atraso: draw hold (13×2 @ ~12 fps) → soltar no frame 14 (~0.16–0.17s).
    const float MuzzleDelay = 0.16f;
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

    Vector2 _direction = Vector2.right;
    float _speed;
    float _lifetime;
    float _damage;
    float _age;
    float _trailClock;
    Color _fxColor;
    bool _armed;
    bool _hit;
    SpriteRenderer _renderer;
    Collider2D _hitbox;

    public static void Launch(
        Transform owner,
        Vector2 direction,
        AbilityData ability,
        float damage,
        Vector3 localOffset,
        Color color)
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
        arrow._fxColor = color;

        arrow._renderer = go.AddComponent<SpriteRenderer>();
        arrow._renderer.sprite = ArrowSprite();
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
        if (_hit)
            return;

        _age += Time.deltaTime;

        if (!_armed)
        {
            if (_age < MuzzleDelay)
                return;
            Arm();
        }

        transform.position += (Vector3)(_direction * _speed * Time.deltaTime);
        // Mantém a flecha alinhada à direção (rajada em leque / facing).
        transform.right = _direction;
        SpawnTrail();

        if (_age - MuzzleDelay >= _lifetime)
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

        var enemy = other.GetComponent<EnemyController>();
        if (enemy == null)
            enemy = other.GetComponentInParent<EnemyController>();
        if (enemy == null || enemy.Health == null || enemy.Health.IsDead)
            return;

        _hit = true;
        enemy.ReceiveDamage(_damage);
        PixelBurst.Spawn(transform.position, _fxColor, 6);
        PixelBurst.Spawn(transform.position, Color.Lerp(_fxColor, Color.white, 0.45f), 3);
        Destroy(gameObject);
    }

    static Sprite ArrowSprite()
    {
        if (_arrowSprite != null)
            return _arrowSprite;

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
