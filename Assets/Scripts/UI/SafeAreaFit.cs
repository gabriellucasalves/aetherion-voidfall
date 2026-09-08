using System.Runtime.InteropServices;
using UnityEngine;

// Recua o canvas da barra do iOS / recorte da tela (safe area + Safari).
public class SafeAreaFit : MonoBehaviour
{
#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")]
    static extern float AetherionSafeInsetBottom();
#endif

    RectTransform _rect;

    void Awake()
    {
        _rect = GetComponent<RectTransform>();
        Apply();
    }

    void OnRectTransformDimensionsChange()
    {
        Apply();
    }

    void Apply()
    {
        if (_rect == null)
            _rect = GetComponent<RectTransform>();
        if (_rect == null)
            return;

        var safe = Screen.safeArea;
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

        safe.y += extraBottom;
        safe.height = Mathf.Max(8f, safe.height - extraBottom);

        var min = safe.position;
        var max = min + safe.size;
        min.x /= Mathf.Max(1, Screen.width);
        min.y /= Mathf.Max(1, Screen.height);
        max.x /= Mathf.Max(1, Screen.width);
        max.y /= Mathf.Max(1, Screen.height);
        _rect.anchorMin = min;
        _rect.anchorMax = max;
        _rect.offsetMin = Vector2.zero;
        _rect.offsetMax = Vector2.zero;
    }
}
