using UnityEngine;

public class MeleeSlash : MonoBehaviour
{
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
    SpriteRenderer _renderer;

    public void Swing(Transform owner, AbilityData ability, float damage)
    {
        _damage = damage;
        _life = ability.Lifetime;
        transform.SetParent(owner, false);
        transform.localPosition = new Vector3(0.92f, 0.12f, 0f);
        transform.localRotation = Quaternion.Euler(0f, 0f, -18f);
        transform.localScale = new Vector3(ability.ProjectileSize.x, ability.ProjectileSize.y, 1f);

        _renderer = gameObject.AddComponent<SpriteRenderer>();
        _renderer.sprite = SlashSprite();
        _renderer.sortingOrder = 21;

        var body = gameObject.AddComponent<Rigidbody2D>();
        body.bodyType = RigidbodyType2D.Kinematic;
        body.gravityScale = 0f;
        body.freezeRotation = true;

        var collider = gameObject.AddComponent<BoxCollider2D>();
        collider.isTrigger = true;
        collider.size = Vector2.one;

        var ownerCollider = owner.GetComponent<Collider2D>();
        if (ownerCollider != null)
            Physics2D.IgnoreCollision(collider, ownerCollider, true);
    }

    void Update()
    {
        _age += Time.deltaTime;
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
        var enemy = other.GetComponent<EnemyController>();
        if (enemy == null)
            enemy = other.GetComponentInParent<EnemyController>();
        if (enemy == null || enemy.Health == null || enemy.Health.IsDead)
            return;

        enemy.ReceiveDamage(_damage);
        _damage = 0f;
    }
}
