using System;
using UnityEngine;

public class ShieldSystem : MonoBehaviour
{
    public float Max { get; private set; }
    public float Current { get; private set; }
    public bool IsBroken => Current <= 0f;

    public event Action<ShieldSystem> Changed;

    float _regenDelay;

    public void Configure(float max)
    {
        Max = Mathf.Max(1f, max);
        Current = Max;
        _regenDelay = 0f;
        Changed?.Invoke(this);
    }

    public float Absorb(float amount)
    {
        if (amount <= 0f || IsBroken)
            return amount;

        float taken = Mathf.Min(Current, amount);
        Current -= taken;
        _regenDelay = 1.35f;
        Changed?.Invoke(this);
        return amount - taken;
    }

    void Update()
    {
        if (Current >= Max)
            return;

        if (_regenDelay > 0f)
        {
            _regenDelay -= Time.deltaTime;
            return;
        }

        Current = Mathf.Min(Max, Current + Max * 0.22f * Time.deltaTime);
        Changed?.Invoke(this);
    }
}
