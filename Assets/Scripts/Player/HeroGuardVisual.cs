using UnityEngine;

// A native, pixel-aligned guard rig: reuses the current character poses and palette.
// The warrior raises a solid red/gold shield; the archer projects a crystalline aegis.
// Mage keeps the existing authored runic ward. No idle/attack art is replaced.
[DefaultExecutionOrder(150)]
public sealed class HeroGuardVisual : MonoBehaviour
{
    PlayerCombat _combat;
    HealthSystem _health;
    ShieldSystem _pool;
    SpriteRenderer _body, _front;
    Vector3 _rest;
    float _raise, _hit, _lastShield;
    static Sprite _warriorShield, _archerAegis;
    public bool Raised => _combat != null && _combat.IsBlocking;
    public SpriteRenderer Front => _front;

    public void Setup(CharacterData hero)
    {
        _combat = GetComponent<PlayerCombat>();
        _health = GetComponent<HealthSystem>();
        _pool = GetComponent<ShieldSystem>();
        foreach (var renderer in GetComponentsInChildren<SpriteRenderer>())
            if (renderer.name == "GuerreiroPixel" || renderer.name == "MagoPixel" || renderer.name == "ArqueiroPixel")
            { _body = renderer; break; }
        if (_body == null) return;
        _rest = _body.transform.localPosition;
        if (_pool != null)
        {
            _lastShield = _pool.Current;
            _pool.Changed += OnShieldChanged;
        }
        if (hero.Id == "mago") return;
        var go = new GameObject(hero.Id == "guerreiro" ? "EscudoFisico" : "EgideCristal");
        go.transform.SetParent(_body.transform, false);
        _front = go.AddComponent<SpriteRenderer>();
        _front.sprite = hero.Id == "guerreiro" ? WarriorShield() : ArcherAegis();
        _front.sortingOrder = _body.sortingOrder + 2;
        _front.enabled = false;
    }

    void OnShieldChanged(ShieldSystem shield)
    {
        if (shield.Current < _lastShield) _hit = 1f;
        _lastShield = shield.Current;
    }
    void OnDestroy() { if (_pool != null) _pool.Changed -= OnShieldChanged; }

    void LateUpdate()
    {
        if (_body == null || _combat == null) return;
        bool raised = Raised && (_health == null || !_health.IsDead);
        _raise = Mathf.MoveTowards(_raise, raised ? 1f : 0f, Time.deltaTime * (raised ? 7f : 10f));
        _hit = Mathf.MoveTowards(_hit, 0f, Time.deltaTime * 8f);
        float step = Mathf.Round(_raise * 4f) / 4f;
        _body.transform.localPosition = Snap(_rest + new Vector3(step * (.0625f - _hit * .0625f), 0, 0));
        if (_front == null) return;
        _front.enabled = _raise > 0f;
        _front.sortingOrder = _body.sortingOrder + 2;
        // Four discrete lift poses, then a planted hold and impact recoil.
        _front.transform.localPosition = Snap(new Vector3(Mathf.Lerp(.18f, _combat.IsWarrior ? .82f : .70f, step) - _hit * .125f,
            Mathf.Lerp(.28f, .78f, step), 0f));
        _front.color = Color.Lerp(Color.white, new Color(1f, .84f, .45f), _hit);
        if (_combat.IsArcher)
        {
            float pulse = 1f + (Mathf.FloorToInt(Time.time * 8f) % 2) * .045f;
            _front.transform.localScale = new Vector3(pulse, pulse, 1f);
            var color = _front.color; color.a = .86f + _hit * .14f; _front.color = color;
        }
    }

    static Vector3 Snap(Vector3 p) => new Vector3(Mathf.Round(p.x * 16f) / 16f, Mathf.Round(p.y * 16f) / 16f, p.z);

    static Sprite WarriorShield()
    {
        if (_warriorShield != null) return _warriorShield;
        return _warriorShield = PixelSprite("EscudoGuerreiroAtual", new[] {
            "....0000....", "..00233200..", ".0233333320.", "023211112320",
            "032144441230", "032144441230", "032144441230", "032133331230",
            "032133331230", "032143341230", "032144441230", ".0321441230.",
            ".0232112320.", "..02333230..", "...023320...", "....0220...."
        }, false);
    }

    static Sprite ArcherAegis()
    {
        if (_archerAegis != null) return _archerAegis;
        return _archerAegis = PixelSprite("EgideCristalArqueiro", new[] {
            "......cc......", ".....cwwc.....", "....cw..wc....", "...cw....wc...",
            "..cw......wc..", ".cw...yy...wc.", "cw....yy....wc", "cw....yy....wc",
            "cw..yywwyy..wc", "cw...ywwy...wc", "cw...ywwy...wc", "cw..yywwyy..wc",
            "cw....yy....wc", "cw....yy....wc", ".cw...yy...wc.", "..cw......wc..",
            "...cw....wc...", "....cw..wc....", ".....cwwc.....", "......cc......"
        }, true);
    }

    static Sprite PixelSprite(string name, string[] rows, bool crystal)
    {
        int w = rows[0].Length, h = rows.Length;
        var pixels = new Color32[w * h];
        for (int y = 0; y < h; y++) for (int x = 0; x < w; x++)
        {
            char c = rows[h - 1 - y][x];
            pixels[y * w + x] = crystal ? c switch {
                'c' => new Color32(42, 154, 194, 235), 'w' => new Color32(180, 242, 255, 255),
                'y' => new Color32(255, 220, 68, 245), _ => new Color32(0, 0, 0, 0)
            } : c switch {
                '0' => new Color32(14, 6, 8, 255), '1' => new Color32(128, 16, 18, 255),
                '2' => new Color32(176, 116, 40, 255), '3' => new Color32(255, 220, 68, 255),
                '4' => new Color32(188, 20, 20, 255), _ => new Color32(0, 0, 0, 0)
            };
        }
        var texture = new Texture2D(w, h, TextureFormat.RGBA32, false) { name = name, filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
        texture.SetPixels32(pixels); texture.Apply();
        var sprite = Sprite.Create(texture, new Rect(0, 0, w, h), new Vector2(.5f, .5f), PixelArt.Ppu, 0, SpriteMeshType.FullRect);
        sprite.name = name;
        return sprite;
    }
}
