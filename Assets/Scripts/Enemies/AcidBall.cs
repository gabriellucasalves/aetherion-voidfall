using UnityEngine;

// Bola ácida do Zumbi Corrompido: sai em arco balístico até o jogador,
// gira no ar e estoura em respingos verdes ao acertar qualquer coisa.
public class AcidBall : MonoBehaviour
{
    static readonly Color AcidBright = new Color(0.62f, 0.9f, 0.35f);
    static Sprite _sprite;

    float _damage;
    Transform _player;
    Rigidbody2D _body;
    float _age;

    public static void Lob(Vector3 from, Transform player, float damage)
    {
        if (player == null)
            return;

        var go = new GameObject("BolaAcida");
        go.transform.position = from;
        var ball = go.AddComponent<AcidBall>();
        ball._damage = damage;
        ball._player = player;

        var renderer = go.AddComponent<SpriteRenderer>();
        renderer.sprite = BlobSprite();
        renderer.sortingOrder = 20;

        var body = go.AddComponent<Rigidbody2D>();
        body.bodyType = RigidbodyType2D.Dynamic;
        body.gravityScale = 1f;
        ball._body = body;

        var collider = go.AddComponent<CircleCollider2D>();
        collider.isTrigger = true;
        collider.radius = 0.22f;

        // arco balístico até a posição atual do jogador (gravidade da fase é -32)
        Vector2 target = player.position + Vector3.up * 0.6f;
        float g = Mathf.Abs(Physics2D.gravity.y);
        float vy = 12f; // apex ~2.2 un acima do zumbi — arco visível e desviável
        float dy = target.y - from.y;
        float root = Mathf.Max(0.1f, vy * vy - 2f * g * dy);
        float t = (vy + Mathf.Sqrt(root)) / g;
        float vx = Mathf.Clamp((target.x - from.x) / t, -10f, 10f);
        body.linearVelocity = new Vector2(vx, vy);
    }

    void Update()
    {
        _age += Time.deltaTime;
        transform.Rotate(0f, 0f, 540f * Time.deltaTime);
        if (_age > 5f)
            Splash();
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        // não estoura em outros inimigos nem em gatilhos (aura, corte etc.)
        if (other.GetComponentInParent<EnemyController>() != null)
            return;
        if (other.isTrigger)
            return;

        var player = other.GetComponent<PlayerController>();
        if (player == null)
            player = other.GetComponentInParent<PlayerController>();
        if (player != null)
            EnemyController.DamagePlayer(player.transform, transform.position, _damage);

        Splash();
    }

    void Splash()
    {
        PixelBurst.Spawn(transform.position, AcidBright, 8);
        Destroy(gameObject);
    }

    static Sprite BlobSprite()
    {
        if (_sprite != null)
            return _sprite;

        string[] art =
        {
            "..kkkk..",
            ".kggggk.",
            "kggllggk",
            "kgllllgk",
            "kglllggk",
            "kggggggk",
            ".kggggk.",
            "..kkkk..",
        };

        int size = 8;
        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        var pixels = new Color32[size * size];
        for (int y = 0; y < size; y++)
        {
            string row = art[size - 1 - y];
            for (int x = 0; x < size; x++)
            {
                pixels[y * size + x] = row[x] switch
                {
                    'l' => new Color32(178, 235, 96, 255),
                    'g' => new Color32(96, 168, 58, 255),
                    'k' => new Color32(28, 52, 20, 255),
                    _ => new Color32(0, 0, 0, 0),
                };
            }
        }

        texture.SetPixels32(pixels);
        texture.Apply();
        texture.filterMode = FilterMode.Point;
        texture.wrapMode = TextureWrapMode.Clamp;
        _sprite = Sprite.Create(
            texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 16f, 0, SpriteMeshType.FullRect);
        return _sprite;
    }
}
