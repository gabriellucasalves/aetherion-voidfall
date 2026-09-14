using UnityEngine;

/// <summary>
/// Tiro básico do Mago — projétil reto na facing (sem homing).
/// Visual: cristal/orbe arcano azul-ciano pixel art + rastro curto + impacto.
/// Dano e velocidade vêm de <see cref="AbilityData.CreateOrbe"/>.
/// </summary>
public class ArcaneBolt : MonoBehaviour
{
    // Atraso curto para alinhar o disparo com o thrust do cast (frames 14–15 @ ~12 fps).
    const float MuzzleDelay = 0.08f;
    const float TrailInterval = 0.045f;

    static readonly string[] BoltArt =
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

    static Sprite _boltSprite;

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

    public static void Launch(Transform owner, Vector2 direction, AbilityData ability, float damage)
    {
        if (ability == null || owner == null)
            return;
        if (direction.sqrMagnitude < 0.01f)
            direction = Vector2.right;
        direction.Normalize();

        var go = new GameObject("OrbeArcano");
        go.transform.position = owner.position + (Vector3)direction * 0.55f + Vector3.up * 0.35f;

        var bolt = go.AddComponent<ArcaneBolt>();
        bolt._direction = direction;
        bolt._speed = ability.ProjectileSpeed;
        bolt._lifetime = ability.Lifetime;
        bolt._damage = damage;
        bolt._fxColor = ability.Color;

        bolt._renderer = go.AddComponent<SpriteRenderer>();
        bolt._renderer.sprite = BoltSprite();
        bolt._renderer.sortingOrder = 20;
        bolt._renderer.color = ability.Color;
        bolt._renderer.enabled = false; // aparece no muzzle delay (sync cast)
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
        circle.radius = 0.42f;
        circle.enabled = false;
        bolt._hitbox = circle;

        var ownerCollider = owner.GetComponent<Collider2D>();
        if (ownerCollider != null)
            Physics2D.IgnoreCollision(circle, ownerCollider, true);
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

        // Rastro 2–3px: partículas mínimas atrás do orbe (Point / PixelBurst).
        Vector3 back = transform.position - (Vector3)(_direction * 0.18f);
        PixelBurst.Spawn(back, Color.Lerp(_fxColor, Color.white, 0.35f), 1);
        PixelBurst.Spawn(back + (Vector3)(_direction * -0.06f), new Color(0.35f, 0.85f, 1f, 0.7f), 1);
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
        PixelBurst.Spawn(transform.position, _fxColor, 5);
        PixelBurst.Spawn(transform.position, Color.Lerp(_fxColor, Color.white, 0.5f), 3);
        Destroy(gameObject);
    }

    static Sprite BoltSprite()
    {
        if (_boltSprite != null)
            return _boltSprite;

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
                    'w' => new Color32(220, 250, 255, 255),
                    'c' => new Color32(90, 210, 255, 255),
                    'b' => new Color32(40, 120, 220, 255),
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
}
