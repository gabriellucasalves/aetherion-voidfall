using System.Collections.Generic;
using UnityEngine;

// Feixe Infernal: retângulo horizontal no torso. O visual nunca aumenta a área de dano.
public class InfernalBeam : MonoBehaviour
{
    public const float Length = 7f;
    public const float Height = 0.45f;
    public const float Startup = 0.15f;
    public const float ActiveDuration = 0.4f;
    public const float DamageScale = 1.2f;
    const float MuzzleOffset = 0.35f;
    readonly HashSet<EnemyController> _hit = new HashSet<EnemyController>();
    Transform _owner;
    HealthSystem _health;
    Collider2D _ownerCollider;
    float _direction;
    float _damage;
    float _age;
    SpriteRenderer _renderer;
    static Sprite[] _frames;
    public bool IsActive => _age >= Startup && _age < Startup + ActiveDuration;
    public Bounds DamageBounds { get; private set; }

    public static InfernalBeam Launch(Transform owner, Vector2 facing, float damage)
    {
        var go = new GameObject("FeixeInfernal");
        var beam = go.AddComponent<InfernalBeam>();
        beam._owner = owner;
        beam._health = owner.GetComponent<HealthSystem>();
        beam._ownerCollider = owner.GetComponent<Collider2D>();
        beam._direction = facing.x < 0f ? -1f : 1f;
        beam._damage = damage;
        beam._renderer = go.AddComponent<SpriteRenderer>();
        beam._renderer.sortingOrder = 21;
        if (_frames == null)
        {
            var texture = Resources.Load<Texture2D>("Effects/feixe_infernal");
            if (texture != null)
            {
                _frames = new Sprite[4];
                for (int i = 0; i < 4; i++)
                    _frames[i] = Sprite.Create(texture, new Rect(0, i * 8, 112, 8), new Vector2(0.5f, 0.5f), 16f);
            }
        }
        beam.UpdatePose();
        return beam;
    }

    void UpdatePose()
    {
        float torsoY = _ownerCollider != null ? _ownerCollider.bounds.center.y + 0.1f : _owner.position.y;
        var center = new Vector3(_owner.position.x + _direction * (MuzzleOffset + Length * 0.5f), torsoY, 0f);
        transform.position = center;
        DamageBounds = new Bounds(center, new Vector3(Length, Height, 0f));
        if (_frames != null)
            _renderer.sprite = _frames[(int)(_age * 24f) % _frames.Length];
        _renderer.flipX = _direction < 0f;
        _renderer.enabled = IsActive;
    }

    void FixedUpdate()
    {
        if (_owner == null || (_health != null && _health.IsDead))
        {
            Destroy(gameObject);
            return;
        }
        _age += Time.fixedDeltaTime;
        UpdatePose();
        if (_age >= Startup + ActiveDuration)
        {
            Destroy(gameObject);
            return;
        }
        if (!IsActive)
            return;
        foreach (var collider in Physics2D.OverlapBoxAll(DamageBounds.center, DamageBounds.size, 0f))
        {
            var enemy = collider.GetComponentInParent<EnemyController>();
            if (enemy != null && enemy.Health != null && !enemy.Health.IsDead && _hit.Add(enemy))
                enemy.ReceiveDamage(_damage);
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(DamageBounds.center, DamageBounds.size);
    }
}
