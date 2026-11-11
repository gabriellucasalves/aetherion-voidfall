using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class BossIntroCutscene : MonoBehaviour
{
    public Text SpeechText => _falaText;
    public bool SkipToEnd { get; private set; }

    static Sprite[] _retratoFrames;
    static Sprite[] _setaFrames;
    static Sprite _molduraSprite;
    static Sprite _caixaSprite;
    static Sprite _placaSprite;
    static Texture2D _fileteTexture;

    string _fullSpeech;
    bool _isRetry;
    float _totalDuration;
    bool _closed;
    bool _didShake;
    bool _skipRequestedOnce;

    Image _veuMundo;
    Image _letterboxTopo;
    RawImage _fileteTopo;
    Image _letterboxBase;
    RawImage _fileteBase;
    Image _caixaDialogo;
    Image _placaNome;
    Text _falaText;
    Image _setaContinuar;
    Image _retratoBoss;
    Image _molduraRetrato;
    Text _dicaPular;

    CanvasGroup _dialogGroup;
    CanvasGroup _portraitGroup;
    CanvasGroup _hudGroup;

    public static BossIntroCutscene Create(string speechText, bool isRetry, float totalDuration)
    {
        EnsureAssets();

        var canvas = UiKit.CreateCanvas(null, "BossIntroCutscene");
        canvas.overrideSorting = true;
        canvas.sortingOrder = 450;

        var cutscene = canvas.gameObject.AddComponent<BossIntroCutscene>();
        cutscene.Setup(speechText, isRetry, totalDuration);
        return cutscene;
    }

    static void EnsureAssets()
    {
        if (_retratoFrames == null)
        {
            var tex = Resources.Load<Texture2D>("Bosses/DevoradorDoVazio/v2/cutscene/retrato_devorador_3f_96x96");
            if (tex != null)
            {
                tex.filterMode = FilterMode.Point;
                tex.wrapMode = TextureWrapMode.Clamp;
                _retratoFrames = new Sprite[3];
                for (int i = 0; i < 3; i++)
                {
                    _retratoFrames[i] = Sprite.Create(tex, new Rect(i * 96, 0, 96, 96), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
                    _retratoFrames[i].name = "retrato_devorador_3f_96x96_" + i;
                }
            }
        }

        if (_setaFrames == null)
        {
            var tex = Resources.Load<Texture2D>("Bosses/DevoradorDoVazio/v2/cutscene/seta_continuar_2f_8x6");
            if (tex != null)
            {
                tex.filterMode = FilterMode.Point;
                tex.wrapMode = TextureWrapMode.Clamp;
                _setaFrames = new Sprite[2];
                for (int i = 0; i < 2; i++)
                {
                    _setaFrames[i] = Sprite.Create(tex, new Rect(i * 8, 0, 8, 6), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
                    _setaFrames[i].name = "seta_continuar_2f_8x6_" + i;
                }
            }
        }

        if (_molduraSprite == null)
            _molduraSprite = LoadUiSprite("moldura_retrato_boss_104x104");
        if (_caixaSprite == null)
            _caixaSprite = LoadUiSprite("caixa_dialogo_boss_336x48");
        if (_placaSprite == null)
            _placaSprite = LoadUiSprite("placa_nome_devorador_117x13");
        if (_fileteTexture == null)
            _fileteTexture = Resources.Load<Texture2D>("Bosses/DevoradorDoVazio/v2/cutscene/letterbox_filete_16x4");
    }

    static Sprite LoadUiSprite(string name)
    {
        var tex = Resources.Load<Texture2D>("Bosses/DevoradorDoVazio/v2/cutscene/" + name);
        if (tex == null) return null;
        tex.filterMode = FilterMode.Point;
        tex.wrapMode = TextureWrapMode.Clamp;
        var s = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
        s.name = name;
        return s;
    }

    void Setup(string speechText, bool isRetry, float totalDuration)
    {
        _fullSpeech = speechText ?? "";
        _isRetry = isRetry;
        _totalDuration = totalDuration;

        // Travar herói
        var player = FindFirstObjectByType<PlayerController>();
        if (player != null) player.SetLocked(true);
        var combat = FindFirstObjectByType<PlayerCombat>();
        if (combat != null) combat.SetLocked(true);

        // Ocultar HUD
        var hudGo = GameObject.Find("HudHorizontalV1");
        if (hudGo != null)
        {
            _hudGroup = hudGo.GetComponent<CanvasGroup>();
            if (_hudGroup == null) _hudGroup = hudGo.AddComponent<CanvasGroup>();
            _hudGroup.alpha = 0f;
        }
        var titulo = GameObject.Find("TituloFase");
        if (titulo != null) titulo.SetActive(false);
        var rodape = GameObject.Find("ControlesRodape");
        if (rodape != null) rodape.SetActive(false);

        BuildLayout();
    }

    void BuildLayout()
    {
        // 1. VeuMundo (1920x1080)
        _veuMundo = CreateImage("VeuMundo", null, 0, 0, 1920, 1080, new Color(0f, 0f, 0f, 0f));

        // 2. LetterboxTopo (1920x96)
        _letterboxTopo = CreateImage("LetterboxTopo", null, 0, 0, 1920, 0, Color.black);

        // 3. FileteTopo (1920x16)
        _fileteTopo = CreateRawImage("FileteTopo", _fileteTexture, 0, -80, 1920, 16, new Rect(0, 1, 30, -1));

        // 4. LetterboxBase (1920x96)
        _letterboxBase = CreateImage("LetterboxBase", null, 0, -1080, 1920, 96, Color.black);

        // 5. FileteBase (1920x16)
        _fileteBase = CreateRawImage("FileteBase", _fileteTexture, 0, -984, 1920, 16, new Rect(0, 0, 30, 1));

        // 6. CaixaDialogo (1344x192)
        _caixaDialogo = CreateImage("CaixaDialogo", _caixaSprite, 64, -760, 1344, 192, Color.white);
        _dialogGroup = _caixaDialogo.gameObject.AddComponent<CanvasGroup>();

        // 7. PlacaNome (468x52)
        _placaNome = CreateImage("PlacaNome", _placaSprite, 908, -736, 468, 52, Color.white);
        var placaGroup = _placaNome.gameObject.AddComponent<CanvasGroup>();

        // 8. FalaBoss (1216x136)
        var falaGo = new GameObject("FalaBoss", typeof(RectTransform), typeof(Text), typeof(Outline), typeof(CanvasGroup));
        falaGo.transform.SetParent(transform, false);
        var frt = (RectTransform)falaGo.transform;
        frt.anchorMin = frt.anchorMax = new Vector2(0, 1);
        frt.pivot = new Vector2(0, 1);
        frt.anchoredPosition = new Vector2(128, -812);
        frt.sizeDelta = new Vector2(1216, 136);

        _falaText = falaGo.GetComponent<Text>();
        _falaText.font = UiKit.ResolveFont();
        _falaText.fontSize = 34;
        _falaText.color = new Color(246f / 255f, 238f / 255f, 222f / 255f);
        _falaText.alignment = TextAnchor.UpperLeft;
        _falaText.horizontalOverflow = HorizontalWrapMode.Wrap;
        _falaText.verticalOverflow = VerticalWrapMode.Truncate;
        _falaText.lineSpacing = 1.1f;
        _falaText.raycastTarget = false;
        _falaText.text = "";

        var outline = falaGo.GetComponent<Outline>();
        outline.effectColor = Color.black;
        outline.effectDistance = new Vector2(2f, -2f);

        // 9. SetaContinuar (32x24)
        _setaContinuar = CreateImage("SetaContinuar", _setaFrames != null && _setaFrames.Length > 0 ? _setaFrames[0] : null, 1352, -916, 32, 24, Color.white);
        _setaContinuar.gameObject.SetActive(false);

        // 10. RetratoBoss (384x384)
        _retratoBoss = CreateImage("RetratoBoss", _retratoFrames != null && _retratoFrames.Length > 0 ? _retratoFrames[0] : null, 1920, -560, 384, 384, Color.white);
        _portraitGroup = _retratoBoss.gameObject.AddComponent<CanvasGroup>();

        // 11. MolduraRetrato (416x416)
        _molduraRetrato = CreateImage("MolduraRetrato", _molduraSprite, 1920, -544, 416, 416, Color.white);
        var molduraGroup = _molduraRetrato.gameObject.AddComponent<CanvasGroup>();

        // 12. DicaPular (480x40)
        var dicaGo = new GameObject("DicaPular", typeof(RectTransform), typeof(Text));
        dicaGo.transform.SetParent(transform, false);
        var drt = (RectTransform)dicaGo.transform;
        drt.anchorMin = drt.anchorMax = new Vector2(0, 1);
        drt.pivot = new Vector2(0, 1);
        drt.anchoredPosition = new Vector2(1400, -1010);
        drt.sizeDelta = new Vector2(480, 40);

        _dicaPular = dicaGo.GetComponent<Text>();
        _dicaPular.font = UiKit.ResolveFont();
        _dicaPular.fontSize = 22;
        _dicaPular.color = new Color(170f / 255f, 160f / 255f, 185f / 255f);
        _dicaPular.alignment = TextAnchor.MiddleRight;
        _dicaPular.text = "ESPAÇO / toque: pular";
        _dicaPular.raycastTarget = false;
    }

    Image CreateImage(string name, Sprite sprite, float x, float y, float w, float h, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(transform, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = rt.anchorMax = new Vector2(0, 1);
        rt.pivot = new Vector2(0, 1);
        rt.anchoredPosition = new Vector2(x, y);
        rt.sizeDelta = new Vector2(w, h);

        var img = go.GetComponent<Image>();
        img.sprite = sprite;
        img.color = color;
        img.raycastTarget = false;
        return img;
    }

    RawImage CreateRawImage(string name, Texture2D tex, float x, float y, float w, float h, Rect uv)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(RawImage));
        go.transform.SetParent(transform, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = rt.anchorMax = new Vector2(0, 1);
        rt.pivot = new Vector2(0, 1);
        rt.anchoredPosition = new Vector2(x, y);
        rt.sizeDelta = new Vector2(w, h);

        var raw = go.GetComponent<RawImage>();
        raw.texture = tex;
        raw.uvRect = uv;
        raw.raycastTarget = false;
        return raw;
    }

    void Update()
    {
        // Pular se pressionar Espaço, Enter, E, J ou toque/clique
        if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return) ||
            Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.J) ||
            Input.GetMouseButtonDown(0))
        {
            RequestSkip();
        }
    }

    public void RequestSkip()
    {
        if (_falaText != null && _falaText.text != _fullSpeech)
        {
            _falaText.text = _fullSpeech;
            _skipRequestedOnce = true;
            if (_setaContinuar != null) _setaContinuar.gameObject.SetActive(true);
            if (_retratoFrames != null && _retratoFrames.Length > 2 && _retratoBoss != null)
                _retratoBoss.sprite = _retratoFrames[2];
        }
        else
        {
            SkipToEnd = true;
        }
    }

    public void Tick(float elapsed)
    {
        if (_closed) return;

        if (_isRetry)
        {
            TickRetry(elapsed);
        }
        else
        {
            TickFull(elapsed);
        }
    }

    void TickFull(float elapsed)
    {
        // 0.00 - 0.25: letterbox entra (altura 0 -> 96), veu mundo vai a 0.27
        if (elapsed <= 0.25f)
        {
            float u = Mathf.Clamp01(elapsed / 0.25f);
            if (_letterboxTopo != null) _letterboxTopo.rectTransform.sizeDelta = new Vector2(1920f, Mathf.Lerp(0f, 96f, u));
            if (_letterboxBase != null) _letterboxBase.rectTransform.anchoredPosition = new Vector2(0f, Mathf.Lerp(-1080f, -984f, u));
            if (_veuMundo != null) _veuMundo.color = new Color(0f, 0f, 0f, Mathf.Lerp(0f, 0.27f, u));
            if (_hudGroup != null) _hudGroup.alpha = Mathf.Lerp(1f, 0f, u);

            if (_dialogGroup != null) _dialogGroup.alpha = 0f;
            if (_portraitGroup != null) _portraitGroup.alpha = 0f;
        }
        else
        {
            if (_letterboxTopo != null) _letterboxTopo.rectTransform.sizeDelta = new Vector2(1920f, 96f);
            if (_letterboxBase != null) _letterboxBase.rectTransform.anchoredPosition = new Vector2(0f, -984f);
            if (_veuMundo != null) _veuMundo.color = new Color(0f, 0f, 0f, 0.27f);
            if (_hudGroup != null) _hudGroup.alpha = 0f;
        }

        // 0.25 - 0.55: retrato desliza da direita (x 1920 -> 1456), caixa sobe de y-60 com fade
        if (elapsed > 0.25f && elapsed <= 0.55f)
        {
            float u = Mathf.Clamp01((elapsed - 0.25f) / 0.30f);
            float ease = 1f - Mathf.Pow(1f - u, 3f);
            if (_retratoBoss != null) _retratoBoss.rectTransform.anchoredPosition = new Vector2(Mathf.Lerp(1920f, 1456f, ease), -560f);
            if (_molduraRetrato != null) _molduraRetrato.rectTransform.anchoredPosition = new Vector2(Mathf.Lerp(1904f, 1440f, ease), -544f);
            if (_caixaDialogo != null) _caixaDialogo.rectTransform.anchoredPosition = new Vector2(64f, Mathf.Lerp(-820f, -760f, ease));
            if (_placaNome != null) _placaNome.rectTransform.anchoredPosition = new Vector2(908f, Mathf.Lerp(-796f, -736f, ease));
            if (_falaText != null) _falaText.rectTransform.anchoredPosition = new Vector2(128f, Mathf.Lerp(-872f, -812f, ease));
            SetAlpha(_dialogGroup, ease);
            SetAlpha(_portraitGroup, ease);
            SetAlpha(_placaNome?.GetComponent<CanvasGroup>(), ease);
            SetAlpha(_molduraRetrato?.GetComponent<CanvasGroup>(), ease);
            SetAlpha(_falaText?.GetComponent<CanvasGroup>(), ease);
        }
        else if (elapsed > 0.55f && elapsed <= 5.20f)
        {
            if (_retratoBoss != null) _retratoBoss.rectTransform.anchoredPosition = new Vector2(1456f, -560f);
            if (_molduraRetrato != null) _molduraRetrato.rectTransform.anchoredPosition = new Vector2(1440f, -544f);
            if (_caixaDialogo != null) _caixaDialogo.rectTransform.anchoredPosition = new Vector2(64f, -760f);
            if (_placaNome != null) _placaNome.rectTransform.anchoredPosition = new Vector2(908f, -736f);
            if (_falaText != null) _falaText.rectTransform.anchoredPosition = new Vector2(128f, -812f);
            SetAlpha(_dialogGroup, 1f);
            SetAlpha(_portraitGroup, 1f);
            SetAlpha(_placaNome?.GetComponent<CanvasGroup>(), 1f);
            SetAlpha(_molduraRetrato?.GetComponent<CanvasGroup>(), 1f);
            SetAlpha(_falaText?.GetComponent<CanvasGroup>(), 1f);
        }

        // 0.55: Máquina de escrever a 30 chars/s
        if (elapsed >= 0.55f && !_skipRequestedOnce)
        {
            float textTime = elapsed - 0.55f;
            int count = Mathf.Clamp(Mathf.FloorToInt(textTime * 30f), 0, _fullSpeech.Length);
            string cur = _fullSpeech.Substring(0, count);
            if (_falaText != null) _falaText.text = cur;

            // Retrato e animações
            if (cur.Contains("Eu sou o Devorador do Vazio."))
            {
                if (!_didShake)
                {
                    _didShake = true;
                    CameraFollow.Shake(0.25f, 0.12f);
                }
                if (_retratoFrames != null && _retratoFrames.Length > 2 && _retratoBoss != null)
                    _retratoBoss.sprite = _retratoFrames[2];
            }
            else
            {
                int frame = (int)(elapsed / 0.12f) % 2;
                if (_retratoFrames != null && _retratoFrames.Length > frame && _retratoBoss != null)
                    _retratoBoss.sprite = _retratoFrames[frame];
            }

            if (count >= _fullSpeech.Length)
            {
                if (_setaContinuar != null)
                {
                    _setaContinuar.gameObject.SetActive(true);
                    if (_setaFrames != null && _setaFrames.Length > 1)
                        _setaContinuar.sprite = _setaFrames[(int)(elapsed * 4f) % 2];
                }
            }
        }

        // 5.20 - 5.60: caixa, placa e retrato saem com fade
        if (elapsed > 5.20f && elapsed <= 5.60f)
        {
            float u = Mathf.Clamp01((elapsed - 5.20f) / 0.40f);
            float fade = 1f - u;
            SetAlpha(_dialogGroup, fade);
            SetAlpha(_portraitGroup, fade);
            SetAlpha(_placaNome?.GetComponent<CanvasGroup>(), fade);
            SetAlpha(_molduraRetrato?.GetComponent<CanvasGroup>(), fade);
            SetAlpha(_falaText?.GetComponent<CanvasGroup>(), fade);
        }

        // 5.60 - 6.00: letterbox recolhe e HUD volta
        if (elapsed > 5.60f)
        {
            float u = Mathf.Clamp01((elapsed - 5.60f) / 0.40f);
            if (_letterboxTopo != null) _letterboxTopo.rectTransform.sizeDelta = new Vector2(1920f, Mathf.Lerp(96f, 0f, u));
            if (_letterboxBase != null) _letterboxBase.rectTransform.anchoredPosition = new Vector2(0f, Mathf.Lerp(-984f, -1080f, u));
            if (_veuMundo != null) _veuMundo.color = new Color(0f, 0f, 0f, Mathf.Lerp(0.27f, 0f, u));
            if (_hudGroup != null) _hudGroup.alpha = Mathf.Lerp(0f, 1f, u);
        }
    }

    static void SetAlpha(CanvasGroup cg, float a)
    {
        if (cg != null) cg.alpha = a;
    }

    void TickRetry(float elapsed)
    {
        // Retry (2.0s): letterbox em 0.15s, retrato e caixa entram juntos em 0.15s
        if (elapsed <= 0.15f)
        {
            float u = Mathf.Clamp01(elapsed / 0.15f);
            if (_letterboxTopo != null) _letterboxTopo.rectTransform.sizeDelta = new Vector2(1920f, Mathf.Lerp(0f, 96f, u));
            if (_letterboxBase != null) _letterboxBase.rectTransform.anchoredPosition = new Vector2(0f, Mathf.Lerp(-1080f, -984f, u));
            if (_veuMundo != null) _veuMundo.color = new Color(0f, 0f, 0f, Mathf.Lerp(0f, 0.27f, u));
            if (_hudGroup != null) _hudGroup.alpha = Mathf.Lerp(1f, 0f, u);

            if (_retratoBoss != null) _retratoBoss.rectTransform.anchoredPosition = new Vector2(Mathf.Lerp(1920f, 1456f, u), -560f);
            if (_molduraRetrato != null) _molduraRetrato.rectTransform.anchoredPosition = new Vector2(Mathf.Lerp(1904f, 1440f, u), -544f);
            if (_caixaDialogo != null) _caixaDialogo.rectTransform.anchoredPosition = new Vector2(64f, Mathf.Lerp(-820f, -760f, u));
            if (_placaNome != null) _placaNome.rectTransform.anchoredPosition = new Vector2(908f, Mathf.Lerp(-796f, -736f, u));
            if (_falaText != null) _falaText.rectTransform.anchoredPosition = new Vector2(128f, Mathf.Lerp(-872f, -812f, u));
            SetAlpha(_dialogGroup, u);
            SetAlpha(_portraitGroup, u);
            SetAlpha(_placaNome?.GetComponent<CanvasGroup>(), u);
            SetAlpha(_molduraRetrato?.GetComponent<CanvasGroup>(), u);
            SetAlpha(_falaText?.GetComponent<CanvasGroup>(), u);
        }
        else if (elapsed <= 1.70f)
        {
            if (_letterboxTopo != null) _letterboxTopo.rectTransform.sizeDelta = new Vector2(1920f, 96f);
            if (_letterboxBase != null) _letterboxBase.rectTransform.anchoredPosition = new Vector2(0f, -984f);
            if (_veuMundo != null) _veuMundo.color = new Color(0f, 0f, 0f, 0.27f);
            if (_hudGroup != null) _hudGroup.alpha = 0f;

            if (_retratoBoss != null) _retratoBoss.rectTransform.anchoredPosition = new Vector2(1456f, -560f);
            if (_molduraRetrato != null) _molduraRetrato.rectTransform.anchoredPosition = new Vector2(1440f, -544f);
            if (_caixaDialogo != null) _caixaDialogo.rectTransform.anchoredPosition = new Vector2(64f, -760f);
            if (_placaNome != null) _placaNome.rectTransform.anchoredPosition = new Vector2(908f, -736f);
            if (_falaText != null) _falaText.rectTransform.anchoredPosition = new Vector2(128f, -812f);
            SetAlpha(_dialogGroup, 1f);
            SetAlpha(_portraitGroup, 1f);
            SetAlpha(_placaNome?.GetComponent<CanvasGroup>(), 1f);
            SetAlpha(_molduraRetrato?.GetComponent<CanvasGroup>(), 1f);
            SetAlpha(_falaText?.GetComponent<CanvasGroup>(), 1f);

            if (!_skipRequestedOnce)
            {
                float textTime = elapsed - 0.15f;
                int count = Mathf.Clamp(Mathf.FloorToInt(textTime * 40f), 0, _fullSpeech.Length);
                string cur = _fullSpeech.Substring(0, count);
                if (_falaText != null) _falaText.text = cur;

                if (_retratoFrames != null && _retratoFrames.Length > 2 && _retratoBoss != null)
                    _retratoBoss.sprite = _retratoFrames[2];

                if (count >= _fullSpeech.Length)
                {
                    if (_setaContinuar != null)
                    {
                        _setaContinuar.gameObject.SetActive(true);
                        if (_setaFrames != null && _setaFrames.Length > 1)
                            _setaContinuar.sprite = _setaFrames[(int)(elapsed * 4f) % 2];
                    }
                }
            }
        }
        else
        {
            // 1.70 - 2.00: saída
            float u = Mathf.Clamp01((elapsed - 1.70f) / 0.30f);
            float fade = 1f - u;
            SetAlpha(_dialogGroup, fade);
            SetAlpha(_portraitGroup, fade);
            SetAlpha(_placaNome?.GetComponent<CanvasGroup>(), fade);
            SetAlpha(_molduraRetrato?.GetComponent<CanvasGroup>(), fade);
            SetAlpha(_falaText?.GetComponent<CanvasGroup>(), fade);
            if (_letterboxTopo != null) _letterboxTopo.rectTransform.sizeDelta = new Vector2(1920f, Mathf.Lerp(96f, 0f, u));
            if (_letterboxBase != null) _letterboxBase.rectTransform.anchoredPosition = new Vector2(0f, Mathf.Lerp(-984f, -1080f, u));
            if (_veuMundo != null) _veuMundo.color = new Color(0f, 0f, 0f, Mathf.Lerp(0.27f, 0f, u));
            if (_hudGroup != null) _hudGroup.alpha = Mathf.Lerp(0f, 1f, u);
        }
    }

    public void Close()
    {
        if (_closed) return;
        _closed = true;

        // Destravar herói
        var player = FindFirstObjectByType<PlayerController>();
        if (player != null) player.SetLocked(false);
        var combat = FindFirstObjectByType<PlayerCombat>();
        if (combat != null) combat.SetLocked(false);

        // Restaurar HUD
        if (_hudGroup != null) _hudGroup.alpha = 1f;
        var titulo = GameObject.Find("TituloFase");
        if (titulo != null) titulo.SetActive(true);
        var rodape = GameObject.Find("ControlesRodape");
        if (rodape != null) rodape.SetActive(true);

        Destroy(gameObject);
    }

    void OnDestroy()
    {
        Close();
    }
}
