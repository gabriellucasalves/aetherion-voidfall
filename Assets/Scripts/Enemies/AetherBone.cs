using UnityEngine;

// Osso Aether do Esqueleto Alien: disparo direcionado, destrói ao atingir terreno ou jogador.
public class AetherBone : MonoBehaviour
{
    static readonly Color AetherSpark = new Color(0.3f, 0.9f, 0.95f);
    static Sprite _sprite;

    float _damage;
    float _age;
    Rigidbody2D _body;
    bool _held;
    Vector2 _heldVelocity;
    float _heldGravity;

    public static void Launch(Vector3 from, Transform player, float damage)
    {
        if (player == null)
            return;

        var go = new GameObject("OssoAether");
        go.transform.position = from;
        var axe = go.AddComponent<AetherBone>();
        axe._damage = damage;

        var renderer = go.AddComponent<SpriteRenderer>();
        renderer.sprite = BoneSprite();
        renderer.sortingOrder = 20;

        var body = go.AddComponent<Rigidbody2D>();
        body.bodyType = RigidbodyType2D.Dynamic;
        body.gravityScale = 0f; // voo reto — contrasta com o arco da bola ácida
        axe._body = body;

        body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        var collider = go.AddComponent<CircleCollider2D>();
        collider.isTrigger = true;
        collider.radius = 0.24f;

        Vector2 target = player.position + Vector3.up * 0.7f;
        body.linearVelocity = (target - (Vector2)from).normalized * 9f;
    }

    void Update()
    {
        if (VoidTimeStop.TickFreeze(_body, ref _held, ref _heldVelocity, ref _heldGravity))
            return;

        _age += Time.deltaTime;
        transform.Rotate(0f, 0f, -260f * Time.deltaTime); // gira como machado arremessado
        if (_age > 3.5f)
            Shatter();
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (VoidTimeStop.ProjectilesHeld || _held)
            return;

        // atravessa outros inimigos e gatilhos (aura, cortes, chuva)
        if (other.GetComponentInParent<EnemyController>() != null)
            return;
        if (other.isTrigger)
            return;

        var player = other.GetComponent<PlayerController>();
        if (player == null)
            player = other.GetComponentInParent<PlayerController>();
        if (player != null)
            EnemyController.DamagePlayer(player.transform, transform.position, _damage);

        Shatter();
    }

    void Shatter()
    {
        PixelBurst.Spawn(transform.position, AetherSpark, 6);
        Destroy(gameObject);
    }

    static Sprite BoneSprite()
    {
        if (_sprite != null)
            return _sprite;

        // machado pequeno: cabo de osso na diagonal + lâmina enferrujada
        string[] art =
        {
            "..ww....ww..",
            ".wwww..wwww.",
            "..wwwwwwww..",
            "...wmmmmw...",
            "...wmmmmw...",
            "..wwwwwwww..",
            ".wwww..wwww.",
            "..ww....ww..",
        };

        int height = art.Length;
        int width = art[0].Length;
        var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
        var pixels = new Color32[width * height];
        for (int y = 0; y < height; y++)
        {
            string row = art[height - 1 - y];
            for (int x = 0; x < width; x++)
            {
                pixels[y * width + x] = row[x] switch
                {
                    'm' => new Color32(68, 206, 218, 255), // lâmina de metal sujo
                    'k' => new Color32(52, 46, 38, 255),    // contorno escuro
                    'w' => new Color32(202, 226, 202, 255), // cabo de osso
                    _ => new Color32(0, 0, 0, 0),
                };
            }
        }

        texture.SetPixels32(pixels);
        texture.Apply();
        texture.filterMode = FilterMode.Point;
        texture.wrapMode = TextureWrapMode.Clamp;
        _sprite = Sprite.Create(
            texture, new Rect(0f, 0f, width, height), new Vector2(0.5f, 0.5f), 16f, 0, SpriteMeshType.FullRect);
        return _sprite;
    }
}
