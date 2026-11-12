using UnityEngine;

/// <summary>
/// RT 1536×864 Point. OrthoSize 6.75 mantém o enquadramento de 24×13.5 u.
/// Escala inteira em telas maiores; redução Point apenas quando a tela é menor
/// que o RT, para não cortar o mundo. A resolução não adiciona pixels à arte fonte.
/// </summary>
[DefaultExecutionOrder(-100)]
public class PixelPresentation : MonoBehaviour
{
    public const int InternalWidth = 1536;
    public const int InternalHeight = 864;

    /// <summary>
    /// Altura de design do framing original. Não substituir por <see cref="InternalHeight"/>.
    /// </summary>
    const int LegacyDesignHeight = 216;

    /// <summary>
    /// Metade da altura de mundo visível. 216 / (PixelArt.Ppu * 2) = 6.75 com PPU 16.
    /// </summary>
    public static float OrthoSize => LegacyDesignHeight / (PixelArt.Ppu * 2f);

    Camera _world;
    RenderTexture _target;
    Rect _dest;
    GameObject _screenRoot;
    UnityEngine.UI.RawImage _view;
    RectTransform _viewRt;
    bool _fillMobile;

    void OnEnable()
    {
        _world = GetComponent<Camera>();
        _fillMobile = MobileControls.ShouldShow();
        EnsureTarget();
        if (_screenRoot != null)
            _screenRoot.SetActive(true);
        _world.orthographic = true;
        _world.orthographicSize = OrthoSize;
        ApplyAspect();
        Fit();
        UpdateScreen();
    }

    void EnsureTarget()
    {
        float aspect = _fillMobile ? (float)Screen.width / Mathf.Max(1, Screen.height)
            : (float)InternalWidth / InternalHeight;
        // Keep the same vertical field of view; wider phones show more world.
        // Bound allocation on very wide screens, and reallocate only on resize.
        float density = Mathf.Min(1f, 4096f / (InternalHeight * aspect));
        int height = Mathf.Max(1, Mathf.RoundToInt(InternalHeight * density));
        int width = Mathf.Clamp(Mathf.RoundToInt(height * aspect), 1, 4096);
        if (_target != null && _target.width == width && _target.height == height)
            return;
        if (_target != null)
        {
            _world.targetTexture = null;
            _target.Release();
            Destroy(_target);
        }
        _target = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32)
        {
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp,
            useMipMap = false,
            autoGenerateMips = false,
            antiAliasing = 1
        };
        _target.Create();
        _world.targetTexture = _target;
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
        EnsureTarget();
        ApplyAspect();
        Fit();
        UpdateScreen();
    }

    void ApplyAspect()
    {
        if (_world == null)
            return;
        _world.aspect = _fillMobile ? (float)Screen.width / Mathf.Max(1, Screen.height)
            : (float)InternalWidth / InternalHeight;
    }

    void Fit()
    {
        _dest = _fillMobile ? new Rect(0, 0, Screen.width, Screen.height)
            : FitRect(Screen.width, Screen.height);
    }

    public static Rect FitRect(int width, int height)
    {
        float scale = Mathf.Min((float)width / InternalWidth, (float)height / InternalHeight);
        float w = Mathf.Floor(InternalWidth * scale);
        float h = Mathf.Floor(InternalHeight * scale);
        return new Rect(Mathf.Floor((width-w)*0.5f), Mathf.Floor((height-h)*0.5f), w, h);
    }

    // Mostra o framebuffer num Canvas overlay atrás do HUD. RawImage já respeita
    // a orientação da RenderTexture em todas as plataformas (inclusive WebGL),
    // e o HUD (outros Canvas overlay) continua desenhado por cima.
    void EnsureScreen()
    {
        if (_screenRoot != null)
            return;
        _screenRoot = new GameObject("PixelPresentationScreen");

        // Câmera de backbuffer vazia (cullingMask=0): evita o aviso 'No cameras rendering' no Editor da Unity
        var dummyCam = _screenRoot.AddComponent<Camera>();
        dummyCam.clearFlags = CameraClearFlags.SolidColor;
        dummyCam.backgroundColor = Color.black;
        dummyCam.cullingMask = 0;
        dummyCam.depth = -100;

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
