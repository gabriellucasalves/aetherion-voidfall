using UnityEngine;
using UnityEngine.UI;

// In WebGL, the HTML template owns the viewport and safe-area insets.
// Never modify the CanvasScaler root there: it causes double padding and
// recursive canvas rebuilds. Keep the existing standalone/editor behavior.
public class SafeAreaFit : MonoBehaviour
{
#if !UNITY_WEBGL || UNITY_EDITOR
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
        if (_rect == null) return;
        _lastW = Screen.width;
        _lastH = Screen.height;
        float extraBottom = MobileControls.ShouldShow() ? Screen.height * 0.06f : 0f;
        float scale = _canvas != null && _canvas.scaleFactor > 0.01f ? _canvas.scaleFactor : 1f;
        float bottom = extraBottom / scale;
        if (Mathf.Abs(bottom - _lastBottom) < 0.5f) return;
        _lastBottom = bottom;
        _rect.anchorMin = Vector2.zero;
        _rect.anchorMax = Vector2.one;
        _rect.offsetMin = new Vector2(0f, bottom);
        _rect.offsetMax = Vector2.zero;
    }
#endif
}
