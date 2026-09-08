using UnityEngine;

[CreateAssetMenu(fileName = "Stage", menuName = "Aetherion/Stage")]
public class StageData : ScriptableObject
{
    public string Id;
    public string DisplayName;
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
        data.DisplayName = "Ruínas da Borda";
        data.HalfWidth = 34f;
        data.HalfHeight = 8f;
        data.GroundTop = -2.35f;
        data.GroundColor = new Color(0.05f, 0.07f, 0.08f);
        data.SkyColor = new Color(0.04f, 0.06f, 0.1f);
        data.RuinColor = new Color(0.16f, 0.2f, 0.24f);
        data.AccentColor = new Color(0.42f, 0.52f, 0.58f);
        return data;
    }
}
