/// <summary>
/// Contrato de pixel art dos heróis jogáveis — valores medidos na Etapa 1
/// (<c>CharacterPixelArtAudit.md</c>) e usados em runtime pelos *Visuals.
/// Não altera gameplay; só documenta constantes compartilhadas por tools/debug.
/// </summary>
public static class CharacterPixelArtStandards
{
    public const int CellSize = 64;
    public const int SheetColumns = 7;
    public const int GuerreiroArqueiroRows = 3;
    public const int MagoRows = 4;

    /// <summary>PPU efetivo em GuerreiroVisual / MagoVisual / ArqueiroVisual.</summary>
    public const float RuntimePixelsPerUnit = 15f;

    /// <summary>Valor ainda presente nos .meta de Resources (sobrescrito no Sprite.Create).</summary>
    public const float MetaPixelsPerUnitLegacy = 28f;

    /// <summary>Pivot Y efetivo: 3px a partir da base da célula.</summary>
    public const float FootPivotY = 3f / 64f;
    public const int FootPaddingPixels = 3;

    public const float VisualLocalOffsetY = -0.5f;

    // --- Linhas-guia derivadas do idle do Guerreiro (y a partir do TOPO da célula) ---
    public const int GuideCrownY = 34;
    public const int GuideHeadBottomY = 46;
    public const int GuideShouldersY = 47;
    public const int GuideHandsY = 49;
    public const int GuideWaistY = 53;
    public const int GuideKneesY = 56;
    public const int GuideFeetY = 60;
    public const int GuideGroundY = 61; // linha do FootPivot (3px da base)

    public const int TargetSilhouetteHeightPx = 27;
    public const int TargetSilhouetteHeightMaxPx = 32;
    public const int TargetBodyWidthPx = 19;
    public const int TargetMaxWidthWithGearPx = 32;
    public const int OutlineThicknessPx = 1;
    public const int TargetColorCountMin = 8;
    public const int TargetColorCountMax = 16;

    /// <summary>
    /// Converte linha de pixel (y do topo da célula) → Y local do sprite
    /// com pivot no pé (FootPivotY) e PPU de runtime.
    /// </summary>
    public static float PixelRowToLocalY(int yFromTop)
    {
        float pxFromPivot = (CellSize - FootPaddingPixels) - yFromTop;
        return pxFromPivot / RuntimePixelsPerUnit;
    }
}
