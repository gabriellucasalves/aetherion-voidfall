using System;
using UnityEngine;

// The Mage's runic shield uses the shared damage-absorbing pool. Its existing
// ring/hex artwork stays intact; holding no longer drains charge or leaks 45% HP.
public class MagicWard : MonoBehaviour
{
    public const float MaxCharge = 100f;
    public const float DamageTakenScale = 0f;
    public const float AuraRadius = 1.35f;
    ShieldSystem _shield;
    bool _holding;
    public float Charge => _shield != null ? _shield.Current : 0f;
    public float Max => _shield != null ? _shield.Max : MaxCharge;
    public float Normalized => Max > 0f ? Charge / Max : 0f;
    public bool IsActive { get; private set; }
    public event Action<MagicWard> Changed;
    float _pulse, _sparkClock;
    SpriteRenderer _ring, _hex;
    static Sprite _ringSprite, _hexSprite;

    public void Configure()
    {
        if (_shield != null) _shield.Changed -= OnShieldChanged;
        _shield = GetComponent<ShieldSystem>();
        if (_shield == null) { _shield = gameObject.AddComponent<ShieldSystem>(); _shield.Configure(MaxCharge); }
        _shield.Changed += OnShieldChanged;
        _holding = false;
        IsActive = false;
        SetVisualActive(false);
        Changed?.Invoke(this);
    }

    void OnDestroy() { if (_shield != null) _shield.Changed -= OnShieldChanged; }
    void OnShieldChanged(ShieldSystem _) { SetHolding(_holding); Changed?.Invoke(this); }

    public void SetHolding(bool holding)
    {
        _holding = holding;
        bool active = holding && _shield != null && !_shield.IsBroken;
        if (active == IsActive) return;
        IsActive = active;
        SetVisualActive(active);
        Changed?.Invoke(this);
    }

    public float Mitigate(float damage) => IsActive && _shield != null ? _shield.Absorb(damage) : damage;

    void Update()
    {
        SetHolding(_holding);
        if (IsActive) AnimateAura();
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
