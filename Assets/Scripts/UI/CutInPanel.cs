using System.Collections;
using UnityEngine;
using UnityEngine.UI;

// Painel lateral do Especial Supremo: terço direito, três batidas
// (olhos, busto com grito, faixa do nome). Corre em tempo real (unscaled).
public class CutInPanel : MonoBehaviour
{
    public static CutInPanel Current { get; private set; }

    const float PanelWidth = 640f;
    const float SlideTime = 0.18f;
    const float BeatEyes = 0.36f;
    const float BeatBust = 0.42f;
    const float BeatName = 0.64f;
    const int PixelScale = 8;

    static Sprite _lines;

    Image _eyes;
    Image _bust;
    GameObject _shoutRoot;
    Text _shout;
    GameObject _bannerRoot;
    Text _name;
    RectTransform _panel;
    Image _speed;

    public static void Cancel()
    {
        if (Current != null)
            Object.Destroy(Current.gameObject);
    }

    public static IEnumerator Play(SpecialData data, CharacterData hero)
    {
        var go = new GameObject("CutIn");
        var panel = go.AddComponent<CutInPanel>();
        Current = panel;
        yield return panel.Run(data, hero);
        if (Current == panel)
            Current = null;
        if (go != null)
            Object.Destroy(go);
    }

    void OnDestroy()
    {
        if (Current == this)
            Current = null;
    }

    IEnumerator Run(SpecialData data, CharacterData hero)
    {
        string heroId = data != null && !string.IsNullOrEmpty(data.HeroId)
            ? data.HeroId
            : hero != null ? hero.Id : "guerreiro";
        var accent = data != null ? data.Accent : new Color(1f, 0.84f, 0.25f);
        var dark = data != null ? data.PanelDark : new Color(0.16f, 0.03f, 0.04f, 0.96f);
        var light = data != null ? data.PanelLight : new Color(0.5f, 0.08f, 0.06f, 0.45f);

        var canvas = UiKit.CreateCanvas(transform, "CutInCanvas");
        canvas.overrideSorting = true;
        canvas.sortingOrder = 500;

        var topBar = UiKit.Panel(canvas.transform, "LetterboxTopo", new Vector2(1920f, 48f), new Vector2(0f, 516f), Color.black);
        var bottomBar = UiKit.Panel(canvas.transform, "LetterboxBase", new Vector2(1920f, 48f), new Vector2(0f, -516f), Color.black);

        var root = UiKit.Panel(canvas.transform, "Painel", new Vector2(PanelWidth, 1080f), Vector2.zero, dark);
        _panel = root.rectTransform;
        _panel.anchorMin = _panel.anchorMax = new Vector2(1f, 0.5f);
        _panel.pivot = new Vector2(1f, 0.5f);
        _panel.sizeDelta = new Vector2(PanelWidth, 1080f);
        _panel.anchoredPosition = new Vector2(PanelWidth + 40f, 0f);

        _speed = UiKit.Panel(root.transform, "SpeedLines", new Vector2(PanelWidth + 80f, 1080f), Vector2.zero, light);
        _speed.sprite = SpeedLines();
        _speed.color = new Color(light.r, light.g, light.b, 0.55f);
        _speed.raycastTarget = false;

        _eyes = SpriteImage(root.transform, "Olhos", HeroCutInSprites.Eyes(heroId, false));
        _bust = SpriteImage(root.transform, "Busto", HeroCutInSprites.Bust(heroId, data != null ? data.BustFrame : 0));
        _bust.gameObject.SetActive(false);

        _shoutRoot = ShoutBubble(root.transform, accent);
        _shout = _shoutRoot.GetComponentInChildren<Text>();
        if (_shout != null)
            _shout.text = data != null ? data.Callout : "";
        _shoutRoot.SetActive(false);

        _bannerRoot = Banner(root.transform, accent, out _name);
        _bannerRoot.SetActive(false);

        Frame(root.transform, accent);
        topBar.transform.SetAsLastSibling();
        bottomBar.transform.SetAsLastSibling();

        yield return Slide(_panel, PanelWidth + 40f, 0f, SlideTime, true);
        yield return PlayEyes(heroId);
        yield return PlayBust();
        yield return PlayName(data != null ? data.DisplayName : "");
        _shoutRoot.SetActive(false);
        yield return Slide(_panel, 0f, PanelWidth + 80f, SlideTime, false);

        // letterbox sai junto — o canvas inteiro é destruído pelo Play().
        topBar.enabled = false;
        bottomBar.enabled = false;
    }

    IEnumerator PlayEyes(string heroId)
    {
        Place(_eyes.rectTransform, 0f, 30f, Scaled(_eyes.sprite, PixelScale + 2));
        float clock = 0f;
        float flip = 0f;
        bool bright = false;
        while (clock < BeatEyes)
        {
            float dt = Time.unscaledDeltaTime;
            clock += dt;
            flip += dt;
            if (flip >= 0.08f)
            {
                flip = 0f;
                bright = !bright;
                var next = HeroCutInSprites.Eyes(heroId, bright);
                if (next != null)
                    _eyes.sprite = next;
                if (_speed != null)
                {
                    var scale = _speed.rectTransform.localScale;
                    scale.x = scale.x >= 0f ? -1f : 1f;
                    _speed.rectTransform.localScale = scale;
                }
            }

            if (clock > BeatEyes * 0.55f)
            {
                _eyes.rectTransform.anchoredPosition = new Vector2(0f, 30f) + new Vector2(
                    Random.Range(-3f, 3f),
                    Random.Range(-3f, 3f));
            }

            yield return null;
        }

        _eyes.rectTransform.anchoredPosition = new Vector2(0f, 30f);
    }

    IEnumerator PlayBust()
    {
        var eyesFrom = _eyes.rectTransform.anchoredPosition;
        var eyesFromSize = _eyes.rectTransform.sizeDelta;
        var eyesTo = new Vector2(0f, 300f);
        var eyesToSize = Scaled(_eyes.sprite, 4);
        var bustSize = Scaled(_bust.sprite, 8);
        Place(_bust.rectTransform, 0f, 10f, bustSize);
        _bust.gameObject.SetActive(true);
        _bust.color = new Color(1f, 1f, 1f, 0f);
        _shoutRoot.SetActive(true);

        float clock = 0f;
        const float intro = 0.14f;
        while (clock < BeatBust)
        {
            clock += Time.unscaledDeltaTime;
            float u = Mathf.Clamp01(clock / intro);
            u = 1f - Mathf.Pow(1f - u, 3f);
            _eyes.rectTransform.anchoredPosition = Vector2.Lerp(eyesFrom, eyesTo, u);
            _eyes.rectTransform.sizeDelta = Vector2.Lerp(eyesFromSize, eyesToSize, u);
            var color = _bust.color;
            color.a = Mathf.Clamp01(clock / 0.12f);
            _bust.color = color;
            if (_speed != null && (int)(clock * 16f) % 2 == 0)
            {
                var scale = _speed.rectTransform.localScale;
                scale.x = Mathf.Abs(scale.x);
                _speed.rectTransform.localScale = scale;
            }
            else if (_speed != null)
            {
                var scale = _speed.rectTransform.localScale;
                scale.x = -Mathf.Abs(scale.x);
                _speed.rectTransform.localScale = scale;
            }

            yield return null;
        }
    }

    IEnumerator PlayName(string displayName)
    {
        if (displayName == null)
            displayName = "";
        _bannerRoot.SetActive(true);
        _name.text = "";
        float clock = 0f;
        int shown = -1;
        while (clock < BeatName)
        {
            clock += Time.unscaledDeltaTime;
            float u = Mathf.Clamp01(clock / (BeatName * 0.78f));
            int typed = displayName.Length == 0 ? 0 : Mathf.Clamp(Mathf.CeilToInt(displayName.Length * u), 0, displayName.Length);
            if (typed != shown)
            {
                shown = typed;
                _name.text = displayName.Substring(0, typed);
            }

            if (_panel != null)
                _panel.anchoredPosition = new Vector2(Random.Range(-5f, 5f), Random.Range(-3f, 3f));
            yield return null;
        }

        _name.text = displayName;
        if (_panel != null)
            _panel.anchoredPosition = Vector2.zero;
        if (_shoutRoot != null)
            _shoutRoot.SetActive(false);
    }

    static IEnumerator Slide(RectTransform rect, float from, float to, float time, bool easeOut)
    {
        if (rect == null)
            yield break;

        for (float t = 0f; t < time; t += Time.unscaledDeltaTime)
        {
            float u = Mathf.Clamp01(t / time);
            if (easeOut)
                u = 1f - Mathf.Pow(1f - u, 3f);
            rect.anchoredPosition = new Vector2(Mathf.Lerp(from, to, u), 0f);
            yield return null;
        }

        rect.anchoredPosition = new Vector2(to, 0f);
    }

    static Image SpriteImage(Transform parent, string name, Sprite sprite)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var image = go.AddComponent<Image>();
        image.sprite = sprite != null ? sprite : SupremeFx.DotSprite();
        image.preserveAspect = true;
        image.raycastTarget = false;
        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        return image;
    }

    static void Place(RectTransform rect, float x, float y, Vector2 size)
    {
        if (rect == null)
            return;
        rect.anchoredPosition = new Vector2(x, y);
        rect.sizeDelta = size;
    }

    static Vector2 Scaled(Sprite sprite, int scale)
    {
        if (sprite == null)
            return new Vector2(64f * scale, 64f * scale);
        scale = Mathf.Max(1, scale);
        return new Vector2(Mathf.Round(sprite.rect.width) * scale, Mathf.Round(sprite.rect.height) * scale);
    }

    static GameObject ShoutBubble(Transform parent, Color accent)
    {
        var frame = UiKit.Panel(parent, "Balao", new Vector2(528f, 138f), new Vector2(0f, -170f), accent);
        var bg = UiKit.Panel(frame.transform, "Miolo", new Vector2(512f, 122f), Vector2.zero, Color.white);
        bg.raycastTarget = false;
        var label = UiKit.Label(bg.transform, "Grito", 32, Vector2.zero, new Color(0.08f, 0.05f, 0.06f), new Vector2(480f, 108f));
        label.raycastTarget = false;
        return frame.gameObject;
    }

    static GameObject Banner(Transform parent, Color accent, out Text name)
    {
        var bg = UiKit.Panel(parent, "FaixaNome", new Vector2(600f, 150f), new Vector2(0f, -360f), new Color(0.05f, 0.03f, 0.05f, 0.94f));
        var line = UiKit.Panel(bg.transform, "Filete", new Vector2(560f, 6f), new Vector2(0f, 58f), accent);
        line.raycastTarget = false;
        name = UiKit.Label(bg.transform, "Nome", 30, new Vector2(0f, -8f), accent, new Vector2(560f, 110f));
        name.raycastTarget = false;
        name.horizontalOverflow = HorizontalWrapMode.Wrap;
        name.verticalOverflow = VerticalWrapMode.Overflow;
        return bg.gameObject;
    }

    static void Frame(Transform parent, Color accent)
    {
        UiKit.Panel(parent, "BordaEsq", new Vector2(8f, 1080f), new Vector2(-316f, 0f), accent).raycastTarget = false;
        UiKit.Panel(parent, "BordaDir", new Vector2(8f, 1080f), new Vector2(316f, 0f), accent).raycastTarget = false;
        UiKit.Panel(parent, "BordaTopo", new Vector2(640f, 8f), new Vector2(0f, 536f), accent).raycastTarget = false;
        UiKit.Panel(parent, "BordaBase", new Vector2(640f, 8f), new Vector2(0f, -536f), accent).raycastTarget = false;
    }

    static Sprite SpeedLines()
    {
        if (_lines != null)
            return _lines;

        const int w = 160;
        const int h = 90;
        var pixels = new Color32[w * h];
        var rng = new System.Random(7);
        for (int i = 0; i < 80; i++)
        {
            int y = rng.Next(h);
            int x = rng.Next(-30, w);
            int len = rng.Next(18, 80);
            byte alpha = (byte)rng.Next(90, 170);
            for (int k = 0; k < len; k++)
            {
                int px = x + k;
                if ((uint)px >= (uint)w)
                    continue;
                pixels[y * w + px] = new Color32(255, 255, 255, alpha);
            }
        }

        _lines = SupremeFx.MakeSprite(w, h, pixels, 16f, new Vector2(0.5f, 0.5f));
        return _lines;
    }
}
