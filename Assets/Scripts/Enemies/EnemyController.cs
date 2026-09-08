using UnityEngine;

// IA dos inimigos T1. Todos perseguem o jogador; cada classe ataca de um jeito:
//   esqueleto -> "axe": arremessa machado giratório reto de média distância;
//   ghoul     -> "acid": cospe bola ácida em arco;
//   zumbi     -> "explode": chega perto, pisca vermelho 0.5s e explode em área.
// Cada um também tem resistência própria (EnemyData.DamageTaken).
public class EnemyController : MonoBehaviour
{
    enum Phase
    {
        Chase,
        Windup,
        Recover
    }

    EnemyData _data;
    Transform _target;
    StageData _stage;
    Rigidbody2D _body;
    HealthSystem _health;
    float _touchReady;
    Phase _phase = Phase.Chase;
    float _phaseLeft;
    float _faceDir = 1f;

    public HealthSystem Health => _health;
    public EnemyData Data => _data;
    public bool IsWindingUp => _phase == Phase.Windup;

    public void Setup(EnemyData data, Transform target, StageData stage)
    {
        _data = data;
        _target = target;
        _stage = stage;
        _body = GetComponent<Rigidbody2D>();
        _health = gameObject.AddComponent<HealthSystem>();
        _health.Configure(data.MaxHealth);
        _health.Died += OnDied;
    }

    void FixedUpdate()
    {
        if (_target == null || _data == null || _health == null || _health.IsDead || _body == null)
            return;

        float dx = _target.position.x - transform.position.x;
        float dir = dx >= 0f ? 1f : -1f;
        float adx = Mathf.Abs(dx);
        float ady = Mathf.Abs(_target.position.y - transform.position.y);

        switch (_phase)
        {
            case Phase.Chase:
                Chase(dir, adx, ady);
                break;
            case Phase.Windup:
                Halt();
                _phaseLeft -= Time.fixedDeltaTime;
                if (_phaseLeft <= 0f)
                    Strike();
                break;
            case Phase.Recover:
                Halt();
                _phaseLeft -= Time.fixedDeltaTime;
                if (_phaseLeft <= 0f)
                    _phase = Phase.Chase;
                break;
        }

        Clamp();
        Face(_phase == Phase.Chase ? dir : _faceDir);
    }

    void Chase(float dir, float adx, float ady)
    {
        bool sameLevel = ady < 1.6f;
        if (_data.AttackKind == "explode")
        {
            // kamikaze: só arma a explosão colado no jogador, no mesmo nível
            if (sameLevel && adx < _data.AttackRange)
            {
                BeginWindup(dir);
                return;
            }
        }
        else
        {
            // machado e bola ácida: atira de média distância; funciona até
            // com o jogador em cima de uma plataforma
            if (adx > 2.2f && adx < _data.AttackRange && ady < 3.2f)
            {
                BeginWindup(dir);
                return;
            }
        }

        float move = adx < 0.08f ? 0f : dir;
        var velocity = _body.linearVelocity;
        velocity.x = move * _data.MoveSpeed;
        _body.linearVelocity = velocity;
    }

    void BeginWindup(float dir)
    {
        _faceDir = dir;
        _phase = Phase.Windup;
        _phaseLeft = _data.AttackWindup;
        Halt();
    }

    void Strike()
    {
        if (_data.AttackKind == "explode")
        {
            Explode();
            return;
        }

        // projéteis saem da "boca" na direção do jogador
        var mouth = transform.position + new Vector3(_faceDir * 0.45f, 1.15f, 0f);
        if (_data.AttackKind == "axe")
            AxeProjectile.Throw(mouth, _target, _data.AttackDamage);
        else
            AcidBall.Lob(mouth, _target, _data.AttackDamage);

        PixelBurst.Spawn(mouth, _data.Color, 3);
        _phase = Phase.Recover;
        _phaseLeft = Mathf.Max(0.1f, _data.AttackRecover);
    }

    void Explode()
    {
        var center = transform.position + Vector3.up * 0.8f;

        // dano em área se o jogador estiver no raio do estouro
        var playerCenter = (Vector2)_target.position + Vector2.up * 0.6f;
        if (Vector2.Distance(playerCenter, center) < 2.1f)
            HitPlayer(_data.AttackDamage);

        // estouro: núcleo laranja de fogo + respingos de carne corrompida
        PixelBurst.Spawn(center, new Color(1f, 0.62f, 0.2f), 10);
        PixelBurst.Spawn(center + Vector3.up * 0.5f, new Color(1f, 0.85f, 0.4f), 6);
        PixelBurst.Spawn(center + Vector3.left * 0.7f, new Color(0.45f, 0.62f, 0.28f), 6);
        PixelBurst.Spawn(center + Vector3.right * 0.7f, new Color(0.45f, 0.62f, 0.28f), 6);

        // o zumbi se destrói na explosão (conta como abate e avança a onda)
        DamageSystem.Apply(_health, 99999f);
    }

    void Halt()
    {
        var velocity = _body.linearVelocity;
        velocity.x = 0f;
        _body.linearVelocity = velocity;
    }

    void Clamp()
    {
        if (_stage == null)
            return;

        var position = transform.position;
        position.x = Mathf.Clamp(position.x, -_stage.HalfWidth + 0.6f, _stage.HalfWidth - 0.6f);
        transform.position = position;
    }

    void Face(float dir)
    {
        var scale = transform.localScale;
        scale.x = Mathf.Abs(scale.x) * Mathf.Sign(dir == 0f ? 1f : dir);
        transform.localScale = scale;
    }

    void HitPlayer(float amount)
    {
        DamagePlayer(_target, transform.position, amount);
    }

    // dano no jogador respeitando escudo e defesa — usado pela IA e pelos projéteis
    public static void DamagePlayer(Transform target, Vector3 from, float amount)
    {
        if (target == null)
            return;

        var health = target.GetComponent<HealthSystem>();
        var combat = target.GetComponent<PlayerCombat>();
        var player = target.GetComponent<PlayerController>();
        float incoming = amount;
        if (combat != null)
            combat.TryBlock(from, incoming, out incoming);

        if (incoming > 0f)
        {
            int defense = player != null && player.Hero != null ? player.Hero.Defense : 0;
            if (combat != null && combat.IsBlocking)
                defense += 40;
            DamageSystem.Apply(health, incoming, defense);
        }
    }

    // todo dano do jogador nos inimigos passa por aqui pra aplicar a resistência:
    // ghoul toma 40% a mais (frágil), zumbi toma 35% a menos (tanque)
    public void ReceiveDamage(float amount)
    {
        if (_health == null || _health.IsDead)
            return;

        float multiplier = _data != null ? _data.DamageTaken : 1f;
        DamageSystem.Apply(_health, amount * multiplier);
    }

    void OnTriggerStay2D(Collider2D other)
    {
        if (_data == null || _target == null || Time.time < _touchReady)
            return;

        var player = other.GetComponent<PlayerController>();
        if (player == null)
            player = other.GetComponentInParent<PlayerController>();
        if (player == null)
            return;

        HitPlayer(_data.TouchDamage);
        _touchReady = Time.time + _data.TouchCooldown;
    }

    void OnDied(HealthSystem _)
    {
        var stage = FindFirstObjectByType<GroundT1Controller>();
        if (stage != null)
            stage.RegisterKill();
        // estilhaço de pixels na cor da criatura ao morrer
        PixelBurst.Spawn(transform.position + Vector3.up * 0.8f, _data != null ? _data.Color : Color.gray, 7);
        Destroy(gameObject);
    }
}
