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
        data.Range = 1.7f;
        data.Cooldown = 0.52f;
        data.ProjectileSpeed = 0f;
        data.Lifetime = 0.16f;
        data.ProjectileSize = new Vector2(1.55f, 0.85f);
        data.DamageScale = 0.45f;
        data.Color = new Color(1f, 0.7f, 0.22f);
        return data;
    }

    public static AbilityData CreateOrbe()
    {
        var data = CreateInstance<AbilityData>();
        data.Id = "orbe";
        data.DisplayName = "Orbe Arcano";
        // Kit mago: alcance longo, Power alto. CD real ≈ Cooldown * (40 / AttackSpeed).
        data.Range = 9.2f;
        data.Cooldown = 0.55f;
        data.ProjectileSpeed = 11f;
        data.Lifetime = 1.7f;
        data.ProjectileSize = new Vector2(0.5f, 0.5f);
        data.DamageScale = 0.36f; // dano = Power * DamageScale (100 → 36); Guerreiro melee ainda bate mais forte
        data.Color = new Color(0.45f, 0.72f, 1f);
        return data;
    }

    public static AbilityData CreatePena()
    {
        var data = CreateInstance<AbilityData>();
        data.Id = "pena";
        data.DisplayName = "Pena Celestial";
        data.Range = 7.1f;
        data.Cooldown = 0.4f;
        data.ProjectileSpeed = 17.5f;
        data.Lifetime = 0.52f;
        data.ProjectileSize = new Vector2(0.72f, 0.16f);
        data.DamageScale = 0.18f;
        data.Color = new Color(1f, 0.88f, 0.42f);
        return data;
    }

    public static AbilityData ForHero(CharacterData hero)
    {
        if (hero != null && hero.Id == "mago")
            return CreateOrbe();
        if (hero != null && hero.Id == "anjo")
            return CreatePena();
        return CreateCorte();
    }
}
