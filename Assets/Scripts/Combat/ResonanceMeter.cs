using System;
using UnityEngine;

// Barra de Ressonância do Véu (0–100). Enche com acertos, abates, dano recebido
// e bloqueio perfeito. Cheia libera o Especial Supremo (E / botão SUPREMO).
public class ResonanceMeter : MonoBehaviour
{
    public const float Max = 100f;
    public const float PerHitDealt = 4f;
    public const float PerHitTaken = 6f;
    public const float PerKill = 10f;
    public const float PerPerfectBlock = 8f;

    float _gainScale = 1f;
    HealthSystem _health;
    bool _hooked;

    public float Current { get; private set; }
    public bool IsFull => Current >= Max - 0.01f;
    public float Normalized => Max <= 0f ? 0f : Current / Max;
    public event Action<ResonanceMeter> Changed;

    public void Configure(CharacterData hero)
    {
        Unhook();

        // Poder → habilidade especial (prompt.md §21). Mago ganha +25% pela fragilidade.
        float power = hero != null ? hero.Power : 50f;
        _gainScale = Mathf.Lerp(0.85f, 1.25f, Mathf.InverseLerp(20f, 100f, power));
        if (hero != null && hero.Id == "mago")
            _gainScale *= 1.25f;

        _health = GetComponent<HealthSystem>();
        if (_health != null)
            _health.Damaged += OnPlayerDamaged;
        EnemyController.AnyDamaged += OnEnemyDamaged;
        EnemyController.AnyDied += OnEnemyDied;
        _hooked = true;

        Current = 0f;
        Changed?.Invoke(this);
    }

    void OnDestroy()
    {
        Unhook();
    }

    void Unhook()
    {
        if (!_hooked)
            return;
        if (_health != null)
            _health.Damaged -= OnPlayerDamaged;
        EnemyController.AnyDamaged -= OnEnemyDamaged;
        EnemyController.AnyDied -= OnEnemyDied;
        _hooked = false;
    }

    void OnPlayerDamaged(HealthSystem _) => Add(PerHitTaken);
    void OnEnemyDamaged(EnemyController _, float __) => Add(PerHitDealt);
    void OnEnemyDied(EnemyController _) => Add(PerKill);

    public void Add(float amount)
    {
        if (amount <= 0f || IsFull || SpecialController.IsCinematic)
            return;
        if (_health != null && _health.IsDead)
            return;

        Current = Mathf.Min(Max, Current + amount * _gainScale);
        Changed?.Invoke(this);
    }

    public bool TrySpend()
    {
        if (!IsFull)
            return false;
        Current = 0f;
        Changed?.Invoke(this);
        return true;
    }
}
