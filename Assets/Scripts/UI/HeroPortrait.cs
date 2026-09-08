using UnityEngine;
using UnityEngine.UI;

// Retratos animados dos heróis para os menus: recorta o mesmo sheet usado
// no jogo (Resources/Guerreiro/sheet.png) em sprites de UI e anima o idle.
public static class HeroPortrait
{
    const int Cell = 64;
    const int Cols = 7;

    static Sprite[] _guerreiroIdle;

    // frames de idle do guerreiro (células 0..3 do sheet, linha de cima)
    public static Sprite[] GuerreiroIdle()
    {
        if (_guerreiroIdle != null)
            return _guerreiroIdle;

        var texture = Resources.Load<Texture2D>("Guerreiro/sheet");
        if (texture == null)
            return null;

        texture.filterMode = FilterMode.Point;
        texture.wrapMode = TextureWrapMode.Clamp;

        var frames = new Sprite[4];
        for (int i = 0; i < frames.Length; i++)
        {
            int x = (i % Cols) * Cell;
            int y = texture.height - (i / Cols + 1) * Cell;
            frames[i] = Sprite.Create(
                texture, new Rect(x, y, Cell, Cell), new Vector2(0.5f, 0.5f), Cell, 0, SpriteMeshType.FullRect);
        }

        _guerreiroIdle = frames;
        return frames;
    }

    // coloca o retrato animado num canvas; retorna null se o herói ainda não tem arte
    public static Image Attach(Transform parent, string heroId, Vector2 position, float sizePx)
    {
        Sprite[] frames = heroId == "guerreiro" ? GuerreiroIdle() : null;
        if (frames == null)
            return null;

        var go = new GameObject("Retrato_" + heroId);
        go.transform.SetParent(parent, false);
        var image = go.AddComponent<Image>();
        image.sprite = frames[0];
        image.preserveAspect = true;
        image.raycastTarget = false;
        var rect = go.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(sizePx, sizePx);
        rect.anchoredPosition = position;

        go.AddComponent<UiSpriteAnimator>().Setup(image, frames, 3.2f);
        return image;
    }
}

// cicla sprites numa Image de UI (idle dos retratos)
public class UiSpriteAnimator : MonoBehaviour
{
    Image _image;
    Sprite[] _frames;
    float _fps = 4f;
    float _clock;

    public void Setup(Image image, Sprite[] frames, float fps)
    {
        _image = image;
        _frames = frames;
        _fps = fps;
        _clock = Random.Range(0f, 10f);
    }

    void Update()
    {
        if (_image == null || _frames == null || _frames.Length == 0)
            return;

        _clock += Time.deltaTime;
        _image.sprite = _frames[(int)(_clock * _fps) % _frames.Length];
    }
}
