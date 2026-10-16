using UnityEngine;

/// <summary>
/// Cenário da Fase I — Aetherion: Antes da Queda.
/// Carrega e monta as camadas de arte 16-bit com alta fidelidade visual,
/// profundidade de parallax, relação rica de luz e sombra, e ancoragem dos personagens.
/// </summary>
public static class GoldenAgeScenery
{
    const float Ppu = PixelArt.Ppu;

    static Sprite Load(string name)
    {
        return Resources.Load<Sprite>("GoldenAge/" + name);
    }

    public static void Build(StageData stage)
    {
        BuildLayers(stage);
        BuildGroundVisual(stage);
        BuildCityDecor(stage);
        BuildPlatforms(stage);
        AetherionAtmosphere.Attach(stage);
    }

    // =========================================================================
    // 1. CAMADAS DE PARALLAX (PROFUNDIDADE ATMOSFÉRICA)
    // =========================================================================

    static void BuildLayers(StageData stage)
    {
        // Fundo Panorâmico 16-bit unificado e contínuo:
        // Sol diurno com raios volumétricos, planetas com anéis, picos esmeralda,
        // acrópole com feixe divino e floresta alienígena com cogumelos gigantes.
        var skySprite = Load("sky");
        if (skySprite != null)
        {
            var skyGo = PixelArt.Place("CeuDourado", skySprite, new Vector3(0f, stage.GroundTop + 4.2f, 0f), -60, 0f);
            skyGo.AddComponent<T1Backdrop>().Setup(0.96f, 0.96f, 3.2f);
        }
    }

    // =========================================================================
    // 2. CHÃO VISUAL CONTÍNUO (MURO DE CANTARIA 3D, TERRA E GRAMA VIVA)
    // =========================================================================

    static void BuildGroundVisual(StageData stage)
    {
        var groundTile = Load("pavement");
        if (groundTile == null) return;

        // O topo visual alinha exatamente com stage.GroundTop (-2.35)
        float tileWidth = groundTile.rect.width / Ppu;
        float tileHeight = groundTile.rect.height / Ppu;
        float bottomY = stage.GroundTop - tileHeight;

        int widthTiles = Mathf.CeilToInt((stage.HalfWidth * 2f + 8f) / tileWidth);
        float startX = -stage.HalfWidth - 3f;

        var groundParent = new GameObject("ChaoVisual");
        for (int i = 0; i < widthTiles; i++)
        {
            float x = startX + i * tileWidth + tileWidth * 0.5f;
            var go = PixelArt.Place("BlocoChao_" + i, groundTile, new Vector3(x, bottomY, 0f), -6, 0f);
            go.transform.SetParent(groundParent.transform, true);
        }
    }

    // =========================================================================
    // 3. CENÁRIO DA FRENTE (PLANO JOGÁVEL, ARQUITETURA GRECO-ROMANA)
    // =========================================================================

    static void BuildCityDecor(StageData stage)
    {
        var decorRoot = new GameObject("CenarioFrente");

        // Templo Oeste completo: colunas caneladas, frontão com cornija, porta com luz quente e hera
        // Centro em x = -24.4, base no chão
        var westTempleSprite = Load("west_temple");
        if (westTempleSprite != null)
        {
            var westTemple = PixelArt.Place("TemploOeste", westTempleSprite, new Vector3(-24.4f, stage.GroundTop, 0f), 1, 0f);
            westTemple.transform.SetParent(decorRoot.transform, true);
        }

        // Templo Leste quebrado: frontão partido, coluna fraturada, hera abundante
        // Centro em x = 20.6, base no chão
        var eastTempleSprite = Load("east_temple");
        if (eastTempleSprite != null)
        {
            var eastTemple = PixelArt.Place("TemploLeste", eastTempleSprite, new Vector3(20.6f, stage.GroundTop, 0f), 1, 0f);
            eastTemple.transform.SetParent(decorRoot.transform, true);
        }

        // Ciprestes mediterrâneos
        var cypressSprite = Load("cypress");
        if (cypressSprite != null)
        {
            var cyp1 = PixelArt.Place("CipresteOeste", cypressSprite, new Vector3(-8.2f, stage.GroundTop, 0f), 2, 0f);
            cyp1.transform.SetParent(decorRoot.transform, true);
            var cyp2 = PixelArt.Place("CipresteLeste", cypressSprite, new Vector3(4.8f, stage.GroundTop, 0f), 2, 0f);
            cyp2.transform.SetParent(decorRoot.transform, true);
        }

        // Colunas soltas
        var intactColSprite = Load("column_intact");
        if (intactColSprite != null)
        {
            var col1 = PixelArt.Place("ColunaSolta_Intacta", intactColSprite, new Vector3(-10.4f, stage.GroundTop, 0f), 2, 0f);
            col1.transform.SetParent(decorRoot.transform, true);
        }
        var brokenColSprite = Load("column_broken");
        if (brokenColSprite != null)
        {
            var col2 = PixelArt.Place("ColunaSolta_Quebrada", brokenColSprite, new Vector3(6.5f, stage.GroundTop, 0f), 2, 0f);
            col2.transform.SetParent(decorRoot.transform, true);
        }

        // Fontes com água pulsante
        BuildPulsingFountain("FonteOeste", new Vector3(-4.6f, stage.GroundTop, 0f), decorRoot.transform);
        BuildPulsingFountain("FonteLeste", new Vector3(1.9f, stage.GroundTop, 0f), decorRoot.transform);

        // Estátua clássica heroica com um braço levantado à cabeça sobre pedestal lavrado
        var statueSprite = Load("statue");
        if (statueSprite != null)
        {
            var statue = PixelArt.Place("EstatuaHeroica", statueSprite, new Vector3(-1.35f, stage.GroundTop, 0f), 4, 0f);
            statue.transform.SetParent(decorRoot.transform, true);
        }

        // Arbustos com flores nas cores da imagem (rosa, amarelo, azul)
        var bushSprite = Load("bush");
        if (bushSprite != null)
        {
            float[] bushX = { -32.5f, -27.2f, -17.5f, -11.5f, -6.6f, -3.0f, 0.2f, 3.4f, 10.5f, 13.0f, 26.5f, 31.0f };
            for (int i = 0; i < bushX.Length; i++)
            {
                var go = PixelArt.Place("Arbusto_" + i, bushSprite, new Vector3(bushX[i], stage.GroundTop, 0f), 3, 0f);
                go.transform.SetParent(decorRoot.transform, true);
            }
        }
    }

    static void BuildPulsingFountain(string name, Vector3 pos, Transform parent)
    {
        var basinSprite = Load("fountain_basin");
        if (basinSprite == null) return;

        var root = PixelArt.Place(name, basinSprite, pos, 4, 0f);
        root.transform.SetParent(parent, true);

        var w0 = Load("fountain_water_0");
        var w1 = Load("fountain_water_1");
        var w2 = Load("fountain_water_2");

        if (w0 != null)
        {
            var waterGo = new GameObject("Agua");
            waterGo.transform.SetParent(root.transform, false);
            waterGo.transform.localPosition = Vector3.zero;
            var sr = waterGo.AddComponent<SpriteRenderer>();
            sr.sprite = w0;
            sr.sortingOrder = 5;

            var anim = waterGo.AddComponent<PulsingFountain>();
            anim.frames = new Sprite[] { w0, w1 != null ? w1 : w0, w2 != null ? w2 : w0 };
            anim.waterRenderer = sr;
        }
    }

    // =========================================================================
    // 4. PLATAFORMAS (ONE-WAY EFFECTOR COM ESTRUTURA RÚNICA E ILHAS)
    // =========================================================================

    static void BuildPlatforms(StageData stage)
    {
        var platRoot = new GameObject("Plataformas");

        // 1. Escada oeste de 5 degraus a partir de x = -29.2 (colisores one-way alinhados com os degraus do templo)
        float stairStepWidth = 1.32f;
        float stairSpacing = stairStepWidth * 0.82f;
        float[] westStairHeights = { 0.55f, 1.03f, 1.51f, 1.99f, 2.47f };

        for (int i = 0; i < westStairHeights.Length; i++)
        {
            float x = -29.2f + i * stairSpacing;
            BuildOneWayPlatform("DegrauOeste_" + i, x, westStairHeights[i], stairStepWidth, stage.GroundTop, null, 0f, platRoot.transform);
        }

        // 2. Laje em x = -23.05, altura 2.50, largura 3.3 (colisor da entrada do templo)
        BuildOneWayPlatform("LajeTemploOeste", -23.05f, 2.50f, 3.3f, stage.GroundTop, null, 0f, platRoot.transform);

        // 3. Quatro runas quadradas de 1x1 encostadas, começando em x = -20.9, mesma altura 2.50
        // Selo em losango dourado (Runa de Aetherion) em alto-relevo e hera pendente
        var runeSprite = Load("rune_block");
        for (int i = 0; i < 4; i++)
        {
            float blockCenterX = -20.9f + 0.5f + i * 1.0f;
            BuildOneWayPlatform("BlocoRuna_" + i, blockCenterX, 2.50f, 1.0f, stage.GroundTop, runeSprite, 1.0f, platRoot.transform);
        }

        // Colunas clássicas de sustentação da ponte de blocos rúnicos (sob os blocos)
        var colSprite = Load("column_intact");
        if (colSprite != null)
        {
            var colWest = PixelArt.Place("PilarPonteOeste", colSprite, new Vector3(-20.5f, stage.GroundTop, 0f), 2, 0f);
            colWest.transform.SetParent(platRoot.transform, true);
            var colEast = PixelArt.Place("PilarPonteLeste", colSprite, new Vector3(-17.3f, stage.GroundTop, 0f), 2, 0f);
            colEast.transform.SetParent(platRoot.transform, true);
        }

        // 4. Ilha de grama alta em x = -13.15, altura 3.85, largura 4.5
        var tallIslandSprite = Load("island_tall");
        float tallIslandH = 30f / Ppu;
        BuildOneWayPlatform("IlhaAlta", -13.15f, 3.85f, 4.5f, stage.GroundTop, tallIslandSprite, tallIslandH, platRoot.transform);
        BuildIslandShadow(-13.15f, stage.GroundTop, 4.5f, platRoot.transform);

        // 5. Ilha de grama mais baixa em x = -5.95, altura 2.05, largura 3.5
        var lowIslandSprite = Load("island_low");
        float lowIslandH = 26f / Ppu;
        BuildOneWayPlatform("IlhaBaixa", -5.95f, 2.05f, 3.5f, stage.GroundTop, lowIslandSprite, lowIslandH, platRoot.transform);
        BuildIslandShadow(-5.95f, stage.GroundTop, 3.5f, platRoot.transform);

        // 6. Praça aberta para combate entre x = -4 e 13 (sem plataforma obstruindo)

        // 7. Escada leste de 4 degraus a partir de x = 14, alturas 0.55, 1.05, 1.55, 2.05
        float[] eastStairHeights = { 0.55f, 1.05f, 1.55f, 2.05f };
        for (int i = 0; i < eastStairHeights.Length; i++)
        {
            float x = 14.0f + i * stairSpacing;
            BuildOneWayPlatform("DegrauLeste_" + i, x, eastStairHeights[i], stairStepWidth, stage.GroundTop, null, 0f, platRoot.transform);
        }

        // 8. Terraço em x = 19.9, altura 2.48, largura 5
        BuildOneWayPlatform("TerracoLeste", 19.9f, 2.48f, 5.0f, stage.GroundTop, null, 0f, platRoot.transform);

        // 9. Ilha final em x = 25.85, altura 3.95, largura 3.3
        BuildOneWayPlatform("IlhaFinal", 25.85f, 3.95f, 3.3f, stage.GroundTop, lowIslandSprite, lowIslandH, platRoot.transform);
        BuildIslandShadow(25.85f, stage.GroundTop, 3.3f, platRoot.transform);
    }

    static GameObject BuildOneWayPlatform(string name, float x, float heightFromGround, float width, float groundTop, Sprite visual, float visualHeight, Transform parent)
    {
        const float thickness = 0.38f;
        float top = groundTop + heightFromGround;
        var center = new Vector3(x, top - thickness * 0.5f, 0f);

        var go = new GameObject(name);
        go.transform.position = center;
        go.transform.SetParent(parent, true);

        var collider = go.AddComponent<BoxCollider2D>();
        collider.size = new Vector2(width, thickness);
        collider.usedByEffector = true;

        var effector = go.AddComponent<PlatformEffector2D>();
        effector.useOneWay = true;
        effector.surfaceArc = 165f;

        if (visual != null)
        {
            var visGo = new GameObject("Visual");
            visGo.transform.SetParent(go.transform, false);
            float sprW = visual.rect.width / Ppu;
            float sprH = visual.rect.height / Ppu;
            float scaleX = width / sprW;
            float scaleY = (visualHeight > 0f) ? (visualHeight / sprH) : scaleX;
            visGo.transform.localScale = new Vector3(scaleX, scaleY, 1f);
            visGo.transform.localPosition = new Vector3(0f, thickness * 0.5f - sprH * scaleY, 0f);
            var sr = visGo.AddComponent<SpriteRenderer>();
            sr.sprite = visual;
            sr.sortingOrder = 6;
        }

        return go;
    }

    static void BuildIslandShadow(float x, float groundTop, float width, Transform parent)
    {
        var shadowSprite = Load("shadow_island");
        if (shadowSprite == null) return;
        var shadowGo = PixelArt.Place("SombraIlha", shadowSprite, new Vector3(x, groundTop + 0.02f, 0f), -5, 0f);
        shadowGo.transform.SetParent(parent, true);
    }
}

/// <summary>
/// Camada de parallax que também deriva suavemente ao longo do tempo (nuvens soltas).
/// </summary>
public class GoldenAgeDriftLayer : MonoBehaviour
{
    float _factor;
    Vector3 _origin;
    float _driftSpeed;
    float _drift;

    public void Setup(float factor, float driftSpeed)
    {
        _factor = factor;
        _origin = transform.position;
        _driftSpeed = driftSpeed;
    }

    void LateUpdate()
    {
        var camera = Camera.main;
        if (camera == null)
            return;

        _drift += _driftSpeed * Time.deltaTime;
        var cam = camera.transform.position;
        transform.position = new Vector3(
            _origin.x + _drift + cam.x * _factor,
            _origin.y + cam.y * _factor * 0.18f,
            _origin.z);
    }
}

/// <summary>
/// Anima os jatos de água em cascata da fonte clássica.
/// </summary>
public class PulsingFountain : MonoBehaviour
{
    public SpriteRenderer waterRenderer;
    public Sprite[] frames;
    float _timer;
    int _index;

    void Update()
    {
        if (frames == null || frames.Length == 0 || waterRenderer == null)
            return;

        _timer += Time.deltaTime;
        if (_timer >= 0.14f)
        {
            _timer = 0f;
            _index = (_index + 1) % frames.Length;
            waterRenderer.sprite = frames[_index];
        }
    }
}
