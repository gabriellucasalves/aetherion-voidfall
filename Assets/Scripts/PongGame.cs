using UnityEngine;

public class PongGame : MonoBehaviour
{
    public static PongGame Instance { get; private set; }

    public const int PointsToWin = 7;
    public const float CourtHalfWidth = 8.4f;
    public const float CourtHalfHeight = 4.7f;

    public bool IsPlaying { get; private set; }
    public Ball ActiveBall { get; private set; }
    public PongAudio Audio { get; private set; }

    Paddle _left;
    Paddle _right;
    int _leftScore;
    int _rightScore;
    int _nextServe = 1;
    bool _matchOver;
    bool _rightUsesAi = true;
    string _status = "ESPACO para sacar";

    void Awake()
    {
        Instance = this;
        Physics2D.gravity = Vector2.zero;
        Audio = gameObject.AddComponent<PongAudio>();
        BuildCourt();
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.A))
        {
            _rightUsesAi = !_rightUsesAi;
            if (_right != null)
                _right.UseAi = _rightUsesAi;
        }

        if (!Input.GetKeyDown(KeyCode.Space))
            return;

        if (_matchOver)
        {
            ResetMatch();
            return;
        }

        if (!IsPlaying)
            Serve();
    }

    public void OnGoal(int scoringPlayer)
    {
        if (!IsPlaying)
            return;

        IsPlaying = false;
        ActiveBall.Stop();
        Audio.PlayScore();

        if (scoringPlayer == 1)
            _leftScore++;
        else
            _rightScore++;

        _nextServe = scoringPlayer == 1 ? 2 : 1;

        if (_leftScore >= PointsToWin || _rightScore >= PointsToWin)
        {
            _matchOver = true;
            string winner = _leftScore > _rightScore ? "JOGADOR 1" : (_rightUsesAi ? "CPU" : "JOGADOR 2");
            _status = $"{winner} VENCEU  ·  ESPACO para reiniciar";
            return;
        }

        _status = "ESPACO para sacar";
    }

    void Serve()
    {
        IsPlaying = true;
        _status = string.Empty;
        ActiveBall.Serve(_nextServe);
    }

    void ResetMatch()
    {
        _leftScore = 0;
        _rightScore = 0;
        _matchOver = false;
        _nextServe = Random.value > 0.5f ? 1 : 2;
        _status = "ESPACO para sacar";
        ActiveBall.Stop();
        IsPlaying = false;
    }

    void BuildCourt()
    {
        ConfigureCamera();
        CreateBackdrop();
        CreateCenterLine();
        CreateWall("ParedeCima", new Vector2(0f, CourtHalfHeight + 0.15f), new Vector2(CourtHalfWidth * 2.2f, 0.3f));
        CreateWall("ParedeBaixo", new Vector2(0f, -CourtHalfHeight - 0.15f), new Vector2(CourtHalfWidth * 2.2f, 0.3f));
        CreateGoal("GolEsquerdo", new Vector2(-CourtHalfWidth - 0.6f, 0f), 2);
        CreateGoal("GolDireito", new Vector2(CourtHalfWidth + 0.6f, 0f), 1);

        _left = CreatePaddle("RaqueteEsquerda", Paddle.Side.Left, false, new Vector2(-CourtHalfWidth + 0.45f, 0f));
        _right = CreatePaddle("RaqueteDireita", Paddle.Side.Right, _rightUsesAi, new Vector2(CourtHalfWidth - 0.45f, 0f));
        ActiveBall = CreateBall();
    }

    static void ConfigureCamera()
    {
        var camera = Camera.main;
        if (camera == null)
        {
            var cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            camera = cameraObject.AddComponent<Camera>();
            cameraObject.AddComponent<AudioListener>();
        }

        camera.orthographic = true;
        camera.orthographicSize = 5f;
        camera.backgroundColor = new Color(0.04f, 0.05f, 0.08f);
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.transform.position = new Vector3(0f, 0f, -10f);
    }

    static void CreateBackdrop()
    {
        var go = new GameObject("Fundo");
        PongSprite.AddRenderer(go, new Color(0.06f, 0.07f, 0.1f), new Vector2(CourtHalfWidth * 2f, CourtHalfHeight * 2f));
        go.GetComponent<SpriteRenderer>().sortingOrder = -20;
    }

    static void CreateCenterLine()
    {
        var parent = new GameObject("LinhaCentral");
        const int dashes = 15;
        float start = -CourtHalfHeight + 0.25f;
        float step = (CourtHalfHeight * 2f - 0.5f) / (dashes - 1);

        for (int i = 0; i < dashes; i++)
        {
            var dash = new GameObject($"Traco_{i}");
            dash.transform.SetParent(parent.transform, false);
            dash.transform.position = new Vector3(0f, start + step * i, 0f);
            var renderer = PongSprite.AddRenderer(dash, new Color(1f, 1f, 1f, 0.18f), new Vector2(0.08f, 0.28f));
            renderer.sortingOrder = -10;
        }
    }

    static void CreateWall(string name, Vector2 position, Vector2 size)
    {
        var go = new GameObject(name);
        go.transform.position = position;
        PongSprite.AddRenderer(go, Color.white, size);

        var collider = go.AddComponent<BoxCollider2D>();
        collider.size = Vector2.one;
        go.AddComponent<WallMarker>();
    }

    static void CreateGoal(string name, Vector2 position, int scoringPlayer)
    {
        var go = new GameObject(name);
        go.transform.position = position;
        go.transform.localScale = new Vector3(0.8f, CourtHalfHeight * 2.4f, 1f);

        var collider = go.AddComponent<BoxCollider2D>();
        collider.isTrigger = true;
        collider.size = Vector2.one;

        var goal = go.AddComponent<GoalZone>();
        goal.ScoringPlayer = scoringPlayer;
    }

    static Paddle CreatePaddle(string name, Paddle.Side side, bool useAi, Vector2 position)
    {
        var go = new GameObject(name);
        go.transform.position = position;
        PongSprite.AddRenderer(go, Color.white, new Vector2(0.22f, 1.6f));

        var collider = go.AddComponent<BoxCollider2D>();
        collider.size = Vector2.one;

        var paddle = go.AddComponent<Paddle>();
        float moveLimit = CourtHalfHeight - 0.8f;
        paddle.Setup(side, useAi, moveLimit);
        return paddle;
    }

    static Ball CreateBall()
    {
        var go = new GameObject("Bola");
        PongSprite.AddRenderer(go, Color.white, new Vector2(0.22f, 0.22f));

        var collider = go.AddComponent<CircleCollider2D>();
        collider.radius = 0.5f;

        var trail = go.AddComponent<TrailRenderer>();
        trail.time = 0.18f;
        trail.startWidth = 0.16f;
        trail.endWidth = 0f;
        var shader = Shader.Find("Sprites/Default") ?? Shader.Find("Unlit/Color");
        if (shader != null)
            trail.material = new Material(shader);
        trail.startColor = new Color(1f, 1f, 1f, 0.45f);
        trail.endColor = new Color(1f, 1f, 1f, 0f);
        trail.sortingOrder = -1;

        return go.AddComponent<Ball>();
    }

    void OnGUI()
    {
        DrawLabel(new Rect(0f, 18f, Screen.width * 0.5f, 90f), _leftScore.ToString(), 72, TextAnchor.UpperCenter);
        DrawLabel(new Rect(Screen.width * 0.5f, 18f, Screen.width * 0.5f, 90f), _rightScore.ToString(), 72, TextAnchor.UpperCenter);

        string rightLabel = _rightUsesAi ? "CPU" : "J2";
        DrawLabel(new Rect(40f, Screen.height - 58f, 280f, 40f), "J1  W / S", 18, TextAnchor.LowerLeft);
        DrawLabel(new Rect(Screen.width - 320f, Screen.height - 58f, 280f, 40f), $"{rightLabel}  SETAS", 18, TextAnchor.LowerRight);
        DrawLabel(new Rect(0f, Screen.height - 92f, Screen.width, 30f), "A  troca humano/CPU no lado direito", 16, TextAnchor.UpperCenter);

        if (!string.IsNullOrEmpty(_status))
        {
            DrawLabel(new Rect(0f, Screen.height * 0.42f, Screen.width, 50f), "PING PONG", 42, TextAnchor.MiddleCenter);
            DrawLabel(new Rect(0f, Screen.height * 0.50f, Screen.width, 40f), _status, 22, TextAnchor.MiddleCenter);
        }
    }

    static void DrawLabel(Rect rect, string text, int size, TextAnchor anchor)
    {
        var style = new GUIStyle(GUI.skin.label)
        {
            fontSize = size,
            alignment = anchor,
            fontStyle = FontStyle.Bold
        };
        style.normal.textColor = Color.white;
        GUI.Label(rect, text, style);
    }
}
