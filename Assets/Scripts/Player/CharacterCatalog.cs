using UnityEngine;

public static class CharacterCatalog
{
    // FASE 5: Mago liberado na seleção junto com o Guerreiro e o Arqueiro.
    public const bool UnlockMagoInSelection = true;

    /// <summary>
    /// Caminho principal: escolha o Mago na tela de seleção (UnlockMagoInSelection).
    /// Fallback de debug: <c>GroundT1Controller.ForceMagoForTesting = true</c>
    /// ou <c>GameManager.EnsureExists().SelectHero(CharacterCatalog.ById("mago"))</c>.
    /// </summary>
    public static CharacterData ById(string id)
    {
        if (string.IsNullOrEmpty(id))
            return null;

        var all = All();
        for (int i = 0; i < all.Length; i++)
        {
            if (all[i] != null && all[i].Id == id)
                return all[i];
        }

        return null;
    }

    public static CharacterData[] Playable()
    {
        // Demo WebGL: Guerreiro + Mago + Arqueiro jogáveis.
        var all = All();
        var playable = new System.Collections.Generic.List<CharacterData>();
        for (int i = 0; i < all.Length; i++)
        {
            if (all[i] == null)
                continue;
            if (all[i].Id == "guerreiro" || all[i].Id == "arqueiro")
                playable.Add(all[i]);
            else if (UnlockMagoInSelection && all[i].Id == "mago")
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
                "Corte de Energia", "Corte curto (J/clique). Especial L/Q: Lâmina/Onda de Energia à frente, CD 6s. Escudo (S/K/direito) bloqueia.",
                new Color(0.72f, 0.74f, 0.82f)),
            // Kit: vida baixa, Power alto, alcance longo.
            // Especial FASE 3: Núcleo Arcano (L/Q) — explosão em área, CD 6.5s.
            Make("mago", "Mago", "Dano mágico / área",
                "Manto e cajado. Orbe Arcano reto na facing; Núcleo Arcano explode em área.",
                80, 20, 100, 20, 50, 60,
                "Núcleo Arcano", "Especial (L/Q): explosão arcana em área, CD 6.5s. Orbe básico: clique / J (ou hold) na direção que o mago olha.",
                new Color(0.45f, 0.55f, 0.95f)),
            Make("arqueiro", "Arqueiro", "Mobilidade / equilíbrio",
                "Arco leve e aljava. Esquiva, avança e dispara flechas à distância.",
                100, 50, 65, 45, 100, 80,
                "Flecha", "Flecha reta (clique/J). Shift: dash. L/Q: rajada em leque (CD ~6.5s).",
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
