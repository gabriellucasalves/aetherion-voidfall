using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Desenha linhas de proporção (Guerreiro) no Scene View.
/// Anexe ao root do herói ou ao filho *Pixel. Não altera gameplay.
/// </summary>
[ExecuteAlways]
public class CharacterProportionDebug : MonoBehaviour
{
    [SerializeField] bool showGuides = true;
    [SerializeField] bool assumeVisualOffset = true;
    [SerializeField] bool showLabels = true;
    [SerializeField] Color crownColor = new Color(1f, 0.35f, 0.35f, 0.9f);
    [SerializeField] Color shouldersColor = new Color(1f, 0.7f, 0.2f, 0.9f);
    [SerializeField] Color handsColor = new Color(0.3f, 0.75f, 1f, 0.9f);
    [SerializeField] Color waistColor = new Color(0.3f, 0.9f, 0.45f, 0.9f);
    [SerializeField] Color kneesColor = new Color(0.7f, 0.4f, 1f, 0.9f);
    [SerializeField] Color feetColor = new Color(1f, 0.9f, 0.2f, 0.9f);
    [SerializeField] Color groundColor = new Color(0.7f, 0.7f, 0.7f, 0.9f);
    [SerializeField] float halfWidth = 1.1f;

    public bool ShowGuides
    {
        get => showGuides;
        set => showGuides = value;
    }

#if UNITY_EDITOR
    void OnDrawGizmos()
    {
        if (!showGuides)
            return;

        Vector3 origin = transform.position;
        if (assumeVisualOffset && transform.Find("GuerreiroPixel") == null
            && transform.Find("MagoPixel") == null
            && transform.Find("ArqueiroPixel") == null
            && !name.EndsWith("Pixel"))
        {
            // Componente no root do player: Visuals nascem com y=-0.5
            origin += Vector3.up * CharacterPixelArtStandards.VisualLocalOffsetY;
        }

        DrawLine(origin, CharacterPixelArtStandards.GuideCrownY, crownColor, "Head/Top");
        DrawLine(origin, CharacterPixelArtStandards.GuideHeadBottomY, crownColor, "Head base");
        DrawLine(origin, CharacterPixelArtStandards.GuideShouldersY, shouldersColor, "Shoulders");
        DrawLine(origin, CharacterPixelArtStandards.GuideHandsY, handsColor, "Hands");
        DrawLine(origin, CharacterPixelArtStandards.GuideWaistY, waistColor, "Waist");
        DrawLine(origin, CharacterPixelArtStandards.GuideKneesY, kneesColor, "Knees");
        DrawLine(origin, CharacterPixelArtStandards.GuideFeetY, feetColor, "Feet");
        DrawLine(origin, CharacterPixelArtStandards.GuideGroundY, groundColor, "Ground/Pivot");
    }

    void DrawLine(Vector3 origin, int yFromTop, Color color, string label)
    {
        float localY = CharacterPixelArtStandards.PixelRowToLocalY(yFromTop);
        var a = origin + new Vector3(-halfWidth, localY, 0f);
        var b = origin + new Vector3(halfWidth, localY, 0f);
        Handles.color = color;
        Handles.DrawLine(a, b, 2f);
        if (showLabels)
            Handles.Label(b + Vector3.right * 0.05f, label);
    }
#endif
}
