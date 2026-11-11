using System.Collections.Generic;
using UnityEngine;

// Projétil: meia-lua de fogo que atravessa inimigos e terreno (1 hit por inimigo por projétil).
public class FireCrescent : MonoBehaviour
{
    public const float WaveSpeed = 14f, WaveLife = 0.85f;
    public const float FinalSpeed = 18f, FinalLife = 1.0f;
    static readonly Vector2 WaveBox = new Vector2(1.8f, 2.8f);
    static readonly Vector2 FinalBox = new Vector2(4.0f, 5.5f);

    public static int Alive { get; private set; }
    static Sprite[] _wave, _final;

    readonly HashSet<EnemyController> _hit = new HashSet<EnemyController>();
    Transform _owner;
    HealthSystem _ownerHealth;
    SpriteRenderer _renderer;
    Sprite[] _frames;
    float _dir, _damage, _speed, _life, _age, _emberClock;
    bool _isFinal;

    public bool IsFinal => _isFinal;
    public Bounds DamageBounds { get; private set; }

    public static FireCrescent Spawn(Transform owner, Vector3 position, float dir, float damage, bool final)
    {
        if (_wave == null)
            _wave = TrioAnimationFrames.Vfx("rajada_crescente_6f_40x48", 40, 48, 6, new Vector2(0.5f, 0.5f));
        if (_final == null)
            _final = TrioAnimationFrames.Vfx("rajada_crescente_final_6f_72x96", 72, 96, 6, new Vector2(0.5f, 0.5f));

        var go = new GameObject(final ? "LuaRubraFinal" : "LuaRubra");
        go.transform.position = position;
        var c = go.AddComponent<FireCrescent>();
        c._owner = owner;
        c._ownerHealth = owner != null ? owner.GetComponent<HealthSystem>() : null;
        c._dir = dir < 0f ? -1f : 1f;
        c._damage = damage;
        c._isFinal = final;
        c._speed = final ? FinalSpeed : WaveSpeed;
        c._life = final ? FinalLife : WaveLife;
        c._frames = final ? _final : _wave;
        c._renderer = go.AddComponent<SpriteRenderer>();
        c._renderer.sortingOrder = final ? 23 : 22;
        c._renderer.flipX = c._dir < 0f;
        if (c._frames != null)
            c._renderer.sprite = c._frames[0];
        c.UpdateBounds();
        Alive++;
        return c;
    }

    void OnDestroy()
    {
        Alive = Mathf.Max(0, Alive - 1);
    }

    void UpdateBounds()
    {
        var size = _isFinal ? FinalBox : WaveBox;
        DamageBounds = new Bounds(transform.position, new Vector3(size.x, size.y, 0f));
    }

    void Update()
    {
        if (_ownerHealth != null && _ownerHealth.IsDead)
        {
            Destroy(gameObject);
            return;
        }

        // Visual: 0 nasce (0,05 s) · 1-4 em loop a 16 fps · 5 nos últimos 0,12 s.
        if (_frames != null)
        {
            int f;
            if (_age < 0.05f) f = 0;
            else if (_age > _life - 0.12f) f = 5;
            else f = 1 + (int)((_age - 0.05f) * 16f) % 4;
            _renderer.sprite = _frames[f];
        }

        _emberClock += Time.deltaTime;
        while (_emberClock >= 0.04f)
        {
            _emberClock -= 0.04f;
            float back = _isFinal ? 1.6f : 0.9f;
            float spread = _isFinal ? 2.2f : 1.0f;
            RajadaFx.Ember(transform.position + new Vector3(-_dir * back, Random.Range(-spread, spread), 0f));
        }
    }

    void FixedUpdate()
    {
        _age += Time.fixedDeltaTime;
        if (_age >= _life)
        {
            Destroy(gameObject);
            return;
        }

        transform.position += new Vector3(_dir * _speed * Time.fixedDeltaTime, 0f, 0f);
        UpdateBounds();

        foreach (var collider in Physics2D.OverlapBoxAll(DamageBounds.center, DamageBounds.size, 0f))
        {
            var enemy = collider.GetComponentInParent<EnemyController>();
            if (enemy == null || enemy.Health == null || enemy.Health.IsDead || !_hit.Add(enemy))
                continue;
            Strike(enemy, collider.bounds.center);
        }
    }

    void Strike(EnemyController enemy, Vector3 at)
    {
        enemy.ReceiveDamage(_damage);
        RajadaFx.Impact(at);
        // O Devorador do Vazio toma dano (com o teto de Supremo dele) mas não sofre empurrão nem stun.
        if (enemy.GetComponent<VoidDevourerBoss>() != null)
            return;
        var v = _isFinal
            ? new Vector2(_dir * CrimsonCrescents.FinalKnockX, CrimsonCrescents.FinalKnockY)
            : new Vector2(_dir * CrimsonCrescents.WaveKnockX, CrimsonCrescents.WaveKnockY);
        enemy.Knockback(v, _isFinal ? CrimsonCrescents.FinalStun : CrimsonCrescents.WaveStun);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.5f, 0f);
        Gizmos.DrawWireCube(DamageBounds.center, DamageBounds.size);
    }
}
