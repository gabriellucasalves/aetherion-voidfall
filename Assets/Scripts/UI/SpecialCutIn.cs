using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Cut-in de especial estilo fighting game 8/16-bit (MvC / KOF / anime anos 90):
/// quando o especial dispara (PlayerCombat.IsCastingSpecial), um overlay curto
/// (~0.45s) entra na tela com flash + faixa diagonal + busto do heroi.
///
/// Arte: Assets/Resources/CutIns/&lt;id&gt;.png (grade 5x2, 10 frames 240x135) — tira de 8 frames 240x135
/// gerada por Assets/Art/CutIns/build_cutins.py (mapa em FRAME_MAP.md).
/// Anexado por HeroAppearance.Build; sem sheet, o componente fica inerte.
/// </summary>
public class SpecialCutIn : MonoBehaviour
{
    const int FrameW = 240;
    const int FrameH = 135;
    const int FrameCount = 10;
    const int GridCols = 5;
    const float Fps = 14f; // ~0.71s: mais legivel

    Sprite[] _frames;
    PlayerCombat _combat;
    bool _bound;
    bool _wasCasting;
    GameObject _overlay;
    Image _image;
    float _clock;
    int _index;

    public static void Attach(Transform parent, CharacterData hero)
    {
        if (parent == null || hero == null || string.IsNullOrEmpty(hero.Id))
            return;

        var frames = LoadFrames(hero.Id);
        if (frames == null)
            return;

        var go = new GameObject("SpecialCutIn");
        go.transform.SetParent(parent, false);
        go.AddComponent<SpecialCutIn>()._frames = frames;
    }

    void Update()
    {
        if (_frames == null)
            return;

        if (!_bound)
        {
            _combat = GetComponentInParent<PlayerCombat>();
            if (_combat == null)
                return;
            _bound = true;
            _wasCasting = _combat.IsCastingSpecial;
        }

        bool casting = _combat.IsCastingSpecial;
        if (casting && !_wasCasting && _overlay == null)
            StartOverlay();
        _wasCasting = casting;

        if (_overlay == null)
            return;

        _clock += Time.deltaTime * Fps;
        while (_clock >= 1f)
        {
            _clock -= 1f;
            _index++;
        }

        if (_index >= FrameCount)
        {
            Destroy(_overlay);
            _overlay = null;
            _image = null;
            return;
        }

        _image.sprite = _frames[_index];
    }

    void StartOverlay()
    {
        _clock = 0f;
        _index = 0;

        _overlay = new GameObject("CutInCanvas");
        var canvas = _overlay.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        // Acima do HUD/MobileCanvas (80), abaixo do FadeCanvas (1000).
        canvas.sortingOrder = 140;

        var scaler = _overlay.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        var imageGo = new GameObject("CutInFrame");
        imageGo.transform.SetParent(_overlay.transform, false);
        _image = imageGo.AddComponent<Image>();
        _image.raycastTarget = false;
        _image.sprite = _frames[0];

        // Frame 240x135 esticado na faixa central da tela (16:9 -> 1920x1080).
        var rect = _image.rectTransform;
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(1920f, 1080f);
        rect.anchoredPosition = Vector2.zero;
    }

    void OnDestroy()
    {
        if (_overlay != null)
            Destroy(_overlay);
    }

    static Sprite[] LoadFrames(string heroId)
    {
        var texture = Resources.Load<Texture2D>("CutIns/" + heroId);
        if (texture == null)
            return null;
        int gridRows = (FrameCount + GridCols - 1) / GridCols;
        if (texture.width < FrameW * GridCols || texture.height < FrameH * gridRows)
            return null;

        texture.filterMode = FilterMode.Point;
        texture.wrapMode = TextureWrapMode.Clamp;

        var frames = new Sprite[FrameCount];
        for (int i = 0; i < FrameCount; i++)
        {
            int col = i % GridCols;
            int rowFromTop = i / GridCols;
            float y = texture.height - (rowFromTop + 1) * FrameH;
            frames[i] = Sprite.Create(
                texture,
                new Rect(col * FrameW, y, FrameW, FrameH),
                new Vector2(0.5f, 0.5f),
                100f,
                0,
                SpriteMeshType.FullRect);
        }

        return frames;
    }
}
