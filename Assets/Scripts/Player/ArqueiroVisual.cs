using UnityEngine;

// Runtime visual do Arqueiro — mesma pipeline do GuerreiroVisual
// (Resources/<Hero>/sheet.png, célula 64, 7 colunas, pivot no pé, PPU 15).
//
// Mapa do sheet (índice = linha*7 + coluna, topo→baixo / esq→dir):
//
//   Idle      0  1  2  3
//   Walk      4  5  6  7  8  9
//   Jump     10 11 12          (subida / ápice / queda)
//   Shoot    13 14 15          (puxa → solta → recolhe)
//   Dash     16 17
//   Special  18                (soltura em leque; wind-up reusa 13–14)
//   Hurt     19 20
//
public class ArqueiroVisual : MonoBehaviour
{
    const int Cell = 64;
    const int Cols = 7;
    const int FrameCount = 21;
    const float Ppu = 15f;
    const float FootPivot = 3f / 64f;

    // Idle — respiração com arco em guarda
    static readonly int[] Idle = { 0, 1, 2, 3 };
    // Walk — passada curta
    static readonly int[] Walk = { 4, 5, 6, 7, 8, 9 };
    // Shoot — draw / release / recover
    static readonly int[] Shoot = { 13, 14, 15 };
    // Dash — impulso (no Guerreiro estes slots são Block)
    static readonly int[] Dash = { 16, 17 };
    // Special — puxa (13–14) + soltura em leque (18)
    static readonly int[] Special = { 13, 14, 18 };
    // Hurt — recuo
    static readonly int[] Hurt = { 19, 20 };

    Sprite[] _frames;
    SpriteRenderer _renderer;
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

        var go = new GameObject("ArqueiroPixel");
        go.transform.SetParent(parent, false);
        go.transform.localPosition = new Vector3(0f, -0.5f, 0f);
        go.SetActive(false);
        var visual = go.AddComponent<ArqueiroVisual>();
        visual._frames = frames;
        visual._renderer = go.AddComponent<SpriteRenderer>();
        visual._renderer.sortingOrder = 12;
        visual._renderer.sprite = frames[0];
        go.SetActive(true);
        return true;
    }

    void LateUpdate()
    {
        if (_frames == null || _frames.Length < FrameCount)
            return;

        Bind();

        if (_hurtLeft > 0f)
            _hurtLeft -= Time.deltaTime;

        if (_hurtLeft > 0f)
        {
            Play("hurt", Hurt, 10f, false);
            return;
        }

        if (_combat != null && _combat.IsSpecialAttacking)
        {
            Play("special", Special, 14f, false);
            return;
        }

        if (_combat != null && _combat.IsAttacking)
        {
            Play("shoot", Shoot, 12f, false);
            return;
        }

        if (_player != null && _player.IsDashing)
        {
            Play("dash", Dash, 14f, false);
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

    // Jump — mesmos slots do Guerreiro (10 subida / 11 ápice / 12 queda)
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
        var texture = Resources.Load<Texture2D>("Arqueiro/sheet");
        if (texture == null)
        {
            Debug.LogWarning("Resources/Arqueiro/sheet.png não encontrado.");
            return null;
        }

        texture.filterMode = FilterMode.Point;
        texture.wrapMode = TextureWrapMode.Clamp;
        return texture;
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
