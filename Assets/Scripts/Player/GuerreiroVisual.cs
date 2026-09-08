using UnityEngine;

public class GuerreiroVisual : MonoBehaviour
{
    const int Cell = 64;
    const int Cols = 7;
    const int FrameCount = 21;
    const float Ppu = 15f;
    const float FootPivot = 3f / 64f;

    static readonly int[] Idle = { 0, 1, 2, 3 };
    static readonly int[] Walk = { 4, 5, 6, 7, 8, 9 };
    static readonly int[] Attack = { 13, 14, 15, 16 };
    static readonly int[] Block = { 17, 18 };
    static readonly int[] Hurt = { 19, 20 };

    static Sprite _auraSprite;

    Sprite[] _frames;
    SpriteRenderer _renderer;
    SpriteRenderer _aura;
    PlayerController _player;
    PlayerCombat _combat;
    HealthSystem _health;
    bool _bound;
    float _clock;
    int _index;
    string _clip = "";
    float _hurtLeft;

    public static bool Attach(Transform parent)
    {
        var frames = LoadFrames();
        if (frames == null || frames.Length < FrameCount)
            return false;

        var go = new GameObject("GuerreiroPixel");
        go.transform.SetParent(parent, false);
        go.transform.localPosition = new Vector3(0f, -0.5f, 0f);
        go.SetActive(false);
        var visual = go.AddComponent<GuerreiroVisual>();
        visual._frames = frames;
        visual._renderer = go.AddComponent<SpriteRenderer>();
        visual._renderer.sortingOrder = 12;
        visual._renderer.sprite = frames[0];

        var aura = new GameObject("AuraEscudo");
        aura.transform.SetParent(go.transform, false);
        aura.transform.localPosition = new Vector3(0f, 0.95f, 0f);
        visual._aura = aura.AddComponent<SpriteRenderer>();
        visual._aura.sprite = AuraSprite();
        visual._aura.sortingOrder = 11;
        visual._aura.enabled = false;

        go.SetActive(true);
        return true;
    }

    void LateUpdate()
    {
        if (_frames == null || _frames.Length < FrameCount)
            return;

        Bind();

        bool blocking = _combat != null && _combat.IsBlocking;
        UpdateAura(blocking);

        if (_hurtLeft > 0f)
            _hurtLeft -= Time.deltaTime;

        if (_hurtLeft > 0f)
        {
            Play("hurt", Hurt, 10f, false);
            return;
        }

        // bloqueio tem prioridade sobre o ataque
        if (blocking)
        {
            Play("block", Block, 5f, true);
            return;
        }

        if (_combat != null && _combat.IsAttacking)
        {
            Play("attack", Attack, 12f, false);
            return;
        }

        bool grounded = _player != null && _player.Grounded;
        Vector2 velocity = _player != null ? _player.Velocity : Vector2.zero;
        bool jumping = !grounded || velocity.y > 1.6f;

        if (jumping)
        {
            Show(JumpFrame(velocity.y));
            _clip = "jump";
            return;
        }

        bool walking = Mathf.Abs(velocity.x) > 0.12f || (_player != null && _player.WantsMove);
        if (walking)
        {
            Play("walk", Walk, 12f, true);
            return;
        }

        Play("idle", Idle, 5f, true);
    }

    void Play(string clip, int[] frames, float fps, bool loop)
    {
        if (_clip != clip)
        {
            _clip = clip;
            _index = 0;
            _clock = 0f;
            Show(frames[0]);
            return;
        }

        _clock += Time.deltaTime * fps;
        if (_clock < 1f)
            return;

        _clock -= 1f;
        if (loop)
            _index = (_index + 1) % frames.Length;
        else
            _index = Mathf.Min(_index + 1, frames.Length - 1);
        Show(frames[_index]);
    }

    static int JumpFrame(float vy)
    {
        if (vy > 2.4f)
            return 10;
        if (vy < -1.4f)
            return 12;
        return 11;
    }

    void Show(int frame)
    {
        if (_renderer == null || _frames == null)
            return;
        if (frame < 0 || frame >= _frames.Length)
            return;
        _renderer.sprite = _frames[frame];
    }

    static Sprite[] LoadFrames()
    {
        var texture = LoadSheet();
        if (texture == null)
            return null;

        var frames = new Sprite[FrameCount];
        int height = texture.height;
        for (int i = 0; i < FrameCount; i++)
        {
            int col = i % Cols;
            int rowFromTop = i / Cols;
            int x = col * Cell;
            int y = height - (rowFromTop + 1) * Cell;
            if (x + Cell > texture.width || y < 0)
                return null;

            frames[i] = Sprite.Create(
                texture,
                new Rect(x, y, Cell, Cell),
                new Vector2(0.5f, FootPivot),
                Ppu,
                0,
                SpriteMeshType.FullRect);
        }

        return frames;
    }

    static Texture2D LoadSheet()
    {
        var texture = Resources.Load<Texture2D>("Guerreiro/sheet");
        if (texture == null)
        {
            Debug.LogWarning("Resources/Guerreiro/sheet.png não encontrado.");
            return null;
        }

        texture.filterMode = FilterMode.Point;
        texture.wrapMode = TextureWrapMode.Clamp;
        return texture;
    }

    void UpdateAura(bool blocking)
    {
        if (_aura == null)
            return;

        _aura.enabled = blocking;
        if (!blocking)
            return;

        float pulse = 0.5f + 0.5f * Mathf.Sin(Time.time * 7f);
        _aura.color = new Color(1f, 1f, 1f, 0.6f + 0.35f * pulse);
        _aura.transform.localScale = Vector3.one * (1f + 0.06f * pulse);
    }

    static Sprite AuraSprite()
    {
        if (_auraSprite != null)
            return _auraSprite;

        const int W = 48;
        const int H = 52;
        var texture = new Texture2D(W, H, TextureFormat.RGBA32, false);
        var pixels = new Color32[W * H];
        float cx = (W - 1) / 2f;
        float cy = (H - 1) / 2f;
        for (int y = 0; y < H; y++)
        {
            for (int x = 0; x < W; x++)
            {
                float dx = (x - cx) / 21f;
                float dy = (y - cy) / 24f;
                float d = Mathf.Sqrt(dx * dx + dy * dy);
                Color32 color = default;
                if (d > 0.86f && d <= 1f)
                    color = new Color32(80, 160, 255, 210);
                else if (d > 0.72f && d <= 0.86f)
                    color = new Color32(150, 215, 255, 100);
                else if (d <= 0.72f)
                    color = new Color32(120, 190, 255, 26);
                pixels[y * W + x] = color;
            }
        }

        texture.SetPixels32(pixels);
        texture.Apply();
        texture.filterMode = FilterMode.Point;
        texture.wrapMode = TextureWrapMode.Clamp;
        _auraSprite = Sprite.Create(
            texture,
            new Rect(0f, 0f, W, H),
            new Vector2(0.5f, 0.5f),
            24f,
            0,
            SpriteMeshType.FullRect);
        return _auraSprite;
    }

    void Bind()
    {
        if (_bound)
            return;

        _player = GetComponentInParent<PlayerController>();
        _combat = GetComponentInParent<PlayerCombat>();
        _health = GetComponentInParent<HealthSystem>();
        if (_player == null || _combat == null)
            return;

        if (_health != null)
            _health.Damaged += _ => _hurtLeft = 0.22f;
        _bound = true;
    }
}
