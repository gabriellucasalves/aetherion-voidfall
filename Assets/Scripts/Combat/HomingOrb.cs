using UnityEngine;

public class HomingOrb : MonoBehaviour
{
    Transform _target;
    float _speed;
    float _lifetime;
    float _damage;
    float _age;
    Vector2 _direction = Vector2.right;

    public void Launch(Vector2 direction, AbilityData ability, float damage, Transform target)
    {
        _direction = direction.sqrMagnitude > 0.01f ? direction.normalized : Vector2.right;
        _speed = ability.ProjectileSpeed;
        _lifetime = ability.Lifetime;
        _damage = damage;
        _target = target;
        ApplyLook();
    }

    void Update()
    {
        if (_target == null || !IsAlive(_target))
            _target = FindFallback(transform.position);

        if (_target != null)
        {
            var toTarget = (Vector2)(_target.position - transform.position);
            if (toTarget.sqrMagnitude > 0.01f)
                _direction = Vector2.Lerp(_direction, toTarget.normalized, 8f * Time.deltaTime).normalized;
        }

        transform.position += (Vector3)(_direction * _speed * Time.deltaTime);
        ApplyLook();
        _age += Time.deltaTime;
        if (_age >= _lifetime)
            Destroy(gameObject);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        var enemy = other.GetComponent<EnemyController>();
        if (enemy == null)
            enemy = other.GetComponentInParent<EnemyController>();
        if (enemy == null || enemy.Health == null || enemy.Health.IsDead)
            return;

        enemy.ReceiveDamage(_damage);
        PixelBurst.Spawn(transform.position, new Color(0.55f, 0.8f, 1f), 4);
        Destroy(gameObject);
    }

    void ApplyLook()
    {
        if (_direction.sqrMagnitude > 0.01f)
            transform.right = _direction;
    }

    static bool IsAlive(Transform target)
    {
        var enemy = target.GetComponent<EnemyController>();
        return enemy != null && enemy.Health != null && !enemy.Health.IsDead;
    }

    static Transform FindFallback(Vector2 origin)
    {
        var nearest = PlayerCombat.FindNearest(null, origin, 40f);
        return nearest != null ? nearest.transform : null;
    }
}
