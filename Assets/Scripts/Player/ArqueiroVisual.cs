using UnityEngine;

// Runtime visual do Arqueiro — mesma pipeline do GuerreiroVisual
// (Resources/<Hero>/sheet.png, célula 64, 7 colunas, pivot no pé, PixelArt.Ppu).
// Mapa completo: Assets/Art/Arqueiro/FRAME_MAP.md
//
//   Idle      0  1  2  3
//   Walk      4  5  6  7  8  9
//   Jump     10 11 12          (subida / ápice / queda)
//   Shoot    13 14 15          (puxa → solta → recolhe) — sync Arrow.MuzzleDelay no 14
//   Dash     16 17
//   Special  18                (soltura em leque; wind-up 13×2→14; sync SpecialMuzzleDelay)
//   Hurt     19 20
//
public class ArqueiroVisual : MonoBehaviour
{
    const int Cell = 64;
    const int Cols = 7;
    const int FrameCount = 21;
    const float Ppu = PixelArt.Ppu;
    const float FootPivot = 3f / 64f;

    // Idle — respiração com arco em guarda
    static readonly int[] Idle = { 0, 1, 2, 3 };
    // Walk — passada curta
    static readonly int[] Walk = { 4, 5, 6, 7, 8, 9 };
    // Shoot — draw hold → release → recover (legível; flecha spawna no release)
    static readonly int[] Shoot = { 13, 14, 15, 38 };
    // Dash — impulso (no Guerreiro estes slots são Block)
    static readonly int[] Dash = { 16, 17 };
    // Special — draw hold → release → leque (18); sync Arrow.SpecialMuzzleDelay
    static readonly int[] Special = { 13, 13, 14, 18 };
    // Hurt — recuo
    static readonly int[] Hurt = { 19, 20 };
    static readonly int[] Piercing = { 21, 22, 23, 24, 25, 26 };
    static readonly int[] Death = { 27, 28, 29, 30, 31 };
    static readonly int[] Run = { 32, 33, 34, 35, 36, 37 };

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
    float _afterimageClock;

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

        if (_health != null && _health.IsDead) { Play("death", Death, 7f, false); return; }

        if (_hurtLeft > 0f)
            _hurtLeft -= Time.deltaTime;

        if (_hurtLeft > 0f)
        {
            Play("hurt", Hurt, 10f, false);
            return;
        }

        if (_combat != null && _combat.IsSpecialAttacking)
        {
            // ~12 fps: draw (13×2) + release (14) → leque (18) ~0.25s (= Arrow.SpecialMuzzleDelay).
            Play("special", Piercing, 9f, false);
            return;
        }

        if (_combat != null && _combat.IsAttacking)
        {
            // ~12 fps: draw (13×2) cobre MuzzleDelay ~0.16s; release (14) bate com spawn da flecha.
            Play("shoot", Shoot, 7.5f, false);
            return;
        }

        if (_player != null && _player.IsDashing)
        {
            Play("dash", Dash, 14f, false);
            // Rastro leve (pós-imagem) — VFX trivial do dash Shift.
            _afterimageClock += Time.deltaTime;
            if (_afterimageClock >= 0.045f)
            {
                _afterimageClock = 0f;
                SpawnDashAfterimage();
            }
            return;
        }

        _afterimageClock = 0f;

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
            Play(Mathf.Abs(velocity.x) > 5.5f ? "run" : "walk", Mathf.Abs(velocity.x) > 5.5f ? Run : Walk, Mathf.Abs(velocity.x) > 5.5f ? 13f : 10f, true);
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

        _clock += (clip == "death" ? Time.unscaledDeltaTime : Time.deltaTime) * fps;
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

        var frames = new Sprite[39];
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

        TrioAnimationFrames.Copy(frames, 0, "arqueiro", "idle", 2);
        frames[2] = frames[0]; frames[3] = frames[1];
        TrioAnimationFrames.Copy(frames, 4, "arqueiro", "andar", 6);
        TrioAnimationFrames.Copy(frames, 10, "arqueiro", "pulo", 3);
        var attack = TrioAnimationFrames.Load("arqueiro", "ataque", 4);
        if (attack != null)
        {
            frames[13] = attack[0]; frames[14] = attack[1];
            frames[15] = attack[2]; frames[38] = attack[3];
        }
        TrioAnimationFrames.Copy(frames, 19, "arqueiro", "dano", 2);
        TrioAnimationFrames.Copy(frames, 21, "arqueiro", "especial_perfurante", 6);
        TrioAnimationFrames.Copy(frames, 27, "arqueiro", "morte", 5);
        TrioAnimationFrames.Copy(frames, 32, "arqueiro", "correr", 6);
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


    void SpawnDashAfterimage()
    {
        if (_renderer == null || _renderer.sprite == null)
            return;

        var go = new GameObject("DashGhost");
        go.transform.position = transform.position;
        go.transform.localScale = transform.lossyScale;
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = _renderer.sprite;
        sr.flipX = _renderer.flipX;
        sr.sortingOrder = _renderer.sortingOrder - 1;
        var c = _renderer.color;
        c.a = 0.4f;
        sr.color = c;
        go.AddComponent<DashGhost>().Begin(0.16f);
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
