using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public static class UiKit
{
    static Sprite _white;

    public static Sprite WhiteSprite()
    {
        if (_white != null)
            return _white;

        var texture = new Texture2D(8, 8, TextureFormat.RGBA32, false);
        var pixels = new Color32[64];
        for (int i = 0; i < pixels.Length; i++)
            pixels[i] = new Color32(255, 255, 255, 255);
        texture.SetPixels32(pixels);
        texture.Apply();
        texture.filterMode = FilterMode.Point;
        texture.wrapMode = TextureWrapMode.Clamp;
        _white = Sprite.Create(texture, new Rect(0f, 0f, 8f, 8f), new Vector2(0.5f, 0.5f), 8f, 0, SpriteMeshType.FullRect);
        return _white;
    }
    public static Canvas CreateCanvas(Transform parent, string name)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);

        var canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        var scaler = go.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        go.AddComponent<GraphicRaycaster>();
        EnsureEventSystem();
        return canvas;
    }

    public static Text Label(Transform parent, string text, int size, Vector2 position, Color color, Vector2 sizeDelta)
    {
        var go = new GameObject(text);
        go.transform.SetParent(parent, false);
        var label = go.AddComponent<Text>();
        label.text = text;
        label.font = ResolveFont();
        label.fontSize = size;
        label.alignment = TextAnchor.MiddleCenter;
        label.color = color;
        label.horizontalOverflow = HorizontalWrapMode.Wrap;
        label.verticalOverflow = VerticalWrapMode.Overflow;
        var rect = go.GetComponent<RectTransform>();
        rect.sizeDelta = sizeDelta;
        rect.anchoredPosition = position;
        return label;
    }

    public static Button Button(Transform parent, string text, Vector2 position, Vector2 size, UnityAction onClick)
    {
        var go = new GameObject(text);
        go.transform.SetParent(parent, false);

        var image = go.AddComponent<Image>();
        image.sprite = WhiteSprite();
        image.color = MenuTheme.Button;

        var button = go.AddComponent<Button>();
        var colors = button.colors;
        colors.highlightedColor = MenuTheme.ButtonHover;
        colors.pressedColor = MenuTheme.RiftMagenta;
        button.colors = colors;
        button.onClick.AddListener(onClick);

        var rect = go.GetComponent<RectTransform>();
        rect.sizeDelta = size;
        rect.anchoredPosition = position;
        Label(go.transform, text, 22, Vector2.zero, MenuTheme.SoftIvory, size);
        return button;
    }

    public static Image Panel(Transform parent, string name, Vector2 size, Vector2 position, Color color)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var image = go.AddComponent<Image>();
        image.sprite = WhiteSprite();
        image.color = color;
        image.raycastTarget = true;
        var rect = go.GetComponent<RectTransform>();
        rect.sizeDelta = size;
        rect.anchoredPosition = position;
        return image;
    }

    public static Image Bar(Transform parent, string name, Vector2 position, Vector2 size, Color background, Color fill)
    {
        var track = Panel(parent, name, size, position, background);
        var fillImage = Panel(track.transform, "Fill", size, Vector2.zero, fill);
        var rect = fillImage.rectTransform;
        rect.anchorMin = new Vector2(0f, 0f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = new Vector2(size.x, 0f);
        return fillImage;
    }

    public static void SetBar(Image fill, float ratio, float width)
    {
        if (fill == null)
            return;

        var rect = fill.rectTransform;
        rect.sizeDelta = new Vector2(width * Mathf.Clamp01(ratio), 0f);
    }

    public static Font ResolveFont()
    {
        var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (font != null)
            return font;

        font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        if (font != null)
            return font;

        return Font.CreateDynamicFontFromOSFont(new[] { "Helvetica Neue", "Helvetica", "Arial" }, 24);
    }

    public static void EnsureEventSystem()
    {
        if (Object.FindFirstObjectByType<EventSystem>() != null)
            return;

        var go = new GameObject("EventSystem");
        go.AddComponent<EventSystem>();
        go.AddComponent<StandaloneInputModule>();
    }
}
