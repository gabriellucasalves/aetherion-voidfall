using UnityEngine;

public class SimpleEnemySpawner : MonoBehaviour
{
    public const int MaxAlive = 12;
    public const float Interval = 1.25f;
    public const int DemoWaveCount = 3;
    public const int EnemiesPerWave = 7;

    Transform _player;
    Collider2D _playerCollider;
    StageData _stage;
    EnemyData _skeleton;
    EnemyData _beast;
    EnemyData _golem;
    float _timer;
    bool _locked;
    bool _finished;
    int _wave;
    int _toSpawn;
    float _waveDelay;
    EnemyData _waveType;

    public int Wave => _wave;
    public int TotalWaves => DemoWaveCount;
    public string WaveName => _waveType != null ? _waveType.DisplayName : "";
    public bool Finished => _finished;

    public void Setup(Transform player, StageData stage)
    {
        _player = player;
        _playerCollider = player.GetComponent<Collider2D>();
        _stage = stage;
        _skeleton = EnemyData.CreateAlienSkeleton();
        _beast = EnemyData.CreateBeast();
        _golem = EnemyData.CreateSlateGolem();
        _waveDelay = 1.4f; // respiro antes da primeira onda
    }

    public void SetLocked(bool locked)
    {
        _locked = locked;
    }

    void Update()
    {
        if (_locked || _finished || _player == null)
            return;

        // intervalo entre ondas
        if (_waveDelay > 0f)
        {
            _waveDelay -= Time.deltaTime;
            if (_waveDelay <= 0f)
                StartWave();
            return;
        }

        // onda esgotada: limpa o campo e avança (ou encerra a demo)
        if (_toSpawn <= 0)
        {
            if (CountAlive() == 0)
            {
                if (_wave >= DemoWaveCount)
                    FinishDemo();
                else
                    _waveDelay = 2.4f;
            }
            return;
        }

        _timer += Time.deltaTime;
        if (_timer < Interval)
            return;

        _timer = 0f;
        if (CountAlive() >= MaxAlive)
            return;

        if (Spawn(_waveType))
            _toSpawn--;
    }

    // Três ondas: Esqueleto Alien → Bicho → Golem de Ardósia (7 cada).
    void StartWave()
    {
        if (_wave >= DemoWaveCount)
        {
            FinishDemo();
            return;
        }

        _wave++;
        int cycle = (_wave - 1) % 3;
        if (cycle == 0)
            _waveType = _skeleton;
        else if (cycle == 1)
            _waveType = _beast;
        else
            _waveType = _golem;

        _toSpawn = EnemiesPerWave;

        var stage = FindFirstObjectByType<GroundT1Controller>();
        if (stage != null)
            stage.AnnounceWave(_wave, _waveType.DisplayName);
    }

    void FinishDemo()
    {
        if (_finished)
            return;

        _finished = true;
        _locked = true;
        var stage = FindFirstObjectByType<GroundT1Controller>();
        if (stage != null)
            stage.OpenExitPortal();
    }

    int CountAlive()
    {
        return FindObjectsByType<EnemyController>(FindObjectsSortMode.None).Length;
    }

    bool Spawn(EnemyData data)
    {
        // Nunca gera no ar: testa largura, espaço livre e apoio físico antes de consumir a vaga.
        for (int attempt = 0; attempt < 24; attempt++)
        {
            float side = Random.value < 0.5f ? -1f : 1f;
            float x = Mathf.Clamp(_player.position.x + side * Random.Range(6f, 13f),
                -_stage.HalfWidth + 1.2f, _stage.HalfWidth - 1.2f);
            if (!EnemyTraversal.TrySpawnPoint(x, _stage, data.Size, out var feet)) continue;
            SpawnAt(data, _player, _stage, feet, _playerCollider);
            return true;
        }
        return false;
    }

    public static EnemyController SpawnAt(EnemyData data, Transform player, StageData stage,
        Vector2 feet, Collider2D playerCollider = null)
    {
        var go = new GameObject(data.DisplayName);
        go.transform.position = new Vector3(feet.x, feet.y, 0f);

        var body = go.AddComponent<Rigidbody2D>();
        body.bodyType = RigidbodyType2D.Dynamic;
        body.gravityScale = 1f;
        body.freezeRotation = true;
        body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        body.constraints = RigidbodyConstraints2D.FreezeRotation;

        // colisores sobem a partir dos pés
        var solid = go.AddComponent<BoxCollider2D>();
        solid.isTrigger = false;
        solid.size = data.Size;
        solid.offset = new Vector2(0f, data.Size.y * 0.5f);

        var touch = go.AddComponent<BoxCollider2D>();
        touch.isTrigger = true;
        touch.size = data.Size + new Vector2(0.2f, 0.2f);
        touch.offset = solid.offset;

        if (playerCollider != null)
            Physics2D.IgnoreCollision(solid, playerCollider, true);

        var controller = go.AddComponent<EnemyController>();
        controller.Setup(data, player, stage);
        foreach (var other in FindObjectsByType<EnemyController>(FindObjectsSortMode.None))
            if (other != controller)
                foreach (var collider in other.GetComponents<Collider2D>())
                    if (!collider.isTrigger) Physics2D.IgnoreCollision(solid, collider, true);

        // pixel art; se a sheet faltar, cai no quadrado antigo
        if (!InimigoVisual.Attach(go, data.Id))
        {
            var renderer = go.AddComponent<SpriteRenderer>();
            var texture = Texture2D.whiteTexture;
            renderer.sprite = Sprite.Create(
                texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0f), texture.width);
            renderer.color = data.Color;
            renderer.sortingOrder = 8;
        }
        return controller;
    }
}
