using UnityEngine;

// Pixel art dos inimigos T1 (sheets geradas por Assets/Art/Inimigos/build_enemies.py).
// Cada sheet tem 4 células de 64px: idle A/B + passo A/B, com brilho dos olhos piscando.
public class InimigoVisual : MonoBehaviour
{
    const float Ppu = 15f;               // mesma escala do Guerreiro
    const float FootPivot = 7f / 64f;    // pés na linha 57 da célula
    static readonly int[] WalkCycle = { 2, 0, 3, 1 };

    static readonly System.Collections.Generic.Dictionary<string, Sprite[]> Cache =
        new System.Collections.Generic.Dictionary<string, Sprite[]>();

    SpriteRenderer _renderer;
    Rigidbody2D _body;
    EnemyController _controller;
    Sprite[] _frames;
    float _clock;
    float _hurtLeft;

    public static bool Attach(GameObject owner, string id)
    {
        var frames = Load(id);
        if (frames == null)
            return false;

        var go = new GameObject("Visual");
        go.transform.SetParent(owner.transform, false);
        var visual = go.AddComponent<InimigoVisual>();
        visual._frames = frames;
        visual._renderer = go.AddComponent<SpriteRenderer>();
        visual._renderer.sprite = frames[0];
        visual._renderer.sortingOrder = 8;
        visual._body = owner.GetComponent<Rigidbody2D>();
        visual._controller = owner.GetComponent<EnemyController>();
        visual._clock = Random.Range(0f, 10f); // dessincroniza a animação entre inimigos

        var health = owner.GetComponent<HealthSystem>();
        if (health != null)
            health.Damaged += _ => visual._hurtLeft = 0.14f;
        return true;
    }

    static Sprite[] Load(string id)
    {
        if (Cache.TryGetValue(id, out var cached) && cached != null)
            return cached;

        var texture = Resources.Load<Texture2D>("Inimigos/" + id);
        if (texture == null)
            return null;

        texture.filterMode = FilterMode.Point;
        texture.wrapMode = TextureWrapMode.Clamp;

        int count = texture.width / 64;
        var frames = new Sprite[count];
        for (int i = 0; i < count; i++)
        {
            frames[i] = Sprite.Create(
                texture,
                new Rect(i * 64, 0, 64, 64),
                new Vector2(0.5f, FootPivot),
                Ppu,
                0,
                SpriteMeshType.FullRect);
        }

        Cache[id] = frames;
        return frames;
    }

    void LateUpdate()
    {
        if (_renderer == null || _frames == null)
            return;

        _clock += Time.deltaTime;

        // flash de dano > telegraph de ataque > normal
        if (_hurtLeft > 0f)
        {
            _hurtLeft -= Time.deltaTime;
            _renderer.color = new Color(1f, 0.42f, 0.42f);
        }
        else if (_controller != null && _controller.IsWindingUp)
        {
            // telegraph: zumbi prestes a explodir pisca vermelho bem rápido;
            // os outros piscam laranja enquanto armam o golpe
            bool exploding = _controller.Data != null && _controller.Data.AttackKind == "explode";
            float speed = exploding ? 24f : 14f;
            var flash = exploding ? new Color(1f, 0.3f, 0.24f) : new Color(1f, 0.62f, 0.4f);
            bool on = (int)(Time.time * speed) % 2 == 0;
            _renderer.color = on ? flash : Color.white;
        }
        else
        {
            _renderer.color = Color.white;
        }

        bool moving = _body != null && Mathf.Abs(_body.linearVelocity.x) > 0.15f;
        if (moving)
            _renderer.sprite = _frames[WalkCycle[(int)(_clock * 7f) % WalkCycle.Length]];
        else
            _renderer.sprite = _frames[(int)(_clock * 2.4f) % 2];
    }
}
