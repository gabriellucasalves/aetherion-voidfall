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
        _world.orthographic = true;
        _world.orthographicSize = OrthoSize;
        ApplyAspect();
        Fit();
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
    }

    void LateUpdate()
    {
        ApplyAspect();
        Fit();
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

    void OnGUI()
    {
        if (_target == null || Event.current.type != EventType.Repaint)
            return;

        var prev = GUI.color;
        GUI.color = Color.black;
        if (_dest.x > 0.5f)
        {
            GUI.DrawTexture(new Rect(0f, 0f, _dest.x, Screen.height), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(_dest.xMax, 0f, Screen.width - _dest.xMax, Screen.height), Texture2D.whiteTexture);
        }
        if (_dest.y > 0.5f)
        {
            GUI.DrawTexture(new Rect(0f, 0f, Screen.width, _dest.y), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(0f, _dest.yMax, Screen.width, Screen.height - _dest.yMax), Texture2D.whiteTexture);
        }
        GUI.color = Color.white;
        // RenderTexture nasce de baixo para cima; altura negativa desvira no GUI.
        var flipped = new Rect(_dest.x, _dest.yMax, _dest.width, -_dest.height);
        GUI.DrawTexture(flipped, _target, ScaleMode.StretchToFill, false);
        GUI.color = prev;
    }
}
