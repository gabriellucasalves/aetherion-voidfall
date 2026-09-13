using UnityEngine;

public static class CharacterCatalog
{
    // FASE 5: ligar para liberar o Mago na tela de seleção.
    // FASE 1 mantém só o Guerreiro jogável na UI (demo T1 intacta).
    public const bool UnlockMagoInSelection = false;

    /// <summary>
    /// Force-test do Mago em T1 (seleção ainda trava no Guerreiro):
    /// <code>
    /// GameManager.EnsureExists().SelectHero(CharacterCatalog.ById("mago"));
    /// SceneTransitionManager.Instance.Load(GameScenes.GroundT1);
    /// </code>
    /// Ou ligue <c>GroundT1Controller.ForceMagoForTesting</c>.
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
        var all = All();
        var playable = new System.Collections.Generic.List<CharacterData>();
        for (int i = 0; i < all.Length; i++)
        {
            if (all[i] == null)
                continue;
            if (all[i].Id == "guerreiro")
                playable.Add(all[i]);
            // FASE 5 — desbloquear mago na seleção:
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
                "Corte de Energia", "Golpe de espada curto. Segura o escudo para bloquear.",
                new Color(0.72f, 0.74f, 0.82f)),
            // Kit: vida baixa, Power alto, alcance longo.
            // Especial FASE 3: Núcleo Arcano (L/Q) — explosão em área, CD 6.5s.
            Make("mago", "Mago", "Dano mágico / área",
                "Manto e cajado. Orbe teleguiado à distância; Núcleo Arcano explode em área.",
                80, 20, 100, 20, 50, 60,
                "Núcleo Arcano", "Especial (L/Q): explosão arcana em área, CD 6.5s. Orbe básico busca sozinho; clique / J força o tiro.",
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
