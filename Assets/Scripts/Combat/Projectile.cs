using UnityEngine;

public class Projectile : MonoBehaviour
{
    Vector2 _direction;
    float _speed;
    float _lifetime;
    float _damage;
    float _age;

    public void Launch(Vector2 direction, AbilityData ability, float damage)
    {
        _direction = direction.normalized;
        _speed = ability.ProjectileSpeed;
        _lifetime = ability.Lifetime;
        _damage = damage;
        transform.right = _direction;
    }

    void Update()
    {
        transform.position += (Vector3)(_direction * _speed * Time.deltaTime);
        _age += Time.deltaTime;
        if (_age >= _lifetime)
            Destroy(gameObject);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        var enemy = other.GetComponent<EnemyController>();
        if (enemy == null)
            enemy = other.GetComponentInParent<EnemyController>();
        if (enemy == null || enemy.Health == null)
            return;

        enemy.ReceiveDamage(_damage);
        Destroy(gameObject);
    }
}
