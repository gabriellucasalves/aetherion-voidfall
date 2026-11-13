using UnityEngine;
using UnityEngine.UI;

public class BossHealthBar : MonoBehaviour
{
    static Sprite _frameSprite;
    static Sprite _fillSprite;

    public static void EnsureSprites()
    {
        if (_frameSprite != null) return;
        _frameSprite = LoadSprite("boss_bar_frame");
        _fillSprite = LoadSprite("boss_bar_fill");
    }

    static Sprite LoadSprite(string name)
    {
        var tex = Resources.Load<Texture2D>("Bosses/DevoradorDoVazio/" + name);
        if (tex == null) return UiKit.WhiteSprite();
        tex.filterMode = FilterMode.Point;
        return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 1f);
    }

    public static BossHealthBar Create(Transform canvasTransform)
    {
        EnsureSprites();
        var root = new GameObject("BossHealthBarRoot");
        root.transform.SetParent(canvasTransform, false);

        var rt = root.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = new Vector2(0f, 230f);
        rt.sizeDelta = new Vector2(900f, 60f);

        var bar = root.AddComponent<BossHealthBar>();
        bar.Build();
        root.SetActive(false); // Initially hidden during intro
        return bar;
    }

    readonly Image[] _frames = new Image[3];
    readonly Image[] _fills = new Image[3];
    public int RemainingBars => 4 - _form;
    public int VisibleBars { get { int count = 0; foreach (var frame in _frames) if (frame != null && frame.gameObject.activeSelf) count++; return count; } }
    Image _frame;
    Image _fill;
    Text _nameText;
    float _targetNormalized = 1f;
    float _displayNormalized = 0f;
    bool _isFillingIntro;
    float _introFillTimer;

    public float FillAmount => _fill != null ? _fill.fillAmount : 0f;
    public float TargetNormalized => _targetNormalized;

    int _form = 1;
    public void SetForm(int form)
    {
        _form = Mathf.Clamp(form, 1, 3);
        if (_nameText != null) _nameText.text = BossData.DisplayName;
        for (int i = 0; i < _frames.Length; i++)
        {
            if (_frames[i] == null) continue;
            _frames[i].gameObject.SetActive(i < RemainingBars);
            if (i > 0) _fills[i].fillAmount = 1f;
        }
    }

    public void ForceSync()
    {
        _isFillingIntro = false;
        _displayNormalized = _targetNormalized;
        if (_fill != null) _fill.fillAmount = _targetNormalized;
    }

    void Build()
    {
        // Title Text above bar
        var titleGo = new GameObject("BossTitle");
        titleGo.transform.SetParent(transform, false);
        _nameText = titleGo.AddComponent<Text>();
        _nameText.font = UiKit.ResolveFont();
        _nameText.text = BossData.DisplayName;
        _nameText.fontSize = 20;
        _nameText.alignment = TextAnchor.MiddleCenter;
        _nameText.color = new Color(1f, 0.88f, 0.55f);
        var titleRt = titleGo.GetComponent<RectTransform>();
        titleRt.anchoredPosition = new Vector2(0f, 26f);
        titleRt.sizeDelta = new Vector2(960f, 28f);

        // Only remaining life pools are drawn. The top row is the current form;
        // muted rows below it are the upcoming forms, not extra armor.
        for (int i = 0; i < 3; i++)
        {
            var frameGo = new GameObject("BarFrame" + i);
            frameGo.transform.SetParent(transform, false);
            var frame = frameGo.AddComponent<Image>();
            frame.sprite = _frameSprite;
            frame.raycastTarget = false;
            var frameRt = frameGo.GetComponent<RectTransform>();
            frameRt.anchoredPosition = new Vector2(0f, -i * 22f);
            frameRt.sizeDelta = new Vector2(900f, 20f);
            frame.color = i == 0 ? Color.white : new Color(.70f, .63f, .85f, .80f);
            _frames[i] = frame;
            var fillGo = new GameObject("BarFill" + i);
            fillGo.transform.SetParent(frameGo.transform, false);
            var fill = fillGo.AddComponent<Image>();
            fill.sprite = i == 0 ? _fillSprite : UiKit.WhiteSprite();
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Horizontal;
            fill.fillOrigin = 0;
            fill.fillAmount = 1f;
            fill.raycastTarget = false;
            fill.color = i == 0 ? new Color(.92f, .18f, .28f) : new Color(.55f, .30f, .75f, .95f);
            var fillRt = fillGo.GetComponent<RectTransform>();
            fillRt.anchoredPosition = Vector2.zero;
            fillRt.sizeDelta = new Vector2(888f, 10f);
            _fills[i] = fill;
        }
        _frame = _frames[0];
        _fill = _fills[0];

        SetForm(1);
    }

    void CreateTickMark(Transform parent, float normalized)
    {
        var tickGo = new GameObject("Tick_" + Mathf.RoundToInt(normalized * 100f));
        tickGo.transform.SetParent(parent, false);
        var img = tickGo.AddComponent<Image>();
        img.color = new Color(1f, 0.92f, 0.55f, 0.95f);
        img.raycastTarget = false;
        var rt = tickGo.GetComponent<RectTransform>();
        float x = (normalized - 0.5f) * 888f;
        rt.anchoredPosition = new Vector2(x, 0f);
        rt.sizeDelta = new Vector2(2f, 20f);
    }

    public void BeginIntroFill(float duration = 1.0f)
    {
        gameObject.SetActive(true);
        _isFillingIntro = true;
        _introFillTimer = duration;
        _displayNormalized = 0f;
        _targetNormalized = 1f;
        if (_fill != null) _fill.fillAmount = 0f;
    }

    public void SetHealth(float current, float max)
    {
        _targetNormalized = Mathf.Clamp01(current / max);
    }

    void Update()
    {
        if (_isFillingIntro)
        {
            _introFillTimer -= Time.deltaTime;
            _displayNormalized = Mathf.MoveTowards(_displayNormalized, 1f, Time.deltaTime * 1.25f);
            if (_fill != null) _fill.fillAmount = _displayNormalized;
            if (_displayNormalized >= 1f && _introFillTimer <= 0f)
            {
                _isFillingIntro = false;
                _displayNormalized = 1f;
            }
            return;
        }

        if (_fill != null)
        {
            _displayNormalized = Mathf.MoveTowards(_displayNormalized, _targetNormalized, 1.5f * Time.deltaTime);
            _fill.fillAmount = _displayNormalized;

            // Subtle color tint shifts with phase
            if (_form == 3)
            {
                // Phase 3 Fúria: deep crimson
                _fill.color = new Color(1f, 0.12f, 0.20f);
            }
            else if (_form == 2)
            {
                // Phase 2: vibrant scarlet
                _fill.color = new Color(0.95f, 0.22f, 0.32f);
            }
            else
            {
                // Phase 1: ruby red
                _fill.color = new Color(0.88f, 0.25f, 0.35f);
            }
        }
    }
}

