using UnityEngine;

[CreateAssetMenu(fileName = "Ability", menuName = "Aetherion/Ability")]
public class AbilityData : ScriptableObject
{
    public string Id;
    public string DisplayName;
    public float Range = 2.8f;
    public float Cooldown = 0.7f;
    public float ProjectileSpeed = 14f;
    public float Lifetime = 0.28f;
    public Vector2 ProjectileSize = new Vector2(0.95f, 0.32f);
    public float DamageScale = 0.4f;
    public Color Color = new Color(1f, 0.72f, 0.25f);

    public static AbilityData CreateCorte()
    {
        var data = CreateInstance<AbilityData>();
        data.Id = "corte";
        data.DisplayName = "Corte de Energia";
        // Janela ~0.28s cobre frames 14–16 do ataque (12 fps); hitbox ativa após delay no MeleeSlash.
        data.Range = 2.1f;
        data.Cooldown = 0.48f;
        data.ProjectileSpeed = 0f;
        data.Lifetime = 0.28f;
        data.ProjectileSize = new Vector2(1.9f, 1.15f);
        data.DamageScale = 0.45f;
        data.Color = new Color(1f, 0.7f, 0.22f);
        return data;
    }

    public static AbilityData CreateOrbe()
    {
        var data = CreateInstance<AbilityData>();
        data.Id = "orbe";
        data.DisplayName = "Orbe Arcano";
        // Kit mago: tiro reto na facing (ArcaneBolt), alcance longo, Power alto.
        // CD real ≈ Cooldown * (40 / AttackSpeed). Sem homing / lock de alvo.
        data.Range = 9.2f;
        data.Cooldown = 0.55f;
        data.ProjectileSpeed = 13f;
        data.Lifetime = 1.55f;
        data.ProjectileSize = new Vector2(0.55f, 0.55f);
        data.DamageScale = 0.36f; // dano = Power * DamageScale (100 → 36); Guerreiro melee ainda bate mais forte
        data.Color = new Color(0.4f, 0.78f, 1f);
        return data;
    }

    public static AbilityData CreateNucleoArcano()
    {
        var data = CreateInstance<AbilityData>();
        data.Id = "nucleo_arcano";
        data.DisplayName = "Núcleo Arcano";
        data.Range = 9.2f;
        data.Cooldown = 6.5f; // especial — CD fixo no PlayerCombat.MageSpecialCooldown
        data.ProjectileSpeed = 0f;
        data.Lifetime = 0.42f;
        data.ProjectileSize = new Vector2(3.4f, 3.4f);
        data.DamageScale = 0.95f; // dano = Power * DamageScale
        data.Color = new Color(0.4f, 0.75f, 1f);
        return data;
    }

    /// <summary>
    /// Especial do Guerreiro — Lâmina / Onda de Energia (projétil horizontal).
    /// CD real fica em PlayerCombat.WarriorSpecialCooldown (~6s), independente do Corte.
    /// </summary>
    public static AbilityData CreateOndaEspada()
    {
        var data = CreateInstance<AbilityData>();
        data.Id = "onda_espada";
        data.DisplayName = "Lâmina de Energia";
        data.Range = 6f; // alcance efetivo ≈ speed * lifetime
        data.Cooldown = 6f;
        data.ProjectileSpeed = 14f;
        data.Lifetime = 0.42f; // ~5.9 unidades de voo
        data.ProjectileSize = new Vector2(2.2f, 1.05f);
        data.DamageScale = 0.95f; // dano = Strength * DamageScale (90 → ~85.5)
        data.Color = new Color(1f, 0.72f, 0.2f);
        return data;
    }

    public static AbilityData CreateFlecha()
    {
        // Média distância: projétil rápido; dano híbrido Str/Power é aplicado em PlayerCombat.
        var data = CreateInstance<AbilityData>();
        data.Id = "flecha";
        data.DisplayName = "Flecha";
        data.Range = 7.4f;
        data.Cooldown = 0.38f;
        data.ProjectileSpeed = 19f;
        data.Lifetime = 0.48f;
        data.ProjectileSize = new Vector2(0.78f, 0.14f);
        data.DamageScale = 0.36f;
        data.Color = new Color(1f, 0.82f, 0.35f);
        return data;
    }

    public static AbilityData ForHero(CharacterData hero)
    {
        if (hero != null && hero.Id == "mago")
            return CreateOrbe();
        if (hero != null && hero.Id == "arqueiro")
            return CreateFlecha();
        return CreateCorte();
    }
}
