using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Especial do Guerreiro — Lâmina / Onda de Energia: projétil horizontal na facing.
/// Distinto do Corte melee. CD fica em PlayerCombat.WarriorSpecialCooldown.
/// </summary>
public class SwordWave : MonoBehaviour
{
    const int MaxHits = 2;

    static readonly string[] WaveArt =
    {
        "................",
        ".....oyy........",
        "....oyyyy.......",
        "...oyyyyyy......",
        "..oyyyyyyyy.....",
        ".oyyyyyyyyyy....",
        "oyyyyyyyyyyyy...",
        "oyyyyyyyyyyyyy..",
        "oyyyyyyyyyyyyyw.",
        "oyyyyyyyyyyyyyww",
        "oyyyyyyyyyyyyyw.",
        "oyyyyyyyyyyyyy..",
        "oyyyyyyyyyyyy...",
        ".oyyyyyyyyyy....",
        "..oyyyyyyyy.....",
        "...oyyyyyy......",
        "....oyyyy.......",
        ".....oyy........",
        "................",
    };

    static Sprite _waveSprite;

    Vector2 _direction;
    float _speed;
    float _lifetime;
    float _damage;
    float _age;
    Color _fxColor;
    SpriteRenderer _renderer;
    readonly HashSet<int> _hitIds = new HashSet<int>();

    public static void Launch(Transform owner, Vector2 direction, AbilityData ability, float damage)
    {
        if (ability == null)
            return;
        if (direction.sqrMagnitude < 0.01f)
            direction = Vector2.right;
        direction.Normalize();

        var go = new GameObject("OndaEspada");
        float facingSign = Mathf.Sign(direction.x == 0f ? 1f : direction.x);
        go.transform.position = owner.position + new Vector3(facingSign * 0.85f, 0.55f, 0f);

        var wave = go.AddComponent<SwordWave>();
        wave._direction = direction;
        wave._speed = ability.ProjectileSpeed;
        wave._lifetime = ability.Lifetime;
        wave._damage = damage;
        wave._fxColor = ability.Color;

        wave._renderer = go.AddComponent<SpriteRenderer>();
        wave._renderer.sprite = WaveSprite();
        wave._renderer.sortingOrder = 21;
        wave._renderer.color = ability.Color;
        go.transform.localScale = new Vector3(
            ability.ProjectileSize.x * facingSign,
            ability.ProjectileSize.y,
            1f);

        var body = go.AddComponent<Rigidbody2D>();
        body.bodyType = RigidbodyType2D.Kinematic;
        body.gravityScale = 0f;
        body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        body.freezeRotation = true;

        var collider = go.AddComponent<BoxCollider2D>();
        collider.isTrigger = true;
        collider.size = Vector2.one;

        var ownerCollider = owner.GetComponent<Collider2D>();
        if (ownerCollider != null)
            Physics2D.IgnoreCollision(collider, ownerCollider, true);

        PixelBurst.Spawn(go.transform.position, ability.Color, 6);
    }

    void Update()
    {
        transform.position += (Vector3)(_direction * _speed * Time.deltaTime);
        _age += Time.deltaTime;

        if (_renderer != null)
        {
            var color = _renderer.color;
            color.a = Mathf.Lerp(1f, 0.2f, _age / Mathf.Max(0.05f, _lifetime));
            _renderer.color = color;
        }

        if (_age >= _lifetime)
            Destroy(gameObject);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        Hit(other);
    }

    void OnTriggerStay2D(Collider2D other)
    {
        Hit(other);
    }

    void Hit(Collider2D other)
    {
        if (_damage <= 0f || _hitIds.Count >= MaxHits)
            return;

        var enemy = other.GetComponent<EnemyController>();
        if (enemy == null)
            enemy = other.GetComponentInParent<EnemyController>();
        if (enemy == null || enemy.Health == null || enemy.Health.IsDead)
            return;

        int id = enemy.GetInstanceID();
        if (!_hitIds.Add(id))
            return;

        enemy.ReceiveDamage(_damage);
        PixelBurst.Spawn(other.bounds.center, _fxColor, 6);

        if (_hitIds.Count >= MaxHits)
            Destroy(gameObject);
    }

    static Sprite WaveSprite()
    {
        if (_waveSprite != null)
            return _waveSprite;

        int height = WaveArt.Length;
        int width = WaveArt[0].Length;
        var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
        var pixels = new Color32[width * height];
        for (int y = 0; y < height; y++)
        {
            string row = WaveArt[height - 1 - y];
            for (int x = 0; x < width; x++)
            {
                char ch = x < row.Length ? row[x] : '.';
                pixels[y * width + x] = ch switch
                {
                    'w' => new Color32(255, 248, 210, 255),
                    'y' => new Color32(255, 214, 64, 255),
                    'o' => new Color32(255, 120, 24, 255),
                    _ => new Color32(0, 0, 0, 0),
                };
            }
        }

        texture.SetPixels32(pixels);
        texture.Apply();
        texture.filterMode = FilterMode.Point;
        texture.wrapMode = TextureWrapMode.Clamp;
        _waveSprite = Sprite.Create(
            texture,
            new Rect(0f, 0f, width, height),
            new Vector2(0.35f, 0.5f),
            14f,
            0,
            SpriteMeshType.FullRect);
        return _waveSprite;
    }
}
