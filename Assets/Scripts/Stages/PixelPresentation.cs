using UnityEngine;

/// <summary>
/// Framebuffer fixo 384×216 com escala inteira e filtro Point.
/// O follow da câmera permanece no mesmo GameObject.
/// </summary>
[DefaultExecutionOrder(-100)]
public class PixelPresentation : MonoBehaviour
{
    public const int InternalWidth = 384;
    public const int InternalHeight = 216;

    public static float OrthoSize => InternalHeight / (PixelArt.Ppu * 2f);

    Camera _world;
    RenderTexture _target;
    Rect _dest;
    GameObject _screenRoot;
    UnityEngine.UI.RawImage _view;
    RectTransform _viewRt;

    void OnEnable()
    {
        _world = GetComponent<Camera>();
        _target = new RenderTexture(InternalWidth, InternalHeight, 24, RenderTextureFormat.ARGB32)
        {
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp,
            useMipMap = false,
            autoGenerateMips = false
        };
        _target.Create();
        _world.targetTexture = _target;
        if (_screenRoot != null)
            _screenRoot.SetActive(true);
        _world.orthographic = true;
        _world.orthographicSize = OrthoSize;
        ApplyAspect();
        Fit();
        UpdateScreen();
    }

    void OnDisable()
    {
        if (_world != null && _world.targetTexture == _target)
            _world.targetTexture = null;
        if (_target != null)
        {
            _target.Release();
            Destroy(_target);
            _target = null;
        }
        if (_screenRoot != null)
            _screenRoot.SetActive(false);
    }

    void LateUpdate()
    {
        ApplyAspect();
        Fit();
        UpdateScreen();
    }

    void ApplyAspect()
    {
        if (_world == null)
            return;
        _world.aspect = (float)InternalWidth / InternalHeight;
    }

    void Fit()
    {
        int scale = Mathf.Max(1, Mathf.Min(Screen.width / InternalWidth, Screen.height / InternalHeight));
        float w = InternalWidth * scale;
        float h = InternalHeight * scale;
        float x = (Screen.width - w) * 0.5f;
        float y = (Screen.height - h) * 0.5f;
        _dest = new Rect(x, y, w, h);
    }

    // Mostra o framebuffer num Canvas overlay atrás do HUD. RawImage já respeita
    // a orientação da RenderTexture em todas as plataformas (inclusive WebGL),
    // e o HUD (outros Canvas overlay) continua desenhado por cima.
    void EnsureScreen()
    {
        if (_screenRoot != null)
            return;
        _screenRoot = new GameObject("PixelPresentationScreen");
        var canvas = _screenRoot.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = -1000;

        var bg = new GameObject("Fundo", typeof(RectTransform), typeof(UnityEngine.UI.Image));
        bg.transform.SetParent(_screenRoot.transform, false);
        var bgRt = (RectTransform)bg.transform;
        bgRt.anchorMin = Vector2.zero;
        bgRt.anchorMax = Vector2.one;
        bgRt.offsetMin = Vector2.zero;
        bgRt.offsetMax = Vector2.zero;
        var bgImg = bg.GetComponent<UnityEngine.UI.Image>();
        bgImg.color = Color.black;
        bgImg.raycastTarget = false;

        var view = new GameObject("Mundo", typeof(RectTransform), typeof(UnityEngine.UI.RawImage));
        view.transform.SetParent(_screenRoot.transform, false);
        _view = view.GetComponent<UnityEngine.UI.RawImage>();
        _view.raycastTarget = false;
        _viewRt = (RectTransform)view.transform;
        _viewRt.anchorMin = Vector2.zero;
        _viewRt.anchorMax = Vector2.zero;
        _viewRt.pivot = Vector2.zero;
    }

    void UpdateScreen()
    {
        EnsureScreen();
        _view.texture = _target;
        var scaler = _screenRoot.GetComponent<Canvas>().scaleFactor;
        if (scaler <= 0f) scaler = 1f;
        _viewRt.anchoredPosition = new Vector2(_dest.x, _dest.y) / scaler;
        _viewRt.sizeDelta = new Vector2(_dest.width, _dest.height) / scaler;
    }

    void OnDestroy()
    {
        if (_screenRoot != null)
            Destroy(_screenRoot);
    }
}
