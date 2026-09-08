using UnityEngine;

public static class DamageSystem
{
    public static void Apply(HealthSystem target, float rawDamage, int defense = 0)
    {
        if (target == null || target.IsDead)
            return;

        float reduced = rawDamage * (100f / (100f + Mathf.Max(0, defense)));
        target.TakeDamage(Mathf.Max(1f, reduced));
    }
}
