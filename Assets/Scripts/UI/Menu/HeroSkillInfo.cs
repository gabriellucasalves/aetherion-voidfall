using UnityEngine;

// Textos da tela de seleção (aprovados nos mocks). Valores conferidos no código de combate:
// recargas do Especial 6 s / 6,5 s / 6,5 s (SpecialController), Supremo usa o medidor SUPREMO (ResonanceMeter, máx. 100).
// O Especial do Guerreiro (L/Q) NÃO consome medidor — só recarga. O Supremo fica no E para os três.
public class HeroSkill
{
    public string Tag;      // BÁSICO / ESPECIAL / SUPREMO
    public string Key;      // tecla(s) mostradas no keycap
    public string Extra;    // texto depois do keycap ("recarga 6 s", "medidor SUPREMO")
    public string Name;
    public string Desc;
    public string Icon;     // Resources/Menu/selecao/<Icon>
}

public class HeroSkillInfo
{
    public string Id;
    public string Name;
    public string RoleTag;      // etiqueta curta do card
    public string RoleLine;     // linha à direita do nome no painel
    public string Description;
    public Color NameColor;
    public Color EspecialColor;
    public HeroSkill[] Skills;

    public static readonly Color SupremoColor = new Color32(248, 226, 156, 255);

    public static HeroSkillInfo For(string id)
    {
        if (id == "mago") return Mago;
        if (id == "arqueiro") return Arqueiro;
        return Guerreiro;
    }

    public static readonly HeroSkillInfo Guerreiro = new HeroSkillInfo
    {
        Id = "guerreiro", Name = "GUERREIRO", RoleTag = "TANQUE", RoleLine = "Tank · combate físico",
        Description = "Armadura pesada e espada. Muita vida e escudo forte (S/K) para segurar a linha de frente.",
        NameColor = new Color32(255, 214, 90, 255), EspecialColor = new Color32(255, 150, 60, 255),
        Skills = new[]
        {
            new HeroSkill { Tag = "BÁSICO", Key = "J", Extra = "", Name = "Corte de Energia",
                Desc = "Corte curto de espada à frente, rápido e seguro.", Icon = "icone_guerreiro_basico_24x24" },
            new HeroSkill { Tag = "ESPECIAL", Key = "L/Q", Extra = "recarga 6 s", Name = "Feixe Infernal",
                Desc = "Feixe de fogo à frente que fura a linha inimiga.", Icon = "icone_guerreiro_especial_24x24" },
            new HeroSkill { Tag = "SUPREMO", Key = "E", Extra = "medidor SUPREMO", Name = SpecialJapanese.WarriorName,
                Desc = "5 luas de fogo atravessam todos os inimigos e fecham com um corte gigante.", Icon = "icone_guerreiro_supremo_24x24" },
        }
    };

    public static readonly HeroSkillInfo Mago = new HeroSkillInfo
    {
        Id = "mago", Name = "MAGO", RoleTag = "CONTROLE", RoleLine = "Pulso rúnico · controle de zona",
        Description = "Manto e cajado. Ataca de longe e controla o campo. Campo de Magia (S/K) absorve dano antes da vida.",
        NameColor = new Color32(150, 230, 255, 255), EspecialColor = new Color32(196, 160, 236, 255),
        Skills = new[]
        {
            new HeroSkill { Tag = "BÁSICO", Key = "J", Extra = "", Name = "Pulso Rúnico",
                Desc = "Pulso largo de alto impacto à distância.", Icon = "icone_mago_basico_24x24" },
            new HeroSkill { Tag = "ESPECIAL", Key = "L/Q", Extra = "recarga 6,5 s", Name = "Zona Rúnica",
                Desc = "Círculo no chão: dano contínuo e lentidão por 3,5 s.", Icon = "icone_mago_especial_24x24" },
            new HeroSkill { Tag = "SUPREMO", Key = "E", Extra = "medidor SUPREMO", Name = SpecialJapanese.MageName,
                Desc = "Congela os inimigos, acumula o dano e explode em até 7 estrelas.", Icon = "icone_mago_supremo_24x24" },
        }
    };

    public static readonly HeroSkillInfo Arqueiro = new HeroSkillInfo
    {
        Id = "arqueiro", Name = "ARQUEIRO", RoleTag = "ATIRADOR", RoleLine = "Artilheiro arcano · alto dano",
        Description = "Capuz arcano e arco de cristal. Pouca vida, dano alto à distância, égide de cristal (S/K) e dash (Shift).",
        NameColor = new Color32(120, 220, 235, 255), EspecialColor = new Color32(120, 220, 235, 255),
        Skills = new[]
        {
            new HeroSkill { Tag = "BÁSICO", Key = "J", Extra = "", Name = "Flecha Arcana Carregada",
                Desc = "Segure para carregar 3 níveis e solte: o nível 3 dá o maior dano.", Icon = "icone_arqueiro_basico_24x24" },
            new HeroSkill { Tag = "ESPECIAL", Key = "L/Q", Extra = "recarga 6,5 s", Name = "Flecha Perfurante",
                Desc = "Atravessa até 3 inimigos e para no terreno.", Icon = "icone_arqueiro_especial_24x24" },
            new HeroSkill { Tag = "SUPREMO", Key = "E", Extra = "medidor SUPREMO", Name = SpecialJapanese.ArcherName,
                Desc = "Salto, 7 flechas teleguiadas e uma explosão atrasada.", Icon = "icone_arqueiro_supremo_24x24" },
        }
    };
}
