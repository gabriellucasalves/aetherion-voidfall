using System.Collections.Generic;
using UnityEngine;

public class MeleeSlash : MonoBehaviour
{
    // Atraso para alinhar o hit com os frames 14–16 do Guerreiro (~12 fps).
    const float ActiveDelay = 0.06f;

    static readonly string[] SlashArt =
    {
        "..oyy..........",
        "..oyyyy........",
        "...oyyyy.......",
        "....oyyyy......",
        ".....oyyyy.....",
        "......oyyyy....",
        "......oyyyyy...",
        ".......oyyyyw..",
        ".......oyyyyww.",
        ".......oyyyyww.",
        ".......oyyyyw..",
        "......oyyyyy...",
        "......oyyyy....",
        ".....oyyyy.....",
        "....oyyyy......",
        "...oyyyy.......",
        "..oyyyy........",
        "..oyy..........",
    };

    static Sprite _slashSprite;

    float _damage;
    float _life;
    float _age;
    Color _fxColor;
    SpriteRenderer _renderer;
    BoxCollider2D _hitbox;
    readonly HashSet<int> _hitIds = new HashSet<int>();

    public void Swing(Transform owner, AbilityData ability, float damage)
    {
        _damage = damage;
        _life = ability.Lifetime;
        _fxColor = ability.Color;
        transform.SetParent(owner, false);
        // Offset alto o bastante para o arco da espada no sprite 64×64 (pivot nos pés).
        transform.localPosition = new Vector3(1.08f, 0.38f, 0f);
        transform.localRotation = Quaternion.Euler(0f, 0f, -18f);
        transform.localScale = new Vector3(ability.ProjectileSize.x, ability.ProjectileSize.y, 1f);

        _renderer = gameObject.AddComponent<SpriteRenderer>();
        _renderer.sprite = SlashSprite();
        _renderer.sortingOrder = 21;

        var body = gameObject.AddComponent<Rigidbody2D>();
        body.bodyType = RigidbodyType2D.Kinematic;
        body.gravityScale = 0f;
        body.freezeRotation = true;

        _hitbox = gameObject.AddComponent<BoxCollider2D>();
        _hitbox.isTrigger = true;
        _hitbox.size = Vector2.one;
        _hitbox.enabled = false; // wind-up do frame 13

        var ownerCollider = owner.GetComponent<Collider2D>();
        if (ownerCollider != null)
            Physics2D.IgnoreCollision(_hitbox, ownerCollider, true);
    }

    void Update()
    {
        _age += Time.deltaTime;
        if (_hitbox != null && !_hitbox.enabled && _age >= ActiveDelay)
            _hitbox.enabled = true;

        if (_renderer != null)
        {
            var color = _renderer.color;
            color.a = Mathf.Lerp(1f, 0.15f, _age / Mathf.Max(0.05f, _life));
            _renderer.color = color;
        }

        if (_age >= _life)
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

    static Sprite SlashSprite()
    {
        if (_slashSprite != null)
            return _slashSprite;

        int height = SlashArt.Length;
        int width = SlashArt[0].Length;
        var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
        var pixels = new Color32[width * height];
        for (int y = 0; y < height; y++)
        {
            string row = SlashArt[height - 1 - y];
            for (int x = 0; x < width; x++)
            {
                char ch = x < row.Length ? row[x] : '.';
                pixels[y * width + x] = ch switch
                {
                    'w' => new Color32(255, 248, 210, 255),
                    'y' => new Color32(255, 220, 68, 255),
                    'o' => new Color32(255, 102, 16, 255),
                    _ => new Color32(0, 0, 0, 0),
                };
            }
        }

        texture.SetPixels32(pixels);
        texture.Apply();
        texture.filterMode = FilterMode.Point;
        texture.wrapMode = TextureWrapMode.Clamp;
        _slashSprite = Sprite.Create(
            texture,
            new Rect(0f, 0f, width, height),
            new Vector2(0.5f, 0.5f),
            15f,
            0,
            SpriteMeshType.FullRect);
        return _slashSprite;
    }

    void Hit(Collider2D other)
    {
        if (_hitbox == null || !_hitbox.enabled || _damage <= 0f)
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
    }
}
