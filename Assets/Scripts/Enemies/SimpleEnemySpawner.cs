using UnityEngine;

public class SimpleEnemySpawner : MonoBehaviour
{
    public const int MaxAlive = 12;
    public const float Interval = 1.25f;
    public const int DemoWaveCount = 3;
    public const int EnemiesPerWave = 7;
    public const int T3MaxAlive = 3;
    public const float T3Interval = 2.2f;

    Transform _player;
    Collider2D _playerCollider;
    StageData _stage;
    EnemyData _skeleton;
    EnemyData _beast;
    EnemyData _golem;
    EnemyData _sentinel;
    EnemyData _herald;
    EnemyData _knight;
    EnemyData _spitter;
    float _timer;
    bool _locked;
    bool _finished;
    int _wave;
    int _toSpawn;
    float _waveDelay;
    EnemyData _waveType;

    bool IsT2 => _stage != null && _stage.Id == "t2";
    bool IsT3 => _stage != null && _stage.Id == "t3";
    public int Wave => _wave;
    public int TotalWaves => IsT3 ? 2 : (IsT2 ? 2 : DemoWaveCount);
    public string WaveName => _waveType != null ? _waveType.DisplayName : "";
    public bool Finished => _finished;
    public int CurrentMaxAlive => IsT3 ? T3MaxAlive : MaxAlive;
    public float CurrentInterval => IsT3 ? T3Interval : Interval;

    public void Setup(Transform player, StageData stage)
    {
        _player = player;
        _playerCollider = player.GetComponent<Collider2D>();
        _stage = stage;
        _skeleton = EnemyData.CreateAlienSkeleton();
        _beast = EnemyData.CreateBeast();
        _golem = EnemyData.CreateSlateGolem();
        _sentinel = EnemyData.CreateRubbleSentinel();
        _herald = EnemyData.CreateEmberHerald();
        _knight = EnemyData.CreateCorruptedKnight();
        _spitter = EnemyData.CreateVoidSpitter();
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
                if (_wave >= TotalWaves)
                    FinishDemo();
                else
                    _waveDelay = IsT3 ? 2.0f : 2.4f;
            }
            return;
        }

        _timer += Time.deltaTime;
        if (_timer < (IsT3 ? T3Interval : Interval))
            return;

        _timer = 0f;
        if (CountAlive() >= (IsT3 ? T3MaxAlive : MaxAlive))
            return;

        EnemyData toSpawnData = IsT3 ? GetNextT3Enemy(_wave, 6 - _toSpawn) : _waveType;
        if (Spawn(toSpawnData))
            _toSpawn--;
    }

    public EnemyData GetNextT3Enemy(int wave, int spawnIndex)
    {
        // T3 usa APENAS CavaleiroCorrompido e CuspidorDoVazio.
        // Onda 1: 4 cavaleiros + 2 cuspidores = 6. Sequência: K, K, S, K, K, S.
        // Onda 2: 3 cavaleiros + 3 cuspidores = 6. Sequência: K, S, K, S, K, S.
        int idx = Mathf.Clamp(spawnIndex, 0, 5);
        if (wave == 1)
        {
            return (idx == 2 || idx == 5) ? _spitter : _knight;
        }
        else
        {
            return (idx % 2 == 1) ? _spitter : _knight;
        }
    }

    // Fase 1: 3 ondas (Esqueleto Alien → Bicho → Golem de Ardósia, 7 cada).
    // Fase 2: 2 ondas (Sentinela de Escombro → Arauto de Brasa, 6 cada).
    // Fase 3: 2 ondas determinísticas apenas com Cavaleiros e Cuspidores (Onda 1: 4+2; Onda 2: 3+3).
    void StartWave()
    {
        if (_wave >= TotalWaves)
        {
            FinishDemo();
            return;
        }

        _wave++;
        if (IsT3)
        {
            _waveType = _wave == 1 ? _knight : _spitter;
            _toSpawn = 6;
        }
        else if (IsT2)
        {
            _waveType = _wave == 1 ? _sentinel : _herald;
            _toSpawn = 6;
        }
        else
        {
            int cycle = (_wave - 1) % 3;
            if (cycle == 0)
                _waveType = _skeleton;
            else if (cycle == 1)
                _waveType = _beast;
            else
                _waveType = _golem;

            _toSpawn = EnemiesPerWave;
        }

        var stage = FindFirstObjectByType<GroundT1Controller>();
        if (stage != null)
        {
            string bannerName = IsT3
                ? (_wave == 1 ? "Cavaleiros Corrompidos" : "Cuspidores do Vazio")
                : _waveType.DisplayName;
            stage.AnnounceWave(_wave, bannerName);
        }
    }

    void FinishDemo()
    {
        if (_finished)
            return;

        _finished = true;
        _locked = true;
        var stage = FindFirstObjectByType<GroundT1Controller>();
        if (stage != null)
        {
            if (!IsT3)
                stage.OpenExitPortal();
        }
    }

    int CountAlive()
    {
        return FindObjectsByType<EnemyController>(FindObjectsSortMode.None).Length;
    }

    bool Spawn(EnemyData data)
    {
        // Nunca gera no ar: testa largura, espaço livre e apoio físico antes de consumir a vaga.
        // Em T3, inimigos do caminho só podem spawnar antes da arena (x <= 34f).
        float maxBoundary = IsT3 ? 34f : (_stage.HalfWidth - 1.2f);
        for (int attempt = 0; attempt < 24; attempt++)
        {
            float side = Random.value < 0.5f ? -1f : 1f;
            float x = Mathf.Clamp(_player.position.x + side * Random.Range(6f, 13f),
                -_stage.HalfWidth + 1.2f, maxBoundary);
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
        bool flying = data != null && data.AttackKind == "ember_flyer";
        go.transform.position = new Vector3(feet.x, feet.y + (flying ? 1.85f : 0f), 0f);

        var body = go.AddComponent<Rigidbody2D>();
        body.bodyType = RigidbodyType2D.Dynamic;
        body.gravityScale = flying ? 0f : 1f;
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
