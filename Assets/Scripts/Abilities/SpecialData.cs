using UnityEngine;

// Ficha do Especial Supremo de cada herói. Criada em código, como AbilityData.
[CreateAssetMenu(fileName = "Special", menuName = "Aetherion/Special")]
public class SpecialData : ScriptableObject
{
    public string Id;
    public string HeroId;
    public string DisplayName;
    [TextArea] public string Callout;
    public string FinalShout;
    public int BustFrame;
    public Color PanelDark = new Color(0.28f, 0.05f, 0.05f);
    public Color PanelLight = new Color(0.5f, 0.06f, 0.07f);
    public Color Accent = new Color(1f, 0.839f, 0.251f);
    public Color Tint = new Color(0.45f, 0.04f, 0.05f, 0.38f);
    public float WorldSlowScale = 0.05f;
    public float HitStop = 0.08f;
    public float MinCooldown = 20f;

    public static SpecialData ForHero(CharacterData hero)
    {
        var data = CreateInstance<SpecialData>();
        data.HeroId = hero != null ? hero.Id : "guerreiro";

        if (data.HeroId == "mago")
        {
            data.Id = "supernova_arcana";
            data.DisplayName = "CONSTELAÇÃO ÍNDIGO: SUPERNOVA ARCANA";
            data.Callout = "Queima, Véu,\ndentro de mim!";
            data.FinalShout = "SUPERNOVA… ARCANA!";
            data.BustFrame = 18;
            data.PanelDark = new Color(0.07f, 0.04f, 0.16f, 0.96f);
            data.PanelLight = new Color(0.22f, 0.12f, 0.42f, 0.55f);
            data.Accent = new Color(0.86f, 0.55f, 1f);
            data.Tint = new Color(0.10f, 0.07f, 0.18f, 0.46f);
            data.HitStop = 0.06f;
            data.MinCooldown = 22f;
        }
        else if (data.HeroId == "arqueiro")
        {
            data.Id = "sete_estrelas";
            data.DisplayName = "SETE ESTRELAS DO CAÇADOR";
            data.Callout = "Sete estrelas…\num só destino.";
            data.FinalShout = "…Vocês já caíram.";
            data.BustFrame = 13;
            data.PanelDark = new Color(0.03f, 0.12f, 0.08f, 0.96f);
            data.PanelLight = new Color(0.08f, 0.32f, 0.20f, 0.55f);
            data.Accent = new Color(1f, 0.839f, 0.251f);
            data.Tint = new Color(0.03f, 0.06f, 0.14f, 0.42f);
            data.HitStop = 0.08f;
            data.MinCooldown = 20f;
        }
        else
        {
            data.Id = "muralha_rubra";
            data.DisplayName = "JURAMENTO DA MURALHA RUBRA";
            data.Callout = "O VAZIO\nNÃO PASSA!";
            data.FinalShout = "MURALHAAA… RUBRA!";
            data.BustFrame = 17;
            data.PanelDark = new Color(0.20f, 0.03f, 0.04f, 0.96f);
            data.PanelLight = new Color(0.55f, 0.08f, 0.06f, 0.5f);
            data.Accent = new Color(1f, 0.839f, 0.251f);
            data.Tint = new Color(0.42f, 0.04f, 0.05f, 0.40f);
            data.HitStop = 0.08f;
            data.MinCooldown = 20f;
        }

        return data;
    }
}
