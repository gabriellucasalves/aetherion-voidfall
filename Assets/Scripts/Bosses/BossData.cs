using UnityEngine;

public static class BossData
{
    public const string DisplayName = "NAZOR, O DESTRUIDOR DE MUNDOS";
    public const string Subtitle = "Aquele que devorou Aetherion";
    public const float MaxHealth = 1000f;
    // A complete health pool per form: no lethal hit can skip a transformation.
    public static float FormHealth(int form) => form >= 3 ? 2200f : form == 2 ? 1500f : MaxHealth;
    public static float FormDamage(int form) => form >= 3 ? 1.30f : form == 2 ? 1.15f : 1f;
    public static float FormCadence(int form) => form >= 3 ? 1.32f : form == 2 ? 1.20f : 1.08f;
    public const float Phase1Threshold = 0.66f; // legacy proportion, not a form trigger
    public const float Phase2Threshold = 0.33f; // legacy proportion, not a form trigger
    public const float ContactDamage = 12f;
    public const float ContactCooldown = 0.85f;
    public const float FlyingDamageMultiplier = 0.60f;
    public const float ExposedNucleusMultiplier = 1.50f;
    public const float SupremeDamageCap = 260f;
    public const float TentacleMaxHealth = 45f;
    public const float TentacleDamageTransfer = 0.40f;
    public const float TransitionDuration = 1.50f;
    public const float IntroDurationFull = 6.0f;
    public const float IntroDurationShort = 2.0f;
}

