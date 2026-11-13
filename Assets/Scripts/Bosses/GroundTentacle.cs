using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GroundTentacle : MonoBehaviour
{
    static Sprite[] _frames;
    static Texture2D _sheetTexture;

    public static void EnsureSprites()
    {
        if (_frames != null && _frames.Length == 6) return;
        _sheetTexture = Resources.Load<Texture2D>("Bosses/DevoradorDoVazio/v2/tentaculo_chao_6f");
        if (_sheetTexture == null) return;
        _sheetTexture.filterMode = FilterMode.Point;
        _sheetTexture.wrapMode = TextureWrapMode.Clamp;

        const int cols = 6;
        const int fw = 64;
        const int fh = 96;
        _frames = new Sprite[cols];

        for (int c = 0; c < cols; c++)
        {
            _frames[c] = Sprite.Create(_sheetTexture,
                new Rect(c * fw, 0, fw, fh),
                new Vector2(0.5f, 0f), 32f, 0, SpriteMeshType.FullRect);
        }
    }

    public static GroundTentacle Spawn(Transform parent, Vector3 groundPos, VoidDevourerBoss boss, float warningTime = 1.0f)
    {
        EnsureSprites();
        var go = new GameObject("GroundTentacle");
        if (parent != null) go.transform.SetParent(parent, false);
        go.transform.position = groundPos;

        var tentacle = go.AddComponent<GroundTentacle>();
        tentacle._boss = boss;
        tentacle._warningTime = warningTime;
        tentacle.Build();
        return tentacle;
    }

    enum State { Warning, Rising, Standing, Retracting, Dead }

    VoidDevourerBoss _boss;
    float _warningTime;
    State _state = State.Warning;
    float _stateTimer;

    SpriteRenderer _sr;
    BoxCollider2D _hurtbox;
    HealthSystem _health;
    EnemyController _enemyController;
    bool _hasHitPlayer;

    GameObject _warningMarker;
    GameObject _warningCrack;

    void Build()
    {
        _sr = gameObject.AddComponent<SpriteRenderer>();
        _sr.sortingOrder = 7;
        if (_frames != null && _frames.Length > 0)
            _sr.sprite = _frames[0];

        _health = gameObject.AddComponent<HealthSystem>();
        _health.Configure(BossData.TentacleMaxHealth * (_boss != null ? 1f + .2f * (_boss.Phase - 1) : 1f));
        _health.Died += OnDied;

        // Hurtbox for hero attacks
        var body = gameObject.AddComponent<Rigidbody2D>();
        body.bodyType = RigidbodyType2D.Kinematic;
        body.simulated = true;

        _hurtbox = gameObject.AddComponent<BoxCollider2D>();
        _hurtbox.isTrigger = true;
        _hurtbox.size = new Vector2(0.75f, 2.5f);
        _hurtbox.offset = new Vector2(0f, 1.25f);
        _hurtbox.enabled = false;

        _enemyController = gameObject.AddComponent<EnemyController>();
        _enemyController.ExternalBrain = true;
        _enemyController.DamageFilter = OnFilterDamage;

        var dummyData = ScriptableObject.CreateInstance<EnemyData>();
        dummyData.Id = "tentaculo_vazio";
        dummyData.DisplayName = "Tentáculo do Vazio";
        dummyData.MaxHealth = Mathf.RoundToInt(BossData.TentacleMaxHealth * (_boss != null ? 1f + .2f * (_boss.Phase - 1) : 1f));
        dummyData.Size = new Vector2(0.85f, 2.2f);
        dummyData.Color = new Color(0.65f, 0.25f, 0.85f);
        _enemyController.Setup(dummyData, null, null);

        _stateTimer = _warningTime;
        NazorVfx.Mark("tentaculo", false);

        _warningMarker = NazorVfx.Play("marcador_chao_vermelho_48x12_4f", transform.position, 0f, 1f, true, _warningTime, 17);
        _warningCrack = NazorVfx.Play("tentaculo_rachadura_aviso_48x8_4f", transform.position, 0f, 1f, true, _warningTime, 17);
    }

    float OnFilterDamage(float incoming)
    {
        VoidImpactFx.Spawn(transform.position + Vector3.up * 1.0f);
        if (_boss != null)
        {
            _boss.ForwardTentacleDamage(incoming * BossData.TentacleDamageTransfer);
        }
        return incoming;
    }

    void Update()
    {
        _stateTimer -= Time.deltaTime;

        switch (_state)
        {
            case State.Warning:
                UpdateWarning();
                break;
            case State.Rising:
                UpdateRising();
                break;
            case State.Standing:
                UpdateStanding();
                break;
            case State.Retracting:
                UpdateRetracting();
                break;
        }
    }

    void UpdateWarning()
    {
        if (_frames != null && _frames.Length > 0)
        {
            bool blinkOn = (Mathf.FloorToInt(Time.time * 16f) % 2) == 0;
            _sr.enabled = blinkOn;
            _sr.sprite = _frames[0];
        }

        if (_stateTimer <= 0f)
        {
            _sr.enabled = true;
            CleanupWarningVfx();

            NazorVfx.Play("tentaculo_terra_explosao_64x48_7f", transform.position, 0f, 1f, false, 0.5f, 18);
            NazorVfx.Mark("tentaculo", true);
            NazorVfx.Juice("tentaculo");
            GameAudio.Emit(GameAudio.Cue.Tentacle, transform.position, 0.85f);
            _state = State.Rising;
            _stateTimer = 0.25f;
            _hurtbox.enabled = true;
            CheckEruptionHit();
        }
    }

    void UpdateRising()
    {
        float p = 1f - Mathf.Clamp01(_stateTimer / 0.25f);
        if (_frames != null && _frames.Length >= 3)
        {
            int f = Mathf.Clamp(Mathf.FloorToInt(p * 3f), 0, 2);
            _sr.sprite = _frames[f];
        }

        if (_stateTimer <= 0f)
        {
            _state = State.Standing;
            _stateTimer = .95f;
        }
    }

    void UpdateStanding()
    {
        if (_frames != null && _frames.Length >= 6)
        {
            int sway = 3 + (Mathf.FloorToInt(Time.time * 6f) % 3);
            _sr.sprite = _frames[sway];
        }

        if (_stateTimer <= 0f)
        {
            _state = State.Retracting;
            _stateTimer = .4f;
            _hurtbox.enabled = false;
            NazorVfx.Play("tentaculo_terra_explosao_64x48_7f", transform.position, 0f, 0.75f, false, 0.4f, 18);
        }
    }

    void UpdateRetracting()
    {
        float p = 1f - Mathf.Clamp01(_stateTimer / .4f);
        if (_frames != null && _frames.Length >= 3)
        {
            int f = 2 - Mathf.Clamp(Mathf.FloorToInt(p * 3f), 0, 2);
            _sr.sprite = _frames[f];
        }

        if (_stateTimer <= 0f)
        {
            Destroy(gameObject);
        }
    }

    void CheckEruptionHit()
    {
        if (_hasHitPlayer) return;
        var center = transform.position + Vector3.up * 1.25f;
        foreach (var col in Physics2D.OverlapBoxAll(center, new Vector2(.75f, 2.5f), 0f))
        {
            var player = col.GetComponentInParent<PlayerController>();
            if (player != null)
            {
                _hasHitPlayer = true;
                NazorVfx.DamagePlayer(player.transform, transform.position, 18f * (_boss != null ? _boss.AttackDamageMultiplier : 1f), 40);

                var rb = player.GetComponent<Rigidbody2D>();
                if (rb != null)
                {
                    rb.linearVelocity = new Vector2(rb.linearVelocity.x, 6f);
                }
                PixelBurst.Spawn(player.transform.position + Vector3.up * 0.5f, new Color(0.75f, 0.28f, 0.95f), 7);
                break;
            }
        }
    }

    void CleanupWarningVfx()
    {
        if (_warningMarker != null) { Destroy(_warningMarker); _warningMarker = null; }
        if (_warningCrack != null) { Destroy(_warningCrack); _warningCrack = null; }
    }

    void OnDied(HealthSystem _)
    {
        _state = State.Dead;
        CleanupWarningVfx();
        VoidImpactFx.Spawn(transform.position + Vector3.up * 1.0f);
        Destroy(gameObject);
    }

    void OnDestroy()
    {
        CleanupWarningVfx();
    }
}
