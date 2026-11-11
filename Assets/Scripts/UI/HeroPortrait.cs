using UnityEngine;
using UnityEngine.UI;

// Retratos animados dos heróis para menus e mapa: usa os modelos novos (Resources/TrioAnimations/<id>/idle|andar,
// células 64x64, pés na linha y=61 a partir do topo, corpo centrado em x≈26.5).
// Attach(...) mantém a assinatura antiga (centro visual na posição) para não quebrar chamadas existentes.
// AttachFeet(...) é o novo padrão: pivô NOS PÉS + escala inteira (pixel perfeito).
public static class HeroPortrait
{
    const int Cell = 64;
    const int Cols = 7;
    public static readonly Vector2 FeetPivot = new Vector2(26.5f / 64f, 3f / 64f);
    public static readonly Vector2 CenterPivot = new Vector2(26.5f / 64f, 16.5f / 64f);

    static Sprite[] _guerreiroIdle, _magoIdle, _arqueiroIdle;
    static Sprite[] _guerreiroWalk, _magoWalk, _arqueiroWalk;

    public static Sprite[] GuerreiroIdle() { return Frames(ref _guerreiroIdle, "guerreiro", "idle", "Guerreiro/sheet", 0, 4); }
    public static Sprite[] MagoIdle() { return Frames(ref _magoIdle, "mago", "idle", "Mago/sheet", 0, 4); }
    public static Sprite[] ArqueiroIdle() { return Frames(ref _arqueiroIdle, "arqueiro", "idle", "Arqueiro/sheet", 0, 4); }

    public static Sprite[] IdleFrames(string heroId)
    {
        return heroId == "mago" ? MagoIdle() : heroId == "arqueiro" ? ArqueiroIdle() : heroId == "guerreiro" ? GuerreiroIdle() : null;
    }

    // andar (6 frames) dos modelos novos; fallback = células 4..9 do sheet antigo
    public static Sprite[] WalkFrames(string heroId)
    {
        if (heroId == "mago") return Frames(ref _magoWalk, "mago", "andar", "Mago/sheet", 4, 6);
        if (heroId == "arqueiro") return Frames(ref _arqueiroWalk, "arqueiro", "andar", "Arqueiro/sheet", 4, 6);
        return Frames(ref _guerreiroWalk, "guerreiro", "andar", "Guerreiro/sheet", 4, 6);
    }

    static Sprite[] Frames(ref Sprite[] cache, string heroId, string anim, string fallbackPath, int fallbackStart, int fallbackCount)
    {
        if (cache != null) return cache;

        var animTex = Resources.Load<Texture2D>("TrioAnimations/" + heroId + "/" + anim);
        if (animTex != null)
        {
            animTex.filterMode = FilterMode.Point;
            animTex.wrapMode = TextureWrapMode.Clamp;
            int count = Mathf.Max(1, animTex.width / Cell);      // TODOS os frames (arqueiro idle tem 2)
            var frames = new Sprite[count];
            for (int i = 0; i < count; i++)
                frames[i] = Sprite.Create(animTex, new Rect(i * Cell, 0, Cell, Cell), FeetPivot, Cell, 0, SpriteMeshType.FullRect);
            return cache = frames;
        }

        var texture = Resources.Load<Texture2D>(fallbackPath);
        if (texture == null) return null;
        texture.filterMode = FilterMode.Point;
        texture.wrapMode = TextureWrapMode.Clamp;
        var sheetFrames = new Sprite[fallbackCount];
        for (int i = 0; i < fallbackCount; i++)
        {
            int cell = fallbackStart + i;
            int x = (cell % Cols) * Cell;
            int y = texture.height - (cell / Cols + 1) * Cell;
            sheetFrames[i] = Sprite.Create(texture, new Rect(x, y, Cell, Cell), FeetPivot, Cell, 0, SpriteMeshType.FullRect);
        }
        return cache = sheetFrames;
    }

    // ANTIGO (compatível): position = centro visual do personagem; sizePx = lado da célula 64x64 na UI
    public static Image Attach(Transform parent, string heroId, Vector2 position, float sizePx)
    {
        var img = Create(parent, heroId, sizePx);
        if (img == null) return null;
        var rect = img.rectTransform;
        rect.pivot = CenterPivot;
        rect.anchoredPosition = position;
        return img;
    }

    // NOVO: feetPos = ponto do chão (anchoredPosition no pai); scale = unidades UI por pixel do sprite (inteiro: 2 no mapa, 4, 8)
    public static Image AttachFeet(Transform parent, string heroId, Vector2 feetPos, int scale)
    {
        var img = Create(parent, heroId, Cell * Mathf.Max(1, scale));
        if (img == null) return null;
        var rect = img.rectTransform;
        rect.pivot = FeetPivot;
        rect.anchoredPosition = feetPos;
        return img;
    }

    // atalho para layouts em pixels nativos do MenuKit (origem no topo-esquerdo do pai).
    // uiScale = unidades UI por pixel do sprite: 8 no título e nos cards (= 2 px nativos), 4 no painel da intro.
    public static Image AttachFeetNative(RectTransform parent, string heroId, float feetX, float feetY, int uiScale)
    {
        var img = AttachFeet(parent, heroId, Vector2.zero, uiScale);
        if (img == null) return null;
        var rt = img.rectTransform;
        rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
        rt.anchoredPosition = new Vector2(feetX * MenuKit.Px, -feetY * MenuKit.Px);
        return img;
    }

    static Image Create(Transform parent, string heroId, float sizePx)
    {
        var frames = IdleFrames(heroId);
        if (frames == null) return null;
        var go = new GameObject("Retrato_" + heroId);
        go.transform.SetParent(parent, false);
        var image = go.AddComponent<Image>();
        image.sprite = frames[0];
        image.preserveAspect = true;
        image.raycastTarget = false;
        var rect = go.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(sizePx, sizePx);
        go.AddComponent<UiSpriteAnimator>().Setup(image, frames, heroId == "arqueiro" ? 2.4f : 3.2f);
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

    public void SetFrames(Sprite[] frames, float fps)
    {
        if (frames == null || frames == _frames) return;
        _frames = frames; _fps = fps; _clock = 0f;
    }

    void Update()
    {
        if (_image == null || _frames == null || _frames.Length == 0) return;
        _clock += Time.unscaledDeltaTime;
        _image.sprite = _frames[(int)(_clock * _fps) % _frames.Length];
    }
}
