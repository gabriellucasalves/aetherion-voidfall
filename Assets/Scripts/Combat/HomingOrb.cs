using UnityEngine;

/// <summary>
/// Orbe Arcano do Mago: segue o alvo travado e re-adquire o inimigo vivo mais próximo
/// dentro do raio de lock. Dano e velocidade vêm de <see cref="AbilityData.CreateOrbe"/>.
/// </summary>
public class HomingOrb : MonoBehaviour
{
    Transform _target;
    float _speed;
    float _lifetime;
    float _damage;
    float _age;
    float _lockRange;
    Vector2 _direction = Vector2.right;
    bool _hit;

    public void Launch(Vector2 direction, AbilityData ability, float damage, Transform target)
    {
        _direction = direction.sqrMagnitude > 0.01f ? direction.normalized : Vector2.right;
        _speed = ability != null ? ability.ProjectileSpeed : 11f;
        _lifetime = ability != null ? ability.Lifetime : 1.7f;
        _damage = damage;
        _target = target;
        // re-lock em voo um pouco além do range de disparo, sem magnetizar o mapa inteiro
        float abilityRange = ability != null ? ability.Range : 9.2f;
        _lockRange = Mathf.Max(12f, abilityRange * 1.5f);
        EnsureCollider();
        ApplyLook();
    }

    void EnsureCollider()
    {
        var body = GetComponent<Rigidbody2D>();
        if (body == null)
        {
            body = gameObject.AddComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Kinematic;
            body.gravityScale = 0f;
        }
        body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        body.interpolation = RigidbodyInterpolation2D.Interpolate;

        var box = GetComponent<BoxCollider2D>();
        if (box != null)
        {
            box.enabled = false;
            Destroy(box);
        }

        var circle = GetComponent<CircleCollider2D>();
        if (circle == null)
            circle = gameObject.AddComponent<CircleCollider2D>();
        circle.isTrigger = true;
        circle.radius = 0.55f;
    }

    void Update()
    {
        if (_hit)
            return;

        if (_target == null || !IsAlive(_target))
            _target = FindFallback(transform.position, _lockRange);

        if (_target != null)
        {
            Vector2 aim = AimPoint(_target);
            var toTarget = aim - (Vector2)transform.position;
            if (toTarget.sqrMagnitude > 0.01f)
            {
                // curva firme o bastante para acompanhar inimigos em movimento
                float turn = 14f * Time.deltaTime;
                _direction = Vector2.Lerp(_direction, toTarget.normalized, turn).normalized;
            }
        }

        transform.position += (Vector3)(_direction * _speed * Time.deltaTime);
        ApplyLook();
        _age += Time.deltaTime;
        if (_age >= _lifetime)
            Destroy(gameObject);
    }

    void OnTriggerEnter2D(Collider2D other) => TryHit(other);

    void OnTriggerStay2D(Collider2D other) => TryHit(other);

    void TryHit(Collider2D other)
    {
        if (_hit || other == null)
            return;

        var enemy = other.GetComponent<EnemyController>();
        if (enemy == null)
            enemy = other.GetComponentInParent<EnemyController>();
        if (enemy == null || enemy.Health == null || enemy.Health.IsDead)
            return;

        _hit = true;
        enemy.ReceiveDamage(_damage);
        PixelBurst.Spawn(transform.position, new Color(0.55f, 0.8f, 1f), 4);
        Destroy(gameObject);
    }

    void ApplyLook()
    {
        if (_direction.sqrMagnitude > 0.01f)
            transform.right = _direction;
    }

    static Vector2 AimPoint(Transform target)
    {
        // mira o tronco (inimigos T1 são altos ~1.7)
        return (Vector2)target.position + Vector2.up * 0.55f;
    }

    static bool IsAlive(Transform target)
    {
        if (target == null)
            return false;
        var enemy = target.GetComponent<EnemyController>();
        if (enemy == null)
            enemy = target.GetComponentInParent<EnemyController>();
        return enemy != null && enemy.Health != null && !enemy.Health.IsDead;
    }

    static Transform FindFallback(Vector2 origin, float range)
    {
        var nearest = PlayerCombat.FindNearest(null, origin, range);
        return nearest != null ? nearest.transform : null;
    }
}
