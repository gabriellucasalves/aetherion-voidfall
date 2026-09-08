using UnityEngine;

public static class CharacterCatalog
{
    public static CharacterData[] All()
    {
        var loaded = Resources.LoadAll<CharacterData>("Characters");
        if (loaded != null && loaded.Length > 0)
            return loaded;

        return new[]
        {
            Make("guerreiro", "Guerreiro", "Tank / combate físico",
                "Armadura pesada e espada. Segura a linha enquanto o Vazio avança.",
                150, 90, 20, 80, 30, 40,
                "Corte de Energia", "Golpe de espada curto. Segura o escudo para bloquear.",
                new Color(0.72f, 0.74f, 0.82f)),
            Make("mago", "Mago", "Dano mágico / área",
                "Manto e cajado. Queima o Vazio à distância com orbes arcanos.",
                80, 20, 100, 20, 50, 60,
                "Orbe Arcano", "Dispara energia automaticamente contra inimigos próximos.",
                new Color(0.45f, 0.55f, 0.95f)),
            Make("anjo", "Anjo", "Mobilidade / equilíbrio",
                "Asas e armadura leve. Esquiva, avança e corta com penas celestiais.",
                100, 50, 65, 45, 100, 80,
                "Pena Celestial", "Leque de penas na frente. Shift dá um dash curto.",
                new Color(0.93f, 0.82f, 0.42f))
        };
    }

    static CharacterData Make(
        string id, string name, string role, string description,
        int health, int strength, int power, int defense, int agility, int attackSpeed,
        string ability, string abilityDescription, Color accent)
    {
        var data = ScriptableObject.CreateInstance<CharacterData>();
        data.Id = id;
        data.DisplayName = name;
        data.Role = role;
        data.Description = description;
        data.MaxHealth = health;
        data.Strength = strength;
        data.Power = power;
        data.Defense = defense;
        data.Agility = agility;
        data.AttackSpeed = attackSpeed;
        data.AbilityName = ability;
        data.AbilityDescription = abilityDescription;
        data.Accent = accent;
        return data;
    }
}
