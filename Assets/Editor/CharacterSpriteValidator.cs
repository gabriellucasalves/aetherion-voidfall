#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Compara Guerreiro / Arqueiro / Mago contra o contrato do audit.
/// Nunca altera arte automaticamente.
/// Menu: Tools &gt; Pixel Art &gt; Character Validator
/// </summary>
public class CharacterSpriteValidator : EditorWindow
{
    Vector2 _scroll;
    readonly List<string> _report = new List<string>();

    struct HeroInfo
    {
        public string Id;
        public string Display;
        public string ResourcesPath;
        public int ExpectedRows;
    }

    static readonly HeroInfo[] Heroes =
    {
        new HeroInfo
        {
            Id = "guerreiro", Display = "Guerreiro",
            ResourcesPath = "Assets/Resources/Guerreiro/sheet.png",
            ExpectedRows = CharacterPixelArtStandards.GuerreiroArqueiroRows
        },
        new HeroInfo
        {
            Id = "arqueiro", Display = "Arqueiro",
            ResourcesPath = "Assets/Resources/Arqueiro/sheet.png",
            ExpectedRows = CharacterPixelArtStandards.GuerreiroArqueiroRows
        },
        new HeroInfo
        {
            Id = "mago", Display = "Mago",
            ResourcesPath = "Assets/Resources/Mago/sheet.png",
            ExpectedRows = CharacterPixelArtStandards.MagoRows
        }
    };

    [MenuItem("Tools/Pixel Art/Character Validator")]
    public static void Open()
    {
        var win = GetWindow<CharacterSpriteValidator>("Character Validator");
        win.minSize = new Vector2(520, 400);
        win.RunValidation();
    }

    void OnGUI()
    {
        EditorGUILayout.LabelField("Character Sprite Validator", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Somente diagnóstico. Não corrige sprites/prefabs.\n" +
            "✅ ok  ·  ⚠ aviso (legado conhecido)  ·  ❌ fora do contrato Guerreiro.",
            MessageType.Info);

        if (GUILayout.Button("Revalidar", GUILayout.Height(28)))
            RunValidation();

        _scroll = EditorGUILayout.BeginScrollView(_scroll);
        foreach (var line in _report)
        {
            var style = EditorStyles.label;
            if (line.StartsWith("❌"))
                style = EditorStyles.boldLabel;
            EditorGUILayout.LabelField(line, style);
        }
        EditorGUILayout.EndScrollView();
    }

    void RunValidation()
    {
        _report.Clear();
        _report.Add($"Contrato: célula {CharacterPixelArtStandards.CellSize}, " +
                    $"PPU runtime {CharacterPixelArtStandards.RuntimePixelsPerUnit}, " +
                    $"pivot (0.5, {CharacterPixelArtStandards.FootPivotY:0.####}), Point, Uncompressed.");
        _report.Add($"Altura alvo ~{CharacterPixelArtStandards.TargetSilhouetteHeightPx}px " +
                    $"(máx construção {CharacterPixelArtStandards.TargetSilhouetteHeightMaxPx}px); " +
                    $"pés y={CharacterPixelArtStandards.GuideFeetY}.");
        _report.Add("");

        foreach (var hero in Heroes)
            ValidateHero(hero);

        ValidateSceneScales();
        Repaint();
    }

    void ValidateHero(HeroInfo hero)
    {
        _report.Add($"━━ {hero.Display} ━━");
        if (!File.Exists(hero.ResourcesPath))
        {
            _report.Add("❌ Sheet Resources ausente: " + hero.ResourcesPath);
            _report.Add("");
            return;
        }

        var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(hero.ResourcesPath);
        var importer = AssetImporter.GetAtPath(hero.ResourcesPath) as TextureImporter;

        int cell = CharacterPixelArtStandards.CellSize;
        int cols = CharacterPixelArtStandards.SheetColumns;
        int expectW = cols * cell;
        int expectH = hero.ExpectedRows * cell;

        if (tex != null)
        {
            Mark(tex.width == expectW && tex.height == expectH,
                $"Resolução sheet {tex.width}×{tex.height} (esperado {expectW}×{expectH})",
                warnOnly: false);
            Mark(tex.width % cell == 0 && tex.height % cell == 0,
                $"Célula divisível por {cell}", warnOnly: false);
        }
        else
            _report.Add("❌ Não carregou Texture2D");

        if (importer != null)
        {
            Mark(importer.textureType == TextureImporterType.Sprite, "Texture Type = Sprite", false);
            Mark(importer.filterMode == FilterMode.Point, "Filter = Point", false);
            Mark(!importer.mipmapEnabled, "Mipmaps Off", false);
            Mark(importer.isReadable, "Read/Write On", false);

            var platform = importer.GetDefaultPlatformTextureSettings();
            Mark(platform.textureCompression == TextureImporterCompression.Uncompressed,
                "Compression = None", false);

            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);

            bool ppuRuntime = Mathf.Approximately(settings.spritePixelsPerUnit,
                CharacterPixelArtStandards.RuntimePixelsPerUnit);
            bool ppuLegacy = Mathf.Approximately(settings.spritePixelsPerUnit,
                CharacterPixelArtStandards.MetaPixelsPerUnitLegacy);
            if (ppuRuntime)
                _report.Add("✅ PPU meta = 15 (alinhado aos Visuals)");
            else if (ppuLegacy)
                _report.Add("⚠ PPU meta = 28 (legado); runtime Visual força 15 via Sprite.Create");
            else
                _report.Add($"❌ PPU meta = {settings.spritePixelsPerUnit} (esperado 15 ou legado 28)");

            var pivot = settings.spriteAlignment == (int)SpriteAlignment.Custom
                ? settings.spritePivot
                : new Vector2(0.5f, 0f);
            bool pivotRuntime = Mathf.Abs(pivot.x - 0.5f) < 0.001f
                                && Mathf.Abs(pivot.y - CharacterPixelArtStandards.FootPivotY) < 0.001f;
            bool pivotLegacy = Mathf.Abs(pivot.x - 0.5f) < 0.001f && Mathf.Abs(pivot.y) < 0.001f;
            if (pivotRuntime)
                _report.Add("✅ Pivot meta = (0.5, 3/64)");
            else if (pivotLegacy)
                _report.Add("⚠ Pivot meta = (0.5, 0); runtime Visual usa (0.5, 3/64)");
            else
                _report.Add($"❌ Pivot meta = ({pivot.x}, {pivot.y})");
        }
        else
            _report.Add("❌ TextureImporter ausente");

        // Medidas do frame 0 (precisa readable)
        if (tex != null && tex.isReadable)
            MeasureIdle(tex, hero);
        else if (tex != null)
            _report.Add("⚠ Textura não readable — pulando medida de silhueta (use Import Tool)");

        _report.Add($"ℹ Runtime Visual: PPU {CharacterPixelArtStandards.RuntimePixelsPerUnit}, " +
                    $"FootPivot {CharacterPixelArtStandards.FootPivotY:0.####}, " +
                    $"localPosition.y {CharacterPixelArtStandards.VisualLocalOffsetY}");
        _report.Add("");
    }

    void MeasureIdle(Texture2D tex, HeroInfo hero)
    {
        int cell = CharacterPixelArtStandards.CellSize;
        // Frame 0 no topo-esquerdo; GetPixels origem = canto inferior esquerdo
        int yBl = tex.height - cell;
        Color32[] pixels;
        try
        {
            pixels = tex.GetPixels32();
        }
        catch
        {
            _report.Add("⚠ GetPixels32 falhou");
            return;
        }

        int minX = cell, maxX = -1, minY = cell, maxY = -1;
        // Converter para coords com y=0 no topo da célula
        for (int cy = 0; cy < cell; cy++)
        {
            for (int cx = 0; cx < cell; cx++)
            {
                int texX = cx;
                int texY = yBl + (cell - 1 - cy); // cy from top → tex Y
                int idx = texY * tex.width + texX;
                if (idx < 0 || idx >= pixels.Length)
                    continue;
                if (pixels[idx].a <= 10)
                    continue;
                if (cx < minX) minX = cx;
                if (cx > maxX) maxX = cx;
                if (cy < minY) minY = cy;
                if (cy > maxY) maxY = cy;
            }
        }

        if (maxX < 0)
        {
            _report.Add("❌ Frame 0 vazio");
            return;
        }

        int silH = maxY - minY + 1;
        int silW = maxX - minX + 1;
        int emptyBelow = (cell - 1) - maxY;

        bool heightOk = silH <= CharacterPixelArtStandards.TargetSilhouetteHeightMaxPx;
        bool heightIdeal = Mathf.Abs(silH - CharacterPixelArtStandards.TargetSilhouetteHeightPx) <= 3;
        if (heightIdeal)
            _report.Add($"✅ Altura visual idle ≈ {silH}px (alvo ~{CharacterPixelArtStandards.TargetSilhouetteHeightPx})");
        else if (heightOk)
            _report.Add($"⚠ Altura visual idle ≈ {silH}px (dentro do teto {CharacterPixelArtStandards.TargetSilhouetteHeightMaxPx}, longe do Guerreiro {CharacterPixelArtStandards.TargetSilhouetteHeightPx})");
        else
            _report.Add($"❌ Altura visual idle ≈ {silH}px (acima do teto {CharacterPixelArtStandards.TargetSilhouetteHeightMaxPx})");

        _report.Add($"ℹ BBox idle: {silW}×{silH} @ x {minX}–{maxX}, y {minY}–{maxY}");

        int deltaFeet = emptyBelow - CharacterPixelArtStandards.FootPaddingPixels;
        if (Mathf.Abs(deltaFeet) <= 1)
            _report.Add($"✅ Baseline pés: {emptyBelow}px vazios abaixo (contrato {CharacterPixelArtStandards.FootPaddingPixels})");
        else if (Mathf.Abs(deltaFeet) <= 3)
            _report.Add($"⚠ Baseline pés: {emptyBelow}px vazios (Δ {deltaFeet} vs {CharacterPixelArtStandards.FootPaddingPixels})");
        else
            _report.Add($"❌ Baseline pés: {emptyBelow}px vazios — flutuação ~{deltaFeet}px vs FootPivot (Guerreiro=3)");
    }

    void ValidateSceneScales()
    {
        _report.Add("━━ Escala na cena aberta ━━");
        var visuals = Object.FindObjectsByType<Transform>(FindObjectsSortMode.None);
        int found = 0;
        foreach (var t in visuals)
        {
            if (t.name != "GuerreiroPixel" && t.name != "MagoPixel" && t.name != "ArqueiroPixel")
                continue;
            found++;
            var s = t.localScale;
            bool scaleOk = Mathf.Abs(Mathf.Abs(s.x) - 1f) < 0.01f
                           && Mathf.Abs(s.y - 1f) < 0.01f
                           && Mathf.Abs(s.z - 1f) < 0.01f;
            Mark(scaleOk, $"{t.name} localScale {s} (esperado |x|=1,y=1,z=1)", false);

            float y = t.localPosition.y;
            if (Mathf.Abs(y - CharacterPixelArtStandards.VisualLocalOffsetY) < 0.01f)
                _report.Add($"✅ {t.name} localPosition.y = {y}");
            else
                _report.Add($"⚠ {t.name} localPosition.y = {y} (Visuals usam {CharacterPixelArtStandards.VisualLocalOffsetY})");
        }

        if (found == 0)
            _report.Add("ℹ Nenhum *Pixel na cena aberta (ok — valide em Play na T1).");
    }

    void Mark(bool ok, string message, bool warnOnly)
    {
        if (ok)
            _report.Add("✅ " + message);
        else if (warnOnly)
            _report.Add("⚠ " + message);
        else
            _report.Add("❌ " + message);
    }
}
#endif
