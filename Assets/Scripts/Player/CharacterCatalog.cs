using UnityEngine;

public static class CharacterCatalog
{
    public static CharacterData[] Playable()
    {
        // Demo WebGL: Guerreiro permanece; Arqueiro entra só para testar kit em T1.
        // Mago fica de fora até a demo permitir os 3 (sem retrato/sheet nova).
        var all = All();
        var playable = new System.Collections.Generic.List<CharacterData>();
        for (int i = 0; i < all.Length; i++)
        {
            if (all[i] == null)
                continue;
            if (all[i].Id == "guerreiro" || all[i].Id == "arqueiro")
                playable.Add(all[i]);
        }

        return playable.Count > 0 ? playable.ToArray() : all;
    }

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
            Make("arqueiro", "Arqueiro", "Mobilidade / equilíbrio",
                "Arco leve e aljava. Esquiva, avança e dispara flechas à distância.",
                100, 50, 65, 45, 100, 80,
                "Flecha", "Uma flecha reta na direção do olhar. Shift dá um dash curto com i-frames.",
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
