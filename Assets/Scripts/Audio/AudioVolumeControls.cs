using UnityEngine;
using UnityEngine.UI;

public static class AudioVolumeControls
{
    public static Slider Add(RectTransform parent, string label, float y, float width, bool music)
    {
        float value = PlayerPrefs.GetFloat(music ? "Audio.Music" : "Audio.Effects", music ? 0.65f : 0.8f);
        var text = MenuKit.Label(parent, label + " " + Mathf.RoundToInt(value * 100) + "%",
            MenuKit.BodyFont, 30, MenuKit.Ivory, 12, y, 82, 16);
        var root = MenuKit.Node(parent, label + "Volume", 96, y, width - 110, 16);
        var hit = root.gameObject.AddComponent<Image>();
        hit.color = new Color(0, 0, 0, 0.01f);
        var slider = root.gameObject.AddComponent<Slider>();
        slider.minValue = 0f;
        slider.maxValue = 1f;
        var track = MenuKit.Img(root, UiKit.WhiteSprite(), 0, 6, width - 110, 4, raycast: false);
        track.color = MenuKit.Dim * new Color(1, 1, 1, .4f);
        var fill = MenuKit.Img(root, UiKit.WhiteSprite(), 0, 6, width - 110, 4, raycast: false);
        fill.color = MenuKit.GoldDark;
        // Slider drives anchors; start with zero offsets in a dedicated area.
        var fillArea = MenuKit.Node(root, "FillArea", 2, 6, width - 114, 4);
        fill.transform.SetParent(fillArea, false);
        fill.rectTransform.offsetMin = fill.rectTransform.offsetMax = Vector2.zero;
        slider.fillRect = fill.rectTransform;
        var handleArea = MenuKit.Node(root, "HandleArea", 3, 0, width - 116, 16);
        var handle = MenuKit.Img(handleArea, UiKit.WhiteSprite(), 0, 2, 6, 12, raycast: true);
        handle.color = MenuKit.Gold;
        handle.rectTransform.pivot = new Vector2(.5f, .5f);
        handle.rectTransform.anchorMin = new Vector2(0, 0);
        handle.rectTransform.anchorMax = new Vector2(0, 1);
        handle.rectTransform.sizeDelta = new Vector2(6 * MenuKit.Px, -4 * MenuKit.Px);
        handle.rectTransform.anchoredPosition = Vector2.zero;
        slider.handleRect = handle.rectTransform;
        slider.targetGraphic = handle;
        slider.SetValueWithoutNotify(value);
        slider.onValueChanged.AddListener(v =>
        {
            text.text = label + " " + Mathf.RoundToInt(v * 100) + "%";
            float other = PlayerPrefs.GetFloat(music ? "Audio.Effects" : "Audio.Music", music ? .8f : .65f);
            GameAudio.SaveVolumes(music ? v : other, music ? other : v);
        });
        return slider;
    }

    public static void Link(Selectable first, Slider music, Slider effects, Selectable last)
    {
        // Preserve the button chain while allowing vertical navigation into audio rows.
        var nav = first.navigation;
        nav.selectOnUp = effects;
        first.navigation = nav;
        nav = last.navigation;
        nav.selectOnDown = music;
        last.navigation = nav;
        music.navigation = new Navigation { mode = Navigation.Mode.Explicit, selectOnUp = last, selectOnDown = effects };
        effects.navigation = new Navigation { mode = Navigation.Mode.Explicit, selectOnUp = music, selectOnDown = first };
    }
}
