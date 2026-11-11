using UnityEngine;

[CreateAssetMenu(fileName = "Stage", menuName = "Aetherion/Stage")]
public class StageData : ScriptableObject
{
    public static int ActivePhase = 1;

    public string Id;
    public string DisplayName;
    public string PhaseIndex;
    public string PhaseTitle;
    public string PhaseSubtitle;
    public float HalfWidth = 34f;
    public float HalfHeight = 8f;
    public float GroundTop = -2.35f;
    public Color GroundColor = new Color(0.16f, 0.1f, 0.09f);
    public Color SkyColor = new Color(0.18f, 0.09f, 0.14f);
    public Color RuinColor = new Color(0.28f, 0.18f, 0.16f);
    public Color AccentColor = new Color(0.42f, 0.16f, 0.28f);

    public static StageData CreateT1()
    {
        var data = CreateInstance<StageData>();
        data.Id = "t1";
        data.PhaseIndex = "Fase I";
        data.PhaseTitle = "Aetherion";
        data.PhaseSubtitle = "Antes da Queda";
        data.DisplayName = "Fase I — Antes da Queda";
        data.HalfWidth = 36f;
        data.HalfHeight = 8f;
        data.GroundTop = -2.35f;
        data.GroundColor = new Color(0.85f, 0.82f, 0.74f);
        data.SkyColor = new Color(0.48f, 0.73f, 0.93f);
        data.RuinColor = new Color(0.92f, 0.90f, 0.85f);
        data.AccentColor = new Color(0.95f, 0.85f, 0.45f);
        return data;
    }

    public static StageData CreateT2()
    {
        var data = CreateInstance<StageData>();
        data.Id = "t2";
        data.PhaseIndex = "Fase II";
        data.PhaseTitle = "Aetherion";
        data.PhaseSubtitle = "O Palácio em Queda";
        data.DisplayName = "Fase II — O Palácio em Queda";
        data.HalfWidth = 36f;
        data.HalfHeight = 8f;
        data.GroundTop = -2.35f;
        data.GroundColor = new Color(0.53f, 0.47f, 0.67f);
        data.SkyColor = new Color(0.102f, 0.086f, 0.188f); // #1A1630
        data.RuinColor = new Color(0.74f, 0.68f, 0.85f);
        data.AccentColor = new Color(0.96f, 0.58f, 0.24f);
        return data;
    }

    public static StageData CreateT3()
    {
        var data = CreateInstance<StageData>();
        data.Id = "t3";
        data.PhaseIndex = "Fase III";
        data.PhaseTitle = "Aetherion";
        data.PhaseSubtitle = "Em Ruínas";
        data.DisplayName = "Fase III — Aetherion em Ruínas";
        data.HalfWidth = 60f;
        data.HalfHeight = 8f;
        data.GroundTop = -2.0f;
        data.GroundColor = new Color(0.18f, 0.12f, 0.22f);
        data.SkyColor = new Color(0.06f, 0.04f, 0.10f);
        data.RuinColor = new Color(0.35f, 0.28f, 0.42f);
        data.AccentColor = new Color(0.72f, 0.25f, 0.85f);
        return data;
    }
}
