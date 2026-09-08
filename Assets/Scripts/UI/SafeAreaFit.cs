using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.UI;

// Recua o conteúdo da barra do iOS / Safari sem mexer em âncoras do Canvas
// (alterar âncora no mesmo objeto do CanvasScaler estoura a pilha no WebGL mobile).
public class SafeAreaFit : MonoBehaviour
{
#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")]
    static extern float AetherionSafeInsetBottom();
#endif

    RectTransform _rect;
    Canvas _canvas;
    int _lastW;
    int _lastH;
    float _lastBottom = -1f;

    void Start()
    {
        _rect = GetComponent<RectTransform>();
        _canvas = GetComponent<Canvas>();
        Apply();
    }

    void LateUpdate()
    {
        if (Screen.width != _lastW || Screen.height != _lastH)
            Apply();
    }

    void Apply()
    {
        if (_rect == null)
            _rect = GetComponent<RectTransform>();
        if (_rect == null)
            return;

        _lastW = Screen.width;
        _lastH = Screen.height;

        float extraBottom = 0f;
#if UNITY_WEBGL && !UNITY_EDITOR
        try
        {
            extraBottom = AetherionSafeInsetBottom();
        }
        catch (System.Exception)
        {
            extraBottom = 0f;
        }
#endif
        if (MobileControls.ShouldShow() && extraBottom < Screen.height * 0.06f)
            extraBottom = Screen.height * 0.06f;

        float scale = _canvas != null && _canvas.scaleFactor > 0.01f ? _canvas.scaleFactor : 1f;
        float bottom = extraBottom / scale;
        if (Mathf.Abs(bottom - _lastBottom) < 0.5f)
            return;

        _lastBottom = bottom;
        _rect.anchorMin = Vector2.zero;
        _rect.anchorMax = Vector2.one;
        _rect.offsetMin = new Vector2(0f, bottom);
        _rect.offsetMax = Vector2.zero;
    }
}
