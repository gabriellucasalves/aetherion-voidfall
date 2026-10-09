using UnityEngine;

// Flecha de luz do Sete Estrelas. Curva inicial larga, depois persegue o alvo.
public class HomingStarArrow : MonoBehaviour
{
    public static int Alive { get; private set; }

    const float Speed = 16f;
    const float Life = 1.8f;

    static Sprite _sprite;

    EnemyController _target;
    Vector2 _direction = Vector2.up;
    float _damage;
    float _age;
    float _trail;
    bool _hit;

    public static void Launch(Vector3 position, EnemyController target, float damage, int index)
    {
        var go = new GameObject("FlechaEstrela");
        go.transform.position = position;
        var arrow = go.AddComponent<HomingStarArrow>();
        arrow._target = target;
        arrow._damage = damage;

        float spread = (index - 3) * 12f;
        float circus = index % 2 == 0 ? 48f : -48f;
        arrow._direction = Rotate(Vector2.up, spread * 0.45f + circus).normalized;

        var renderer = go.AddComponent<SpriteRenderer>();
        renderer.sprite = ArrowSprite();
        renderer.sortingOrder = 20;
        renderer.color = new Color(1f, 0.93f, 0.65f, 1f);
        go.transform.localScale = new Vector3(1.15f, 0.7f, 1f);
        go.transform.right = arrow._direction;

        var body = go.AddComponent<Rigidbody2D>();
        body.bodyType = RigidbodyType2D.Kinematic;
        body.gravityScale = 0f;
        body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        var box = go.AddComponent<BoxCollider2D>();
        box.isTrigger = true;
        box.size = new Vector2(0.85f, 0.28f);
    }

    void Awake()
    {
        Alive++;
    }

    void OnDestroy()
    {
        Alive = Mathf.Max(0, Alive - 1);
    }

    void Update()
    {
        if (_hit)
            return;

        _age += Time.deltaTime;
        Steer();
        transform.position += (Vector3)(_direction * Speed * Time.deltaTime);
        transform.right = _direction;
        Trail();

        if (_age >= Life)
            Destroy(gameObject);
    }

    void Steer()
    {
        if (_target == null || _target.Health == null || _target.Health.IsDead)
            return;

        Vector2 desired = (Vector2)_target.transform.position + Vector2.up * 0.7f - (Vector2)transform.position;
        if (desired.sqrMagnitude < 0.0001f)
            return;

        desired.Normalize();
        float t = 1f - Mathf.Exp(-8f * Time.deltaTime);
        _direction = Vector2.Lerp(_direction, desired, t).normalized;
    }

    void Trail()
    {
        _trail += Time.deltaTime;
        if (_trail < 0.07f)
            return;
        _trail = 0f;
        PixelBurst.Spawn(transform.position, new Color(1f, 0.84f, 0.35f), 1);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (_hit || other == null)
            return;

        var enemy = other.GetComponent<EnemyController>();
        if (enemy == null)
            enemy = other.GetComponentInParent<EnemyController>();
        if (enemy == null || enemy.Health == null || enemy.Health.IsDead)
            return;
        if (_target != null && enemy != _target)
            return;

        _hit = true;
        enemy.ReceiveDamage(_damage);
        enemy.AddVitalMark();
        PixelBurst.Spawn(transform.position, new Color(1f, 0.9f, 0.45f), 4);
        Destroy(gameObject);
    }

    static Vector2 Rotate(Vector2 value, float degrees)
    {
        float rad = degrees * Mathf.Deg2Rad;
        float sin = Mathf.Sin(rad);
        float cos = Mathf.Cos(rad);
        return new Vector2(value.x * cos - value.y * sin, value.x * sin + value.y * cos);
    }

    static Sprite ArrowSprite()
    {
        if (_sprite != null)
            return _sprite;

        string[] art =
        {
            "................",
            ".....k..........",
            "....kmk.........",
            "...kmmmk........",
            "kkwwwwwwwwyyyk..",
            "...kmmmk........",
            "....kmk.........",
            ".....k..........",
            "................",
        };
        _sprite = SupremeFx.ArtSprite(art, ch => ch switch
        {
            'k' => new Color32(40, 24, 8, 255),
            'm' => new Color32(255, 248, 220, 255),
            'w' => new Color32(255, 214, 90, 255),
            'y' => new Color32(110, 240, 180, 255),
            _ => new Color32(0, 0, 0, 0),
        });
        return _sprite;
    }
}
