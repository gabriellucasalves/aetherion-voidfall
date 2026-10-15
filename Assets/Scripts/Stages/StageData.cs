using UnityEngine;

[CreateAssetMenu(fileName = "Stage", menuName = "Aetherion/Stage")]
public class StageData : ScriptableObject
{
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
}
