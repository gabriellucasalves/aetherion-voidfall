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
                "Armadura pesada e espada. Muita vida e escudo forte (S/K) para segurar a linha de frente.",
                160, 90, 20, 90, 30, 40,
                "Corte de Energia", "Corte curto (J/clique). Especial (L/Q): Feixe Infernal à frente, só recarga de 6 s (não gasta medidor). Supremo (E): Lâmina Rubra, usa o medidor SUPREMO. Escudo (S/K/direito) bloqueia.",
                new Color(0.72f, 0.74f, 0.82f)),
            // Kit novo do Mago: vida intermediária (115), Pulso Rúnico largo no básico (52) e Zona Rúnica persistente no especial (L/Q).
            Make("mago", "Mago", "Pulso rúnico / controle de zona",
                "Manto e cajado. Dispara Pulso Rúnico largo com impacto forte, cria Zona Rúnica no chão e canaliza Campo de Magia.",
                115, 20, 100, 20, 50, 60,
                "Pulso Rúnico & Zona Rúnica", "Básico (J/clique): Pulso Rúnico largo de alto impacto. Especial (L/Q): Zona Rúnica no chão (dano contínuo + lentidão, CD 6.5s). Campo (S/K/direito): hold absorve o dano na barra de escudo antes da vida.",
                new Color(0.45f, 0.55f, 0.95f)),
            Make("arqueiro", "Arqueiro", "Artilheiro arcano / alto dano",
                "Capuz arcano e arco de cristal. Menos vida, mas carrega disparos de altíssimo dano à distância e esquiva rápido.",
                70, 50, 65, 45, 100, 80,
                "Flecha Arcana Carregada", "Clique/J: segura para carregar (N3 causa o maior dano básico) e solta para atirar 1 flecha. L/Q: flecha perfurante (até 3 alvos, para no terreno, CD 6.5s). S/K/direito: égide de cristal. Shift: dash.",
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
