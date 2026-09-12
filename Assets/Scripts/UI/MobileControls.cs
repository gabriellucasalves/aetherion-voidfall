using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Pad mobile:
//  - metade esquerda invisível: qualquer toque vira joystick (andar / pular para cima)
//  - direita: só a espada ataca e só o escudo defende
public class MobileControls : MonoBehaviour
{
    public static float Move { get; private set; }
    public static bool AttackHeld { get; private set; }
    public static bool BlockHeld { get; private set; }
    public static bool IsVisible { get; private set; }
    public static bool JumpHeld => _stickJump;

    static bool _stickJump;
    static bool _jumpPressed;
    static bool _specialPressed;
    static bool _dashPressed;
    static bool _forcedOn;
    static Sprite _circle;
    static Sprite _sword;
    static Sprite _shield;
    static Sprite _special;
    static Sprite _dash;

#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")]
    static extern int AetherionIsMobile();
#endif

    public static bool ConsumeJumpDown()
    {
        if (!_jumpPressed)
            return false;
        _jumpPressed = false;
        return true;
    }

    public static bool ConsumeSpecialPressed()
    {
        if (!_specialPressed)
            return false;
        _specialPressed = false;
        return true;
    }

    public static bool ConsumeDashPressed()
    {
        if (!_dashPressed)
            return false;
        _dashPressed = false;
        return true;
    }

    public static bool ShouldShow()
    {
        if (_forcedOn)
            return true;
        if (Application.isMobilePlatform)
            return true;

        string os = (SystemInfo.operatingSystem + " " + SystemInfo.deviceModel).ToLowerInvariant();
        if (os.Contains("android") || os.Contains("iphone") || os.Contains("ipad") || os.Contains("ios"))
            return true;

#if UNITY_WEBGL && !UNITY_EDITOR
        try
        {
            if (AetherionIsMobile() != 0)
                return true;
        }
        catch (System.Exception)
        {
        }
#endif
        return false;
    }

    public static void Attach(Transform owner)
    {
        if (owner == null)
            return;
        if (owner.GetComponentInChildren<MobileControls>(true) != null)
            return;

        UiKit.EnsureEventSystem();
        Input.multiTouchEnabled = true;

        var canvasGo = new GameObject("MobileCanvas");
        canvasGo.transform.SetParent(owner, false);
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 80;
        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 1f;
        canvasGo.AddComponent<GraphicRaycaster>();
        canvasGo.AddComponent<SafeAreaFit>();

        var pad = canvasGo.AddComponent<MobileControls>();
        pad.Build(canvasGo.transform);
        pad.ApplyVisibility(ShouldShow());
    }

    void Update()
    {
        if (!IsVisible && Input.touchCount > 0)
        {
            _forcedOn = true;
            ApplyVisibility(true);
        }
    }

    void OnDisable()
    {
        Move = 0f;
        _stickJump = false;
        AttackHeld = false;
        BlockHeld = false;
        _specialPressed = false;
        _dashPressed = false;
        IsVisible = false;
    }

    void ApplyVisibility(bool on)
    {
        IsVisible = on;
        for (int i = 0; i < transform.childCount; i++)
            transform.GetChild(i).gameObject.SetActive(on);
    }

    void Build(Transform parent)
    {
        BuildLeftZone(parent);

        var sword = IconButton(parent, "Espada", SwordSprite(), new Vector2(1f, 0f), new Vector2(-210f, 430f), 250f,
            new Color(0.18f, 0.1f, 0.22f, 0.88f));
        Hold(sword, () => AttackHeld = true, () => AttackHeld = false);

        var shield = IconButton(parent, "Escudo", ShieldSprite(), new Vector2(1f, 0f), new Vector2(-210f, 160f), 230f,
            new Color(0.08f, 0.14f, 0.28f, 0.88f));
        Hold(shield, () => BlockHeld = true, () => BlockHeld = false);

        // Especial (L/Q) e dash (Shift): toque único no mesmo padrão de botão.
        var special = IconButton(parent, "Especial", SpecialSprite(), new Vector2(1f, 0f), new Vector2(-470f, 300f), 200f,
            new Color(0.08f, 0.22f, 0.16f, 0.9f));
        Hold(special, () => _specialPressed = true, () => { });

        var dash = IconButton(parent, "Dash", DashSprite(), new Vector2(1f, 0f), new Vector2(-470f, 100f), 190f,
            new Color(0.28f, 0.18f, 0.06f, 0.9f));
        Hold(dash, () => _dashPressed = true, () => { });
    }

    void BuildLeftZone(Transform parent)
    {
        var zone = new GameObject("ZonaEsquerda");
        zone.transform.SetParent(parent, false);
        var image = zone.AddComponent<Image>();
        image.sprite = UiKit.WhiteSprite();
        image.color = new Color(1f, 1f, 1f, 0f); // barreira invisível, só captura o toque
        image.raycastTarget = true;
        var rect = zone.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 0f);
        rect.anchorMax = new Vector2(0.5f, 0.86f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        var visual = Circle(zone.transform, "Joystick", new Vector2(0.5f, 0.5f), Vector2.zero, 220f,
            new Color(1f, 1f, 1f, 0.22f));
        visual.raycastTarget = false;
        visual.gameObject.SetActive(false);
        var knob = Circle(visual.transform, "Knob", new Vector2(0.5f, 0.5f), Vector2.zero, 96f,
            new Color(0.93f, 0.78f, 0.38f, 0.95f));
        knob.raycastTarget = false;

        var stick = zone.AddComponent<FloatingStick>();
        stick.Setup(visual.rectTransform, knob.rectTransform);
    }

    static Image IconButton(Transform parent, string name, Sprite icon, Vector2 anchor, Vector2 position, float size, Color bg)
    {
        var image = Circle(parent, name, anchor, position, size, bg);
        var iconGo = new GameObject("Icone");
        iconGo.transform.SetParent(image.transform, false);
        var iconImage = iconGo.AddComponent<Image>();
        iconImage.sprite = icon;
        iconImage.raycastTarget = false;
        iconImage.preserveAspect = true;
        var iconRect = iconGo.GetComponent<RectTransform>();
        iconRect.anchorMin = iconRect.anchorMax = iconRect.pivot = new Vector2(0.5f, 0.5f);
        iconRect.sizeDelta = new Vector2(size * 0.62f, size * 0.62f);
        iconRect.anchoredPosition = Vector2.zero;
        return image;
    }

    static Image Circle(Transform parent, string name, Vector2 anchor, Vector2 position, float diameter, Color color)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var image = go.AddComponent<Image>();
        image.sprite = CircleSprite();
        image.color = color;
        image.raycastTarget = true;
        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = anchor;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(diameter, diameter);
        rect.anchoredPosition = position;
        return image;
    }

    static void Hold(Image image, System.Action down, System.Action up)
    {
        var hold = image.gameObject.AddComponent<HoldPad>();
        hold.Setup(down, up);
    }

    static Sprite CircleSprite()
    {
        if (_circle != null)
            return _circle;

        const int size = 64;
        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        var pixels = new Color32[size * size];
        float cx = (size - 1) * 0.5f;
        float r = size * 0.5f - 1.5f;
        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            float d = Vector2.Distance(new Vector2(x, y), new Vector2(cx, cx));
            if (d > r)
                pixels[y * size + x] = new Color32(0, 0, 0, 0);
            else if (d > r - 3f)
                pixels[y * size + x] = new Color32(255, 255, 255, 255);
            else
                pixels[y * size + x] = new Color32(255, 255, 255, 220);
        }

        texture.SetPixels32(pixels);
        texture.Apply();
        texture.filterMode = FilterMode.Bilinear;
        _circle = Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), size);
        return _circle;
    }

    static Sprite SpecialSprite()
    {
        if (_special != null)
            return _special;
        // arco simples (pixel)
        string[] art =
        {
            "................",
            "........g.......",
            ".......g.g......",
            "......g...g.....",
            ".....g.....g....",
            "....g.......g...",
            "...g.........g..",
            "...g.........g..",
            "....g.......g...",
            ".....g.....g....",
            "......g...g.....",
            ".......g.g......",
            "........g.......",
            "................",
            "................",
            "................",
        };
        _special = PixelSprite(art, ch => ch == 'g'
            ? new Color32(120, 235, 150, 255)
            : new Color32(0, 0, 0, 0));
        return _special;
    }

    static Sprite DashSprite()
    {
        if (_dash != null)
            return _dash;
        string[] art =
        {
            "................",
            "................",
            "....yy..........",
            "...yyyy.........",
            "..yyyyyy..yy....",
            ".yyyyyyyyyyyy...",
            "..yyyyyy..yy....",
            "...yyyy.........",
            "....yy..........",
            "................",
            "................",
            "................",
            "................",
            "................",
            "................",
            "................",
        };
        _dash = PixelSprite(art, ch => ch == 'y'
            ? new Color32(245, 200, 80, 255)
            : new Color32(0, 0, 0, 0));
        return _dash;
    }

    static Sprite SwordSprite()
    {
        if (_sword != null)
            return _sword;
        string[] art =
        {
            "......w.......",
            ".....wyw......",
            ".....wyw......",
            ".....wyw......",
            ".....wyw......",
            ".....wyw......",
            ".....wyw......",
            "....kwwwk.....",
            "....kkkkk.....",
            "......u.......",
            "......u.......",
            "......u.......",
            ".....uuu......",
            "......u.......",
        };
        _sword = PixelSprite(art, ch => ch switch
        {
            'w' => new Color32(230, 230, 236, 255),
            'y' => new Color32(255, 220, 90, 255),
            'k' => new Color32(168, 122, 48, 255),
            'u' => new Color32(92, 70, 48, 255),
            _ => new Color32(0, 0, 0, 0),
        });
        return _sword;
    }

    static Sprite ShieldSprite()
    {
        if (_shield != null)
            return _shield;
        string[] art =
        {
            "..kkkkkkkkkk..",
            ".kbbbbbbbbbbk.",
            "kbbwwwwwwwwbbk",
            "kbwwbbbbbbwwbk",
            "kbwbbwwwwbbwbk",
            "kbwbbwyywbbwbk",
            "kbwbbwwwwbbwbk",
            "kbwwbbbbbbwwbk",
            ".kbwwwwwwwwbk.",
            ".kbbwwwwwwbbk.",
            "..kbbbbbbbbk..",
            "...kbbbbbbk...",
            "....kbbbbk....",
            ".....kkkk.....",
        };
        _shield = PixelSprite(art, ch => ch switch
        {
            'w' => new Color32(210, 230, 255, 255),
            'b' => new Color32(70, 120, 210, 255),
            'y' => new Color32(255, 214, 70, 255),
            'k' => new Color32(24, 36, 70, 255),
            _ => new Color32(0, 0, 0, 0),
        });
        return _shield;
    }

    static Sprite PixelSprite(string[] art, System.Func<char, Color32> paint)
    {
        int height = art.Length;
        int width = art[0].Length;
        var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
        var pixels = new Color32[width * height];
        for (int y = 0; y < height; y++)
        {
            string row = art[height - 1 - y];
            for (int x = 0; x < width; x++)
                pixels[y * width + x] = paint(x < row.Length ? row[x] : '.');
        }

        texture.SetPixels32(pixels);
        texture.Apply();
        texture.filterMode = FilterMode.Point;
        return Sprite.Create(texture, new Rect(0f, 0f, width, height), new Vector2(0.5f, 0.5f), Mathf.Max(width, height));
    }

    class HoldPad : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        System.Action _down;
        System.Action _up;
        int _pointer = -1;

        public void Setup(System.Action down, System.Action up)
        {
            _down = down;
            _up = up;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            _pointer = eventData.pointerId;
            _down?.Invoke();
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (_pointer != eventData.pointerId && _pointer != -1)
                return;
            _pointer = -1;
            _up?.Invoke();
        }
    }

    class FloatingStick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        RectTransform _visual;
        RectTransform _knob;
        Vector2 _origin;
        int _pointer = -1;

        public void Setup(RectTransform visual, RectTransform knob)
        {
            _visual = visual;
            _knob = knob;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            _pointer = eventData.pointerId;
            _origin = eventData.position;
            if (_visual != null)
            {
                RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    transform as RectTransform, eventData.position, eventData.pressEventCamera, out var local);
                _visual.anchoredPosition = local;
                _visual.gameObject.SetActive(true);
                if (_knob != null)
                    _knob.anchoredPosition = Vector2.zero;
            }

            Apply(Vector2.zero);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (_pointer != eventData.pointerId && _pointer != -1)
                return;

            float radius = Mathf.Min(Screen.width, Screen.height) * 0.16f;
            Vector2 delta = eventData.position - _origin;
            Vector2 clamped = Vector2.ClampMagnitude(delta, radius);
            Apply(clamped / radius);

            if (_knob != null)
            {
                float uiRange = 88f;
                _knob.anchoredPosition = (clamped / radius) * uiRange;
            }
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (_pointer != eventData.pointerId && _pointer != -1)
                return;
            _pointer = -1;
            Move = 0f;
            _stickJump = false;
            if (_visual != null)
                _visual.gameObject.SetActive(false);
            if (_knob != null)
                _knob.anchoredPosition = Vector2.zero;
        }

        static void Apply(Vector2 dir)
        {
            Move = Mathf.Abs(dir.x) < 0.12f ? 0f : Mathf.Clamp(dir.x, -1f, 1f);
            bool jump = dir.y > 0.38f;
            if (jump && !_stickJump)
                _jumpPressed = true;
            _stickJump = jump;
        }
    }
}
