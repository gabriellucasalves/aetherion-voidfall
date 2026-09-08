using System;
using UnityEngine;

public class HealthSystem : MonoBehaviour
{
    public float Max { get; private set; }
    public float Current { get; private set; }
    public bool IsDead => Current <= 0f;
    public bool IsInvulnerable { get; set; }

    public event Action<HealthSystem> Changed;
    public event Action<HealthSystem> Damaged;
    public event Action<HealthSystem> Died;

    public void Configure(float max)
    {
        Max = Mathf.Max(1f, max);
        Current = Max;
        Changed?.Invoke(this);
    }

    public void TakeDamage(float amount)
    {
        if (IsDead || IsInvulnerable)
            return;

        Current = Mathf.Max(0f, Current - amount);
        Changed?.Invoke(this);
        Damaged?.Invoke(this);
        if (IsDead)
            Died?.Invoke(this);
    }
}
