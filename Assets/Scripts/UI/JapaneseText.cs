using UnityEngine;

// Bundled glyphs, rather than OS fallbacks, keep Japanese readable in WebGL/mobile.
public static class JapaneseText
{
    static Font _font;
    public static Font Font => _font != null ? _font : (_font = Resources.Load<Font>("Menu/fontes/AetherionSupremeJP"));
    public static bool ContainsJapanese(string text)
    {
        if (string.IsNullOrEmpty(text)) return false;
        foreach (char c in text)
            if ((c >= '\u3040' && c <= '\u30ff') || (c >= '\u3400' && c <= '\u9fff')) return true;
        return false;
    }
    public static Font For(string text, Font fallback) => ContainsJapanese(text) && Font != null ? Font : fallback;
}
