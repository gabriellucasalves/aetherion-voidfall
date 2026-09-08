using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SceneTransitionManager : MonoBehaviour
{
    public static SceneTransitionManager Instance { get; private set; }

    [SerializeField] float _fadeDuration = 0.35f;

    CanvasGroup _fadeGroup;
    bool _busy;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;
        BuildFadeOverlay();
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public void Load(string sceneName)
    {
        if (_busy)
            return;

        StartCoroutine(LoadRoutine(sceneName));
    }

    IEnumerator LoadRoutine(string sceneName)
    {
        _busy = true;
        SceneManager.LoadScene(sceneName);
        yield return null;
        ClearFade();
        AttachFor(sceneName);
        _busy = false;
    }

    static void AttachFor(string sceneName)
    {
        if (sceneName == GameScenes.MainMenu)
            MainMenuController.EnsureExists();
        else if (sceneName == GameScenes.Intro)
            IntroController.EnsureExists();
        else if (sceneName == GameScenes.CharacterSelection)
            CharacterSelectController.EnsureExists();
        else if (sceneName == GameScenes.GroundT1)
            GroundT1Controller.EnsureExists();
    }

    public void ClearFade()
    {
        if (_fadeGroup == null)
            return;

        _fadeGroup.alpha = 0f;
        _fadeGroup.blocksRaycasts = false;
    }

    IEnumerator Fade(float target)
    {
        if (_fadeGroup == null)
            yield break;

        _fadeGroup.blocksRaycasts = true;
        float start = _fadeGroup.alpha;
        float time = 0f;

        while (time < _fadeDuration)
        {
            time += Time.unscaledDeltaTime;
            _fadeGroup.alpha = Mathf.Lerp(start, target, time / _fadeDuration);
            yield return null;
        }

        _fadeGroup.alpha = target;
        _fadeGroup.blocksRaycasts = target > 0.01f;
    }

    void BuildFadeOverlay()
    {
        var canvasObject = new GameObject("FadeCanvas");
        canvasObject.transform.SetParent(transform, false);

        var canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1000;

        var scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);

        canvasObject.AddComponent<GraphicRaycaster>();

        var fadeObject = new GameObject("Fade");
        fadeObject.transform.SetParent(canvasObject.transform, false);

        var image = fadeObject.AddComponent<Image>();
        image.color = Color.black;

        var rect = fadeObject.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        _fadeGroup = fadeObject.AddComponent<CanvasGroup>();
        _fadeGroup.alpha = 0f;
        _fadeGroup.blocksRaycasts = false;
    }
}
