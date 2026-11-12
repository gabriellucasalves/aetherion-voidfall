using UnityEngine;

// Runtime visual do Arqueiro Arcano — mesma pipeline do GuerreiroVisual e MagoVisual
// (Resources/Arqueiro/sheet.png + overrides em Resources/TrioAnimations/arqueiro/<acao>.png,
// célula 64x64, FootPivot = 3/64, PPU = 16).
// Suporta o Tiro Carregado em 5 frames (ataque.png: 0=Puxar, 1=Carga média, 2=Carga máxima, 3=Soltar, 4=Recuo)
// e o indicador discreto de 3 segmentos acima do capuz azul-noturno.
public class ArqueiroVisual : MonoBehaviour
{
    const int Cell = 64;
    const int Cols = 7;
    const int FrameCount = 21;
    const float Ppu = PixelArt.Ppu;
    const float FootPivot = 3f / 64f;

    static readonly int[] Idle = { 0, 1, 2, 3 };
    static readonly int[] Walk = { 4, 5, 6, 7, 8, 9 };
    static readonly int[] Guard = { 13, 14 };
    static readonly int[] ReleaseRecoil = { 38, 39 };
    static readonly int[] Dash = { 16, 17 };
    static readonly int[] Hurt = { 19, 20 };
    static readonly int[] Piercing = { 21, 22, 23, 24, 25, 26 };
    static readonly int[] Death = { 27, 28, 29, 30, 31 };
    static readonly int[] Run = { 32, 33, 34, 35, 36, 37 };

    static Sprite[] _chargeIndicatorSprites;

    Sprite[] _frames;
    SpriteRenderer _renderer;
    SpriteRenderer _chargeIndicatorRenderer;
    PlayerController _player;
    PlayerCombat _combat;
    HealthSystem _health;
    bool _bound;
    float _clock;
    int _index;
    string _clip = "";
    float _hurtLeft;
    float _afterimageClock;
    int _prevChargeSegment;

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

        // Indicador discreto de 3 segmentos acima da cabeça durante a carga
        var indGo = new GameObject("ChargeIndicator");
        indGo.transform.SetParent(go.transform, false);
        indGo.transform.localPosition = new Vector3(0f, 1.92f, 0f);
        visual._chargeIndicatorRenderer = indGo.AddComponent<SpriteRenderer>();
        visual._chargeIndicatorRenderer.sortingOrder = 25;
        visual._chargeIndicatorRenderer.enabled = false;

        go.SetActive(true);
        return true;
    }

    void LateUpdate()
    {
        if (_frames == null || _frames.Length < FrameCount)
            return;

        Bind();

        if (_health != null && _health.IsDead)
        {
            UpdateChargeIndicator(0);
            Play("death", Death, 7f, false);
            return;
        }

        if (_combat != null && _combat.IsBlocking)
        {
            UpdateChargeIndicator(0);
            Play("crystal-guard", Guard, 7f, false);
            return;
        }

        if (_hurtLeft > 0f)
            _hurtLeft -= Time.deltaTime;

        if (_hurtLeft > 0f)
        {
            UpdateChargeIndicator(0);
            Play("hurt", Hurt, 10f, false);
            return;
        }

        if (_combat != null && _combat.IsSpecialAttacking)
        {
            UpdateChargeIndicator(0);
            Play("special", Piercing, 9f, false);
            return;
        }

        // Estado de carga (segurando o botão): trava a pose de mira conforme o nível (Puxar / Média / Máxima)
        if (_combat != null && _combat.IsCharging)
        {
            int seg = _combat.ChargeSegmentCount;
            UpdateChargeIndicator(seg);
            int chargeFrame = seg >= 3 ? 15 : (seg == 2 ? 14 : 13);
            Show(chargeFrame);
            _clip = "charge_" + seg;
            return;
        }

        UpdateChargeIndicator(0);

        // Soltar + Recuo logo após o disparo
        if (_combat != null && _combat.IsAttacking)
        {
            Play("release_recoil", ReleaseRecoil, 10f, false);
            return;
        }

        if (_player != null && _player.IsDashing)
        {
            Play("dash", Dash, 14f, false);
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

    void UpdateChargeIndicator(int segmentCount)
    {
        if (_chargeIndicatorRenderer == null)
            return;

        if (segmentCount <= 0)
        {
            _chargeIndicatorRenderer.enabled = false;
            _prevChargeSegment = 0;
            return;
        }

        // Flash ciano/dourado quando atinge a Carga Máxima (transição para o 3º segmento)
        if (segmentCount == 3 && _prevChargeSegment < 3)
        {
            Vector3 flashPos = transform.position + Vector3.up * 0.9f;
            PixelBurst.Spawn(flashPos, new Color(0.42f, 0.97f, 1f), 5);
            PixelBurst.Spawn(flashPos, new Color(1f, 0.95f, 0.62f), 4);
        }
        _prevChargeSegment = segmentCount;

        var sprites = GetChargeIndicatorSprites();
        int idx = Mathf.Clamp(segmentCount - 1, 0, sprites.Length - 1);
        _chargeIndicatorRenderer.sprite = sprites[idx];
        _chargeIndicatorRenderer.enabled = true;

        // Mantém a barra sempre orientada da esquerda para a direita mesmo quando o herói olha para a esquerda
        float parentSign = transform.lossyScale.x < 0f ? -1f : 1f;
        _chargeIndicatorRenderer.transform.localScale = new Vector3(parentSign, 1f, 1f);
    }

    static Sprite[] GetChargeIndicatorSprites()
    {
        if (_chargeIndicatorSprites != null && _chargeIndicatorSprites.Length == 3 && _chargeIndicatorSprites[0] != null)
            return _chargeIndicatorSprites;

        _chargeIndicatorSprites = new Sprite[3];
        const int w = 13;
        const int h = 4;
        var border = new Color32(18, 16, 32, 255);
        var empty = new Color32(28, 36, 56, 255);
        var seg1 = new Color32(46, 194, 230, 255);
        var seg2 = new Color32(107, 247, 255, 255);
        var seg3 = new Color32(255, 244, 156, 255);

        for (int level = 1; level <= 3; level++)
        {
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            var px = new Color32[w * h];
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    px[y * w + x] = border;
                }
            }
            for (int s = 0; s < 3; s++)
            {
                Color32 c = (s + 1 <= level)
                    ? (s == 0 ? seg1 : (s == 1 ? seg2 : seg3))
                    : empty;
                int sx = 1 + s * 4;
                for (int dy = 1; dy <= 2; dy++)
                {
                    for (int dx = 0; dx < 3; dx++)
                    {
                        px[dy * w + (sx + dx)] = c;
                    }
                }
            }
            tex.SetPixels32(px);
            tex.Apply();
            tex.filterMode = FilterMode.Point;
            tex.wrapMode = TextureWrapMode.Clamp;
            _chargeIndicatorSprites[level - 1] = Sprite.Create(
                tex,
                new Rect(0, 0, w, h),
                new Vector2(0.5f, 0f),
                Ppu,
                0,
                SpriteMeshType.FullRect);
        }

        return _chargeIndicatorSprites;
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
        if (frame < 0 || frame >= _frames.Length || _frames[frame] == null)
            return;
        _renderer.sprite = _frames[frame];
    }

    static Sprite[] LoadFrames()
    {
        var texture = LoadSheet();
        if (texture == null)
            return null;

        var frames = new Sprite[40];
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

        // Suporta ataque.png expandido com 5 frames (0=Puxar, 1=Carga média, 2=Carga máxima, 3=Soltar, 4=Recuo)
        var attack5 = TrioAnimationFrames.Load("arqueiro", "ataque", 5);
        if (attack5 != null)
        {
            frames[13] = attack5[0];
            frames[14] = attack5[1];
            frames[15] = attack5[2];
            frames[38] = attack5[3];
            frames[39] = attack5[4];
        }
        else
        {
            var attack4 = TrioAnimationFrames.Load("arqueiro", "ataque", 4);
            if (attack4 != null)
            {
                frames[13] = attack4[0];
                frames[14] = attack4[1];
                frames[15] = attack4[1];
                frames[38] = attack4[2];
                frames[39] = attack4[3];
            }
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
