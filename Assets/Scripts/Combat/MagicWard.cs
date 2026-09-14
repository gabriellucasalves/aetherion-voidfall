using System;
using UnityEngine;

/// <summary>
/// Campo de Magia (FASE 2) — defesa do Mago, distinta do escudo do Guerreiro.
///
/// Regra: hold (S / K / botão direito / Block mobile) mantém o campo enquanto houver carga.
/// Efeito: reduz 55% do dano recebido (omnidirecional).
/// Custo: drena ~28%/s (~3.5s de carga cheia). Sem carga o campo cai;
/// regenera após breve delay fora do hold. Após zerar, precisa soltar o hold.
/// </summary>
public class MagicWard : MonoBehaviour
{
    public const float MaxCharge = 100f;
    public const float DrainPerSecond = 28f;
    public const float RegenPerSecond = 22f;
    public const float RegenDelay = 0.55f;
    public const float DamageTakenScale = 0.45f; // 55% redução
    public const float AuraRadius = 1.35f;

    public float Charge { get; private set; } = MaxCharge;
    public float Max => MaxCharge;
    public bool IsActive { get; private set; }
    public float Normalized => Charge / MaxCharge;

    public event Action<MagicWard> Changed;

    bool _holding;
    bool _exhausted; // precisa soltar o hold após zerar a carga
    float _regenDelay;
    float _pulse;
    float _sparkClock;
    SpriteRenderer _ring;
    SpriteRenderer _hex;
    static Sprite _ringSprite;
    static Sprite _hexSprite;

    public void Configure()
    {
        Charge = MaxCharge;
        _holding = false;
        _exhausted = false;
        IsActive = false;
        _regenDelay = 0f;
        EnsureVisuals();
        SetVisualActive(false);
        Changed?.Invoke(this);
    }

    public void SetHolding(bool holding)
    {
        if (!holding)
            _exhausted = false;
        _holding = holding && !_exhausted;
    }

    /// <summary>Aplica mitigação se o campo estiver ativo. Retorna dano residual.</summary>
    public float Mitigate(float rawDamage)
    {
        if (!IsActive || rawDamage <= 0f)
            return rawDamage;

        float leftover = rawDamage * DamageTakenScale;
        PixelBurst.Spawn(transform.position + Vector3.up * 0.55f,
            new Color(0.55f, 0.85f, 1f), 4);
        PixelBurst.Spawn(transform.position + Vector3.up * 0.4f,
            new Color(0.72f, 0.45f, 1f), 2);
        // impacto no campo consome um pouco de carga extra
        Charge = Mathf.Max(0f, Charge - rawDamage * 0.35f);
        Changed?.Invoke(this);
        if (Charge <= 0.01f)
            DropField();
        return leftover;
    }

    void Update()
    {
        bool wantActive = _holding && Charge > 0.5f;
        if (wantActive != IsActive)
        {
            IsActive = wantActive;
            SetVisualActive(IsActive);
            if (IsActive)
            {
                _regenDelay = RegenDelay;
                PixelBurst.Spawn(transform.position + Vector3.up * 0.2f,
                    new Color(0.5f, 0.8f, 1f), 5);
            }
            Changed?.Invoke(this);
        }

        if (IsActive)
        {
            Charge = Mathf.Max(0f, Charge - DrainPerSecond * Time.deltaTime);
            Changed?.Invoke(this);
            if (Charge <= 0.01f)
                DropField();
            else
                AnimateAura();
        }
        else
        {
            if (_regenDelay > 0f)
                _regenDelay -= Time.deltaTime;
            else if (Charge < MaxCharge)
            {
                Charge = Mathf.Min(MaxCharge, Charge + RegenPerSecond * Time.deltaTime);
                Changed?.Invoke(this);
            }
        }
    }

    void DropField()
    {
        Charge = 0f;
        IsActive = false;
        _exhausted = true;
        _holding = false;
        _regenDelay = RegenDelay + 0.35f;
        SetVisualActive(false);
        PixelBurst.Spawn(transform.position + Vector3.up * 0.5f,
            new Color(0.7f, 0.5f, 1f), 6);
        Changed?.Invoke(this);
    }

    void EnsureVisuals()
    {
        if (_ring != null)
            return;

        _ring = CreateChild("CampoAnel", RingSprite(), 8);
        _hex = CreateChild("CampoHex", HexSprite(), 9);
        _ring.transform.localPosition = new Vector3(0f, 0.15f, 0f);
        _hex.transform.localPosition = new Vector3(0f, 0.15f, 0f);
        _ring.transform.localScale = Vector3.one * (AuraRadius * 2.05f);
        _hex.transform.localScale = Vector3.one * (AuraRadius * 1.55f);
    }

    SpriteRenderer CreateChild(string name, Sprite sprite, int order)
    {
        var go = new GameObject(name);
        go.transform.SetParent(transform, false);
        var renderer = go.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.sortingOrder = order;
        return renderer;
    }

    void SetVisualActive(bool on)
    {
        EnsureVisuals();
        if (_ring != null)
            _ring.enabled = on;
        if (_hex != null)
            _hex.enabled = on;
    }

    void AnimateAura()
    {
        _pulse += Time.deltaTime * 4.2f;
        float wave = 0.85f + Mathf.Sin(_pulse) * 0.12f;
        float spin = _pulse * 28f;

        if (_ring != null)
        {
            _ring.transform.localRotation = Quaternion.Euler(0f, 0f, -spin * 0.35f);
            _ring.transform.localScale = Vector3.one * (AuraRadius * 2.05f * wave);
            _ring.color = new Color(0.35f, 0.85f, 1f, 0.35f + Mathf.Sin(_pulse * 1.3f) * 0.12f);
        }

        if (_hex != null)
        {
            _hex.transform.localRotation = Quaternion.Euler(0f, 0f, spin * 0.55f);
            _hex.transform.localScale = Vector3.one * (AuraRadius * 1.55f * (1.05f - Mathf.Sin(_pulse) * 0.08f));
            _hex.color = new Color(0.72f, 0.4f, 1f, 0.55f + Mathf.Sin(_pulse * 1.7f) * 0.15f);
        }

        _sparkClock += Time.deltaTime;
        if (_sparkClock >= 0.22f)
        {
            _sparkClock = 0f;
            float ang = UnityEngine.Random.Range(0f, Mathf.PI * 2f);
            Vector3 rim = transform.position
                + new Vector3(Mathf.Cos(ang), Mathf.Sin(ang), 0f) * AuraRadius * 0.55f
                + Vector3.up * 0.2f;
            PixelBurst.Spawn(rim, new Color(0.55f, 0.8f, 1f), 1);
        }
    }

    static Sprite RingSprite()
    {
        if (_ringSprite != null)
            return _ringSprite;

        const int size = 64;
        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        var pixels = new Color32[size * size];
        float cx = (size - 1) * 0.5f;
        float outer = size * 0.48f;
        float inner = size * 0.38f;
        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            float d = Vector2.Distance(new Vector2(x, y), new Vector2(cx, cx));
            if (d <= outer && d >= inner)
            {
                byte a = (byte)(d > outer - 1.2f || d < inner + 1.2f ? 255 : 160);
                pixels[y * size + x] = new Color32(120, 220, 255, a);
            }
            else
                pixels[y * size + x] = new Color32(0, 0, 0, 0);
        }

        texture.SetPixels32(pixels);
        texture.Apply();
        texture.filterMode = FilterMode.Point;
        _ringSprite = Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), size);
        return _ringSprite;
    }

    static Sprite HexSprite()
    {
        if (_hexSprite != null)
            return _hexSprite;

        string[] art =
        {
            ".......cc.......",
            "......cwwc......",
            ".....cwppwc.....",
            "....cwppppwc....",
            "...cwp....pwc...",
            "..cwp..yy..pwc..",
            ".cwp...yy...pwc.",
            "cwp....yy....pwc",
            "cwp....yy....pwc",
            ".cwp...yy...pwc.",
            "..cwp..yy..pwc..",
            "...cwp....pwc...",
            "....cwppppwc....",
            ".....cwppwc.....",
            "......cwwc......",
            ".......cc.......",
        };

        int h = art.Length;
        int w = art[0].Length;
        var texture = new Texture2D(w, h, TextureFormat.RGBA32, false);
        var pixels = new Color32[w * h];
        for (int y = 0; y < h; y++)
        {
            string row = art[h - 1 - y];
            for (int x = 0; x < w; x++)
            {
                char ch = x < row.Length ? row[x] : '.';
                pixels[y * w + x] = ch switch
                {
                    'w' => new Color32(210, 245, 255, 255),
                    'c' => new Color32(60, 190, 255, 230),
                    'p' => new Color32(160, 90, 255, 200),
                    'y' => new Color32(200, 160, 255, 255),
                    _ => new Color32(0, 0, 0, 0),
                };
            }
        }

        texture.SetPixels32(pixels);
        texture.Apply();
        texture.filterMode = FilterMode.Point;
        _hexSprite = Sprite.Create(texture, new Rect(0f, 0f, w, h), new Vector2(0.5f, 0.5f), 14f);
        return _hexSprite;
    }
}
