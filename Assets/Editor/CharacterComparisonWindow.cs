#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

/// <summary>
/// Compara Guerreiro | Arqueiro | Mago lado a lado com o mesmo chão/PPU/linhas.
/// Menu: Tools &gt; Pixel Art &gt; Character Comparison
/// Não altera prefabs de produção.
/// </summary>
public class CharacterComparisonWindow : EditorWindow
{
    const float PreviewScale = 8f;

    Texture2D _guerreiro;
    Texture2D _arqueiro;
    Texture2D _mago;
    bool _showGuides = true;
    Vector2 _scroll;

    struct HeroPreview
    {
        public string Name;
        public Texture2D Sheet;
        public Color Tint;
    }

    [MenuItem("Tools/Pixel Art/Character Comparison")]
    public static void Open()
    {
        var win = GetWindow<CharacterComparisonWindow>("Character Comparison");
        win.minSize = new Vector2(720, 420);
        win.LoadSheets();
    }

    void OnEnable() => LoadSheets();

    void LoadSheets()
    {
        _guerreiro = Resources.Load<Texture2D>("Guerreiro/sheet");
        _arqueiro = Resources.Load<Texture2D>("Arqueiro/sheet");
        _mago = Resources.Load<Texture2D>("Mago/sheet");
        if (_guerreiro == null)
            _guerreiro = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Resources/Guerreiro/sheet.png");
        if (_arqueiro == null)
            _arqueiro = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Resources/Arqueiro/sheet.png");
        if (_mago == null)
            _mago = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Resources/Mago/sheet.png");
    }

    void OnGUI()
    {
        EditorGUILayout.LabelField("Character Comparison (idle frame 0)", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Mesmo chão / PPU 15 / linhas do Guerreiro. Ferramenta de editor — não grava cena nem prefab.",
            MessageType.Info);

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Recarregar sheets", GUILayout.Width(140)))
                LoadSheets();
            _showGuides = GUILayout.Toggle(_showGuides, "Mostrar guias", GUILayout.Width(120));
        }

        EditorGUILayout.Space(4);
        _scroll = EditorGUILayout.BeginScrollView(_scroll);

        float cell = CharacterPixelArtStandards.CellSize;
        float draw = cell * PreviewScale;
        float startX = 40f;

        var heroes = new[]
        {
            new HeroPreview { Name = "Guerreiro", Sheet = _guerreiro, Tint = new Color(1f, 0.85f, 0.85f) },
            new HeroPreview { Name = "Arqueiro", Sheet = _arqueiro, Tint = new Color(0.85f, 1f, 0.85f) },
            new HeroPreview { Name = "Mago", Sheet = _mago, Tint = new Color(0.9f, 0.85f, 1f) }
        };

        var area = GUILayoutUtility.GetRect(
            startX * 2 + draw * 3 + 80,
            20f + draw + 60f);

        Handles.BeginGUI();
        float gY = area.y + 20f + CharacterPixelArtStandards.GuideGroundY * PreviewScale;
        Handles.color = new Color(0.75f, 0.75f, 0.75f, 1f);
        Handles.DrawLine(
            new Vector3(area.x + 20f, gY),
            new Vector3(area.xMax - 20f, gY), 2f);

        for (int i = 0; i < heroes.Length; i++)
        {
            float x = area.x + startX + i * (draw + 48f);
            float y = area.y + 20f;
            var hero = heroes[i];

            EditorGUI.LabelField(new Rect(x, y - 18f, draw, 18f), hero.Name, EditorStyles.boldLabel);

            if (hero.Sheet != null)
            {
                float u0 = 0f;
                float u1 = cell / hero.Sheet.width;
                float v1 = 1f;
                float v0 = 1f - cell / hero.Sheet.height;
                GUI.color = hero.Tint;
                GUI.DrawTextureWithTexCoords(
                    new Rect(x, y, draw, draw),
                    hero.Sheet,
                    new Rect(u0, v0, u1 - u0, v1 - v0));
                GUI.color = Color.white;
            }
            else
            {
                EditorGUI.HelpBox(new Rect(x, y, draw, 40f), "Sheet não encontrado", MessageType.Warning);
            }

            if (_showGuides)
                DrawGuidesGui(x, y, draw);
        }

        Handles.EndGUI();
        EditorGUILayout.EndScrollView();

        EditorGUILayout.Space();
        EditorGUILayout.LabelField(
            $"Contrato: célula {CharacterPixelArtStandards.CellSize} · PPU {CharacterPixelArtStandards.RuntimePixelsPerUnit} · " +
            $"pivot (0.5, {CharacterPixelArtStandards.FootPivotY:0.####}) · silhueta alvo ~{CharacterPixelArtStandards.TargetSilhouetteHeightPx}px",
            EditorStyles.miniLabel);
    }

    static void DrawGuidesGui(float x, float y, float draw)
    {
        void H(int row, Color c)
        {
            float py = y + row * (draw / CharacterPixelArtStandards.CellSize);
            Handles.color = c;
            Handles.DrawLine(new Vector3(x, py), new Vector3(x + draw, py), 1.5f);
        }

        H(CharacterPixelArtStandards.GuideCrownY, new Color(1f, 0.3f, 0.3f, 0.85f));
        H(CharacterPixelArtStandards.GuideShouldersY, new Color(1f, 0.7f, 0.2f, 0.85f));
        H(CharacterPixelArtStandards.GuideHandsY, new Color(0.3f, 0.8f, 1f, 0.85f));
        H(CharacterPixelArtStandards.GuideWaistY, new Color(0.3f, 0.9f, 0.4f, 0.85f));
        H(CharacterPixelArtStandards.GuideKneesY, new Color(0.7f, 0.4f, 1f, 0.85f));
        H(CharacterPixelArtStandards.GuideFeetY, new Color(1f, 0.9f, 0.2f, 0.85f));
        H(CharacterPixelArtStandards.GuideGroundY, new Color(0.8f, 0.8f, 0.8f, 0.9f));
    }
}
#endif
