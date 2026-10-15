using UnityEngine;

/// <summary>
/// Cenário da Fase I — Aetherion: Antes da Queda.
/// Estética clássica inspirada na era de ouro de Aetherion sob a luz do dia:
/// Céu azul vívido com raios solares, montanhas verde-azuladas com templo no pico,
/// floresta densa fechando os vãos, colunatas distantes, templos em mármore com hera,
/// fontes com água pulsante, estátua heroica, ciprestes e plataformas de grama/runas.
/// </summary>
public static class GoldenAgeScenery
{
    const float Ppu = PixelArt.Ppu;

    public static void Build(StageData stage)
    {
        BuildLayers(stage);
        BuildGroundVisual(stage);
        BuildCityDecor(stage);
        BuildPlatforms(stage);
    }

    // =========================================================================
    // 1. CAMADAS DE PARALLAX
    // =========================================================================

    static void BuildLayers(StageData stage)
    {
        // Camada 1: Céu (fator ~0.98, T1Backdrop, sorting -60)
        var skySprite = CreateSkySprite(640, 260);
        var skyGo = PixelArt.Place("CeuDourado", skySprite, Vector3.zero, -60, 0f);
        skyGo.AddComponent<T1Backdrop>().Setup(0.98f, 0.98f, -2.4f);

        // Camada 2: Nuvens Soltas (fator ~0.90, deriva devagar, sorting -55)
        var cloudSprite = CreateLooseCloudsSprite(680, 110);
        var cloudGo = PixelArt.Place("NuvensSoltas", cloudSprite, new Vector3(0f, 2.2f, 0f), -55, 0f);
        cloudGo.AddComponent<GoldenAgeDriftLayer>().Setup(0.90f, 0.45f);

        // Camada 3: Montanhas com Templo no Pico (fator ~0.76, sorting -48)
        var mountainSprite = CreateMountainSprite(800, 170);
        var mountainGo = PixelArt.Place("Montanhas", mountainSprite, new Vector3(0f, stage.GroundTop + 0.2f, 0f), -48, 0.76f);

        // Camada 4: Floresta (duas fileiras, fator ~0.52, sorting -40)
        var forestSprite = CreateForestSprite(840, 130);
        PixelArt.Place("Floresta", forestSprite, new Vector3(0f, stage.GroundTop - 0.15f, 0f), -40, 0.52f);

        // Camada 5: Colunata Distante (fator ~0.30, sorting -30)
        var colonnadeSprite = CreateColonnadeSprite(820, 90);
        PixelArt.Place("ColunataDistante", colonnadeSprite, new Vector3(0f, stage.GroundTop - 0.1f, 0f), -30, 0.30f);
    }

    // =========================================================================
    // 2. CHÃO VISUAL CONTÍNUO
    // =========================================================================

    static void BuildGroundVisual(StageData stage)
    {
        // Chão contínuo em toda a fase (HalfWidth 36).
        // Muro de pedra clara com juntas, faixa de terra e tampa de grama com fio verde-claro.
        // O topo visual coincide exatamente com stage.GroundTop (-2.35).
        int widthTiles = Mathf.CeilToInt((stage.HalfWidth * 2f + 8f) / 4f);
        float startX = -stage.HalfWidth - 3f;
        var groundTile = CreateGroundTileSprite(64, 56);
        float visualHeight = 56f / Ppu; // 3.5 unidades
        float bottomY = stage.GroundTop - visualHeight;

        var groundParent = new GameObject("ChaoVisual");
        for (int i = 0; i < widthTiles; i++)
        {
            float x = startX + i * 4f + 2f;
            var go = PixelArt.Place("BlocoChao_" + i, groundTile, new Vector3(x, bottomY, 0f), -6, 0f);
            go.transform.SetParent(groundParent.transform, true);
        }
    }

    // =========================================================================
    // 3. CENÁRIO DA FRENTE (PLANO JOGÁVEL, SORTING ORDERS DEFINIDOS)
    // =========================================================================

    static void BuildCityDecor(StageData stage)
    {
        var decorRoot = new GameObject("CenarioFrente");

        // Templo Oeste (inteiro): colunas, frontão, porta com luz quente e hera.
        // Centro perto de x = -24.4, base no chão.
        var westTempleSprite = CreateWestTempleSprite(256, 150);
        var westTemple = PixelArt.Place("TemploOeste", westTempleSprite, new Vector3(-24.4f, stage.GroundTop, 0f), 1, 0f);
        westTemple.transform.SetParent(decorRoot.transform, true);

        // Templo Leste (parcialmente quebrado): frontão incompleto, coluna curta, mais hera.
        // Centro perto de x = 20.6, base no chão.
        var eastTempleSprite = CreateEastTempleSprite(220, 150);
        var eastTemple = PixelArt.Place("TemploLeste", eastTempleSprite, new Vector3(20.6f, stage.GroundTop, 0f), 1, 0f);
        eastTemple.transform.SetParent(decorRoot.transform, true);

        // Elementos da praça entre os templos:
        // Cipreste oeste
        var cypressSprite = CreateCypressSprite(36, 100);
        var cyp1 = PixelArt.Place("CipresteOeste", cypressSprite, new Vector3(-8.2f, stage.GroundTop, 0f), 2, 0f);
        cyp1.transform.SetParent(decorRoot.transform, true);

        // Colunas soltas
        var intactColSprite = CreateColumnSprite(22, 90, false);
        var brokenColSprite = CreateColumnSprite(22, 60, true);
        var col1 = PixelArt.Place("ColunaSolta_Intacta", intactColSprite, new Vector3(-10.4f, stage.GroundTop, 0f), 2, 0f);
        col1.transform.SetParent(decorRoot.transform, true);
        var col2 = PixelArt.Place("ColunaSolta_Quebrada", brokenColSprite, new Vector3(6.5f, stage.GroundTop, 0f), 2, 0f);
        col2.transform.SetParent(decorRoot.transform, true);

        // Fonte Oeste (com água que pulsa)
        BuildPulsingFountain("FonteOeste", new Vector3(-4.6f, stage.GroundTop, 0f), decorRoot.transform);

        // Estátua clássica: com um braço levantado até a cabeça
        var statueSprite = CreateStatueSprite(42, 80);
        var statue = PixelArt.Place("EstatuaHeroica", statueSprite, new Vector3(-1.35f, stage.GroundTop, 0f), 4, 0f);
        statue.transform.SetParent(decorRoot.transform, true);

        // Fonte Leste (com água que pulsa)
        BuildPulsingFountain("FonteLeste", new Vector3(1.9f, stage.GroundTop, 0f), decorRoot.transform);

        // Cipreste leste
        var cyp2 = PixelArt.Place("CipresteLeste", cypressSprite, new Vector3(4.8f, stage.GroundTop, 0f), 2, 0f);
        cyp2.transform.SetParent(decorRoot.transform, true);

        // Pedestal / plinto estilo clássico perto da coluna leste (referência verde do concept)
        var plinthSprite = CreatePlinthSprite(28, 48);
        var plinth = PixelArt.Place("PlintoClassico", plinthSprite, new Vector3(8.8f, stage.GroundTop, 0f), 2, 0f);
        plinth.transform.SetParent(decorRoot.transform, true);

        // Flores e arbustos no chão (cores: rosa, amarelo, azul)
        BuildFloraProps(stage, decorRoot.transform);
    }

    static void BuildFloraProps(StageData stage, Transform parent)
    {
        float ground = stage.GroundTop;
        var bushLarge = CreateBushSprite(46, 26, true);
        var bushSmall = CreateBushSprite(32, 20, false);

        float[] bushX = { -32.5f, -27.2f, -17.5f, -11.5f, -6.6f, -3.0f, 0.2f, 3.4f, 10.5f, 13.0f, 26.5f, 31.0f };
        for (int i = 0; i < bushX.Length; i++)
        {
            var sprite = (i % 2 == 0) ? bushLarge : bushSmall;
            var go = PixelArt.Place("Arbusto_" + i, sprite, new Vector3(bushX[i], ground, 0f), 3, 0f);
            go.transform.SetParent(parent, true);
        }
    }

    static void BuildPulsingFountain(string name, Vector3 pos, Transform parent)
    {
        var basinSprite = CreateFountainBasinSprite(52, 42);
        var root = PixelArt.Place(name, basinSprite, pos, 4, 0f);
        root.transform.SetParent(parent, true);

        // Sprite de água animada em cima da bacia
        var water1 = CreateFountainWaterSprite(52, 42, 0);
        var water2 = CreateFountainWaterSprite(52, 42, 1);
        var water3 = CreateFountainWaterSprite(52, 42, 2);

        var waterGo = new GameObject("Agua");
        waterGo.transform.SetParent(root.transform, false);
        waterGo.transform.localPosition = Vector3.zero;
        var sr = waterGo.AddComponent<SpriteRenderer>();
        sr.sprite = water1;
        sr.sortingOrder = 5;

        var anim = waterGo.AddComponent<PulsingFountain>();
        anim.frames = new Sprite[] { water1, water2, water3 };
        anim.waterRenderer = sr;
    }

    // =========================================================================
    // 4. PLATAFORMAS (ONE-WAY EFFECTOR, CAMINHO OESTE -> LESTE)
    // =========================================================================

    static void BuildPlatforms(StageData stage)
    {
        var platRoot = new GameObject("Plataformas");

        // 1. Escada oeste de 5 degraus a partir de x = -29.2, largura 1.32, espaçamento 1.32 * 0.82,
        // alturas 0.55, 1.03, 1.51, 1.99, 2.47. Degrau de pedra com tampa de grama.
        float stairStepWidth = 1.32f;
        float stairSpacing = stairStepWidth * 0.82f;
        float[] westStairHeights = { 0.55f, 1.03f, 1.51f, 1.99f, 2.47f };
        var stepSprite = CreateStairStepSprite(Mathf.RoundToInt(stairStepWidth * Ppu), 14);

        for (int i = 0; i < westStairHeights.Length; i++)
        {
            float x = -29.2f + i * stairSpacing;
            BuildOneWayPlatform("DegrauOeste_" + i, x, westStairHeights[i], stairStepWidth, stage.GroundTop, stepSprite, 14f / Ppu, platRoot.transform);
        }

        // 2. Laje em x = -23.05, altura 2.50, largura 3.3, na frente da porta do templo oeste.
        var lajeSprite = CreateLajeSprite(Mathf.RoundToInt(3.3f * Ppu), 14);
        BuildOneWayPlatform("LajeTemploOeste", -23.05f, 2.50f, 3.3f, stage.GroundTop, lajeSprite, 14f / Ppu, platRoot.transform);

        // 3. Quatro runas quadradas de 1x1 encostadas, começando em x = -20.9, mesma altura 2.50.
        // Saem da fachada do templo. Mármore com selo em losango dourado e hera curta embaixo.
        var runeSprite = CreateRuneBlockSprite();
        for (int i = 0; i < 4; i++)
        {
            float blockCenterX = -20.9f + 0.5f + i * 1.0f;
            BuildOneWayPlatform("BlocoRuna_" + i, blockCenterX, 2.50f, 1.0f, stage.GroundTop, runeSprite, 1.0f, platRoot.transform);
        }

        // 4. Ilha de grama alta em x = -13.15, altura 3.85, largura 4.5.
        var tallIslandSprite = CreateFloatingIslandSprite(Mathf.RoundToInt(4.5f * Ppu), 30);
        BuildOneWayPlatform("IlhaAlta", -13.15f, 3.85f, 4.5f, stage.GroundTop, tallIslandSprite, 30f / Ppu, platRoot.transform);
        BuildIslandShadow(-13.15f, stage.GroundTop, 4.2f, platRoot.transform);

        // 5. Ilha de grama mais baixa em x = -5.95, altura 2.05, largura 3.5.
        var lowIslandSprite = CreateFloatingIslandSprite(Mathf.RoundToInt(3.5f * Ppu), 26);
        BuildOneWayPlatform("IlhaBaixa", -5.95f, 2.05f, 3.5f, stage.GroundTop, lowIslandSprite, 26f / Ppu, platRoot.transform);
        BuildIslandShadow(-5.95f, stage.GroundTop, 3.3f, platRoot.transform);

        // 6. Praça aberta (estátua e fontes) sem plataforma no meio, para o combate. (x = -4 até 13)

        // 7. Escada leste de 4 degraus a partir de x = 14, alturas 0.55, 1.05, 1.55, 2.05.
        float[] eastStairHeights = { 0.55f, 1.05f, 1.55f, 2.05f };
        for (int i = 0; i < eastStairHeights.Length; i++)
        {
            float x = 14.0f + i * stairSpacing;
            BuildOneWayPlatform("DegrauLeste_" + i, x, eastStairHeights[i], stairStepWidth, stage.GroundTop, stepSprite, 14f / Ppu, platRoot.transform);
        }

        // 8. Terraço em x = 19.9, altura 2.48, largura 5, na frente do templo leste.
        var terraceSprite = CreateLajeSprite(Mathf.RoundToInt(5.0f * Ppu), 14);
        BuildOneWayPlatform("TerracoLeste", 19.9f, 2.48f, 5.0f, stage.GroundTop, terraceSprite, 14f / Ppu, platRoot.transform);

        // 9. Ilha final em x = 25.85, altura 3.95, largura 3.3.
        var finalIslandSprite = CreateFloatingIslandSprite(Mathf.RoundToInt(3.3f * Ppu), 26);
        BuildOneWayPlatform("IlhaFinal", 25.85f, 3.95f, 3.3f, stage.GroundTop, finalIslandSprite, 26f / Ppu, platRoot.transform);
        BuildIslandShadow(25.85f, stage.GroundTop, 3.1f, platRoot.transform);
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
            // Pivot é bottom-center. Colocar em (0, thickness*0.5f - visualHeight, 0)
            // faz com que o topo visual coincida exatamente com o topo do colisor.
            visGo.transform.localPosition = new Vector3(0f, thickness * 0.5f - visualHeight, 0f);
            var sr = visGo.AddComponent<SpriteRenderer>();
            sr.sprite = visual;
            sr.sortingOrder = 6;
        }

        return go;
    }

    static void BuildIslandShadow(float x, float groundTop, float width, Transform parent)
    {
        var shadowSprite = CreateOvalShadowSprite(Mathf.RoundToInt(width * Ppu), 10);
        var shadowGo = PixelArt.Place("SombraIlha", shadowSprite, new Vector3(x, groundTop + 0.02f, 0f), -5, 0f);
        shadowGo.transform.SetParent(parent, true);
    }

    // =========================================================================
    // 5. GERADORES DE PIXEL ART PROCEDURAL
    // =========================================================================

    static Sprite CreateSkySprite(int w, int h)
    {
        var pixels = PixelArt.Clear(w, h);

        // Gradiente vertical do céu azul ao horizonte levemente verde
        for (int y = 0; y < h; y++)
        {
            float t = (float)y / (h - 1);
            Color skyCol;
            if (t > 0.45f)
            {
                float mt = (t - 0.45f) / 0.55f;
                // Azul diurno clássico ao topo vibrante
                skyCol = Color.Lerp(new Color(0.48f, 0.73f, 0.94f), new Color(0.24f, 0.56f, 0.88f), mt);
            }
            else
            {
                float lt = t / 0.45f;
                // Horizonte com leve transição verde-água / ciano pálido
                skyCol = Color.Lerp(new Color(0.72f, 0.88f, 0.86f), new Color(0.48f, 0.73f, 0.94f), lt);
            }

            for (int x = 0; x < w; x++)
            {
                pixels[y * w + x] = skyCol;
            }
        }

        // Sol no canto superior direito
        int sunX = (int)(w * 0.80f);
        int sunY = (int)(h * 0.78f);
        int sunRadius = 16;
        int haloRadius = 48;

        for (int y = sunY - haloRadius; y <= sunY + haloRadius; y++)
        {
            for (int x = sunX - haloRadius; x <= sunX + haloRadius; x++)
            {
                if (x < 0 || y < 0 || x >= w || y >= h) continue;
                float d = Mathf.Sqrt((x - sunX) * (x - sunX) + (y - sunY) * (y - sunY));
                if (d <= sunRadius)
                {
                    PixelArt.Plot(pixels, w, h, x, y, new Color(1.0f, 0.99f, 0.92f, 1f));
                }
                else if (d <= haloRadius)
                {
                    float factor = 1f - (d - sunRadius) / (haloRadius - sunRadius);
                    PixelArt.Plot(pixels, w, h, x, y, new Color(1.0f, 0.95f, 0.75f, factor * 0.42f));
                }
            }
        }

        // Raios solares suaves diagonais vindos do sol para baixo/esquerda
        for (int i = 0; i < 7; i++)
        {
            float angleOffset = -0.48f - i * 0.12f;
            float rayWidth = 14f + (i % 3) * 6f;
            for (int step = 0; step < 360; step++)
            {
                float dist = step * 1.4f;
                float rx = sunX + Mathf.Cos(angleOffset) * dist;
                float ry = sunY + Mathf.Sin(angleOffset) * dist;
                int irx = Mathf.RoundToInt(rx);
                int iry = Mathf.RoundToInt(ry);

                for (int rw = -(int)(rayWidth * 0.5f); rw <= (int)(rayWidth * 0.5f); rw++)
                {
                    float fade = (1f - (float)step / 360f) * 0.11f;
                    PixelArt.Plot(pixels, w, h, irx + rw, iry, new Color(1f, 1f, 0.88f, fade));
                }
            }
        }

        // Nuvens suaves pintadas no fundo do céu
        PaintWispyCloud(pixels, w, h, 90, 160, 130, 26);
        PaintWispyCloud(pixels, w, h, 280, 140, 160, 30);
        PaintWispyCloud(pixels, w, h, 480, 150, 140, 24);

        return PixelArt.Make(w, h, pixels);
    }

    static void PaintWispyCloud(Color[] pixels, int w, int h, int cx, int cy, int cloudW, int cloudH)
    {
        for (int y = cy - cloudH / 2; y <= cy + cloudH / 2; y++)
        {
            for (int x = cx - cloudW / 2; x <= cx + cloudW / 2; x++)
            {
                if (x < 0 || y < 0 || x >= w || y >= h) continue;
                float nx = (float)(x - cx) / (cloudW * 0.5f);
                float ny = (float)(y - cy) / (cloudH * 0.5f);
                float dist = nx * nx + ny * ny;
                if (dist <= 1f)
                {
                    float a = (1f - dist) * 0.38f;
                    PixelArt.Plot(pixels, w, h, x, y, new Color(1f, 1f, 1f, a));
                }
            }
        }
    }

    static Sprite CreateLooseCloudsSprite(int w, int h)
    {
        var pixels = PixelArt.Clear(w, h);
        Color cloudTop = new Color(1.0f, 1.0f, 1.0f, 0.95f);
        Color cloudMid = new Color(0.92f, 0.94f, 0.98f, 0.92f);
        Color cloudShade = new Color(0.70f, 0.78f, 0.88f, 0.90f);

        int[] cloudXs = { 70, 230, 420, 590 };
        int[] cloudYs = { 46, 62, 50, 58 };

        for (int c = 0; c < cloudXs.Length; c++)
        {
            int cx = cloudXs[c];
            int cy = cloudYs[c];
            DrawPuffyCloud(pixels, w, h, cx, cy, cloudTop, cloudMid, cloudShade);
        }

        return PixelArt.Make(w, h, pixels);
    }

    static void DrawPuffyCloud(Color[] pixels, int w, int h, int cx, int cy, Color top, Color mid, Color shade)
    {
        int[] lobesX = { -30, -12, 10, 32, -20, 18 };
        int[] lobesY = { 0, 14, 12, -2, -8, -8 };
        int[] radii = { 18, 22, 20, 16, 17, 18 };

        for (int l = 0; l < lobesX.Length; l++)
        {
            int lx = cx + lobesX[l];
            int ly = cy + lobesY[l];
            int r = radii[l];

            for (int y = ly - r; y <= ly + r; y++)
            {
                for (int x = lx - r; x <= lx + r; x++)
                {
                    if (x < 0 || y < 0 || x >= w || y >= h) continue;
                    float d = Mathf.Sqrt((x - lx) * (x - lx) + (y - ly) * (y - ly));
                    if (d <= r)
                    {
                        float verticalFactor = (float)(y - (ly - r)) / (r * 2f);
                        Color c = verticalFactor > 0.65f ? top : (verticalFactor > 0.28f ? mid : shade);
                        PixelArt.Plot(pixels, w, h, x, y, c);
                    }
                }
            }
        }
    }

    static Sprite CreateMountainSprite(int w, int h)
    {
        var pixels = PixelArt.Clear(w, h);

        Color mtnLight = new Color(0.55f, 0.68f, 0.72f);
        Color mtnMid = new Color(0.38f, 0.52f, 0.56f);
        Color mtnDark = new Color(0.24f, 0.38f, 0.44f);
        Color mtnCrest = new Color(0.70f, 0.80f, 0.84f);

        // Cordilheira em verde-azulado
        // O pico alto com o templo fica em x = 440 (lado direito da tela ao iniciar a fase)
        int peakTempleX = 440;
        int peakTempleY = 115;

        for (int x = 0; x < w; x++)
        {
            float nx = (float)x / w;
            float profile = Mathf.Sin(nx * 8f) * 18f
                          + Mathf.Cos(nx * 14f) * 12f
                          + Mathf.Sin(nx * 3f + 1f) * 35f + 48f;

            // Formato escarpado do pico do templo
            float distToPeak = Mathf.Abs(x - peakTempleX);
            if (distToPeak < 65)
            {
                float peakElev = (1f - distToPeak / 65f) * 58f;
                profile = Mathf.Max(profile, peakTempleY + peakElev - 58f);
            }

            int topY = Mathf.Clamp(Mathf.RoundToInt(profile), 15, h - 35);

            for (int y = 0; y <= topY; y++)
            {
                float lit = Mathf.Sin((x + y * 0.5f) * 0.12f);
                Color c = lit > 0.25f ? mtnLight : (lit > -0.2f ? mtnMid : mtnDark);
                if (y >= topY - 2) c = mtnCrest;
                PixelArt.Plot(pixels, w, h, x, y, c);
            }
        }

        // Templo Pálido Pequeno no Pico (Eco do templo da frente)
        DrawPeakTemple(pixels, w, h, peakTempleX, peakTempleY);

        return PixelArt.Make(w, h, pixels);
    }

    static void DrawPeakTemple(Color[] pixels, int w, int h, int cx, int baseY)
    {
        Color marbleLight = new Color(0.95f, 0.95f, 0.96f);
        Color marbleShadow = new Color(0.68f, 0.72f, 0.76f);
        Color doorway = new Color(0.28f, 0.36f, 0.40f);
        Color sunBeam = new Color(1.0f, 1.0f, 0.9f, 0.18f);

        int templeW = 46;
        int templeH = 26;
        int startX = cx - templeW / 2;

        // Base de degraus
        PixelArt.Fill(pixels, w, h, startX - 2, baseY, templeW + 4, 3, marbleShadow);
        PixelArt.Fill(pixels, w, h, startX - 1, baseY + 1, templeW + 2, 2, marbleLight);

        // Colunas e vão da porta
        int colCount = 7;
        int colW = 2;
        int spacing = 5;
        int colBaseX = startX + 3;
        int colH = 14;

        // Fundo escuro atrás das colunas
        PixelArt.Fill(pixels, w, h, colBaseX, baseY + 3, templeW - 6, colH, doorway);

        for (int c = 0; c < colCount; c++)
        {
            int x = colBaseX + c * (colW + spacing);
            PixelArt.Fill(pixels, w, h, x, baseY + 3, colW, colH, marbleLight);
            PixelArt.Plot(pixels, w, h, x + colW - 1, baseY + 3, marbleShadow);
        }

        // Entablamento (viga horizontal sobre as colunas)
        int entabY = baseY + 3 + colH;
        PixelArt.Fill(pixels, w, h, startX - 1, entabY, templeW + 2, 3, marbleLight);
        PixelArt.Fill(pixels, w, h, startX - 2, entabY + 2, templeW + 4, 1, marbleShadow);

        // Frontão triangular
        int pedimentH = 8;
        for (int ph = 0; ph < pedimentH; ph++)
        {
            int pw = templeW - ph * 6;
            if (pw <= 2) break;
            int px = cx - pw / 2;
            PixelArt.Fill(pixels, w, h, px, entabY + 3 + ph, pw, 1, ph == pedimentH - 1 ? marbleLight : marbleLight);
        }

        // Raio de sol banhando o templo no pico
        for (int y = baseY - 8; y < baseY + templeH + 18; y++)
        {
            for (int x = cx - 20; x < cx + 30; x++)
            {
                PixelArt.Plot(pixels, w, h, x, y, sunBeam);
            }
        }
    }

    static Sprite CreateForestSprite(int w, int h)
    {
        var pixels = PixelArt.Clear(w, h);

        // Duas fileiras de árvores:
        // Fileira de trás: mais baixa e azulada
        Color backDark = new Color(0.14f, 0.28f, 0.26f);
        Color backLight = new Color(0.22f, 0.42f, 0.38f);

        for (int x = 0; x < w; x += 18)
        {
            int canopyR = 14 + (x % 5);
            int cy = 42 + (x % 4) * 3;
            DrawTreeCanopy(pixels, w, h, x, cy, canopyR, backLight, backDark);
        }

        // Fileira da frente: mais alta e verde vibrante, fecha o vão entre as fachadas
        Color frontLight = new Color(0.28f, 0.60f, 0.32f);
        Color frontMid = new Color(0.16f, 0.44f, 0.22f);
        Color frontDark = new Color(0.10f, 0.26f, 0.14f);
        Color trunkCol = new Color(0.28f, 0.18f, 0.12f);

        for (int x = 0; x < w; x += 22)
        {
            int canopyR = 19 + (x % 7);
            int cy = 60 + (x % 6) * 3;
            // Tronco
            PixelArt.Fill(pixels, w, h, x - 2, 0, 4, cy, trunkCol);
            DrawTreeCanopy(pixels, w, h, x, cy, canopyR, frontLight, frontMid, frontDark);
        }

        return PixelArt.Make(w, h, pixels);
    }

    static void DrawTreeCanopy(Color[] pixels, int w, int h, int cx, int cy, int r, Color light, Color dark)
    {
        for (int y = cy - r; y <= cy + r; y++)
        {
            for (int x = cx - r; x <= cx + r; x++)
            {
                if (x < 0 || y < 0 || x >= w || y >= h) continue;
                float d = Mathf.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy));
                if (d <= r)
                {
                    Color c = (y > cy - r * 0.2f && x < cx + r * 0.4f) ? light : dark;
                    PixelArt.Plot(pixels, w, h, x, y, c);
                }
            }
        }
    }

    static void DrawTreeCanopy(Color[] pixels, int w, int h, int cx, int cy, int r, Color light, Color mid, Color dark)
    {
        for (int y = cy - r; y <= cy + r; y++)
        {
            for (int x = cx - r; x <= cx + r; x++)
            {
                if (x < 0 || y < 0 || x >= w || y >= h) continue;
                float d = Mathf.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy));
                if (d <= r)
                {
                    float shade = (float)(y - (cy - r)) / (r * 2f);
                    Color c = shade > 0.62f ? light : (shade > 0.28f ? mid : dark);
                    PixelArt.Plot(pixels, w, h, x, y, c);
                }
            }
        }
    }

    static Sprite CreateColonnadeSprite(int w, int h)
    {
        var pixels = PixelArt.Clear(w, h);

        Color marble = new Color(0.88f, 0.88f, 0.86f);
        Color shade = new Color(0.64f, 0.65f, 0.66f);
        Color vine = new Color(0.24f, 0.48f, 0.22f);

        // Colunas pálidas, baixas, com falhas e espaçamento espaçado para não virar muro
        int[] colX = { 35, 75, 120, 190, 240, 310, 370, 440, 500, 560, 630, 710, 770 };

        for (int i = 0; i < colX.Length; i++)
        {
            int cx = colX[i];
            int colH = 34 + (i % 3) * 8;
            bool broken = (i % 4 == 1);
            if (broken) colH = 20;

            int colW = 6;
            // Base
            PixelArt.Fill(pixels, w, h, cx - 1, 0, colW + 2, 4, marble);
            // Fuste com ranhura
            PixelArt.Fill(pixels, w, h, cx, 4, colW, colH, marble);
            PixelArt.Fill(pixels, w, h, cx + colW - 2, 4, 2, colH, shade);

            if (!broken)
            {
                // Capitel
                PixelArt.Fill(pixels, w, h, cx - 2, 4 + colH, colW + 4, 4, marble);
            }

            // Toque de hera
            if (i % 2 == 0)
            {
                PixelArt.Plot(pixels, w, h, cx - 1, 6, vine);
                PixelArt.Plot(pixels, w, h, cx, 7, vine);
                PixelArt.Plot(pixels, w, h, cx - 1, 8, vine);
            }
        }

        return PixelArt.Make(w, h, pixels);
    }

    // =========================================================================
    // 6. PEÇAS DO CHÃO E ESTRUTURAS
    // =========================================================================

    static Sprite CreateGroundTileSprite(int w, int h)
    {
        var pixels = PixelArt.Clear(w, h);

        // Visual:
        // Topo: Fio verde-claro (2px)
        // Faixa de grama (8px) com franja irregular
        // Faixa de terra fértil (8px)
        // Muro de pedra clara com juntas de argamassa (restante)
        Color wire = new Color(0.74f, 0.96f, 0.42f);
        Color grass = new Color(0.36f, 0.68f, 0.18f);
        Color grassDark = new Color(0.24f, 0.48f, 0.12f);
        Color soil = new Color(0.44f, 0.30f, 0.16f);
        Color soilDark = new Color(0.32f, 0.20f, 0.10f);

        Color stoneFace = new Color(0.86f, 0.83f, 0.76f);
        Color stoneLight = new Color(0.94f, 0.92f, 0.86f);
        Color stoneShadow = new Color(0.72f, 0.68f, 0.60f);
        Color mortar = new Color(0.48f, 0.44f, 0.38f);

        int groundY = h - 1;

        // Fio verde-claro no topo
        for (int x = 0; x < w; x++)
        {
            PixelArt.Plot(pixels, w, h, x, groundY, wire);
            PixelArt.Plot(pixels, w, h, x, groundY - 1, wire);
        }

        // Grama
        for (int y = groundY - 8; y <= groundY - 2; y++)
        {
            for (int x = 0; x < w; x++)
            {
                Color c = (y > groundY - 5) ? grass : grassDark;
                PixelArt.Plot(pixels, w, h, x, y, c);
            }
        }
        // Franjas de grama descendo sobre a terra
        for (int x = 0; x < w; x++)
        {
            if (x % 3 == 0)
                PixelArt.Plot(pixels, w, h, x, groundY - 9, grassDark);
        }

        // Faixa de terra
        for (int y = groundY - 16; y <= groundY - 9; y++)
        {
            for (int x = 0; x < w; x++)
            {
                Color c = (y > groundY - 13) ? soil : soilDark;
                PixelArt.Plot(pixels, w, h, x, y, c);
            }
        }

        // Muro de pedra clara com blocos e juntas
        int stoneTopY = groundY - 17;
        int blockH = 8;
        int blockW = 16;

        for (int row = 0; row * blockH <= stoneTopY; row++)
        {
            int by = stoneTopY - (row + 1) * blockH;
            int xOffset = (row % 2 == 0) ? 0 : blockW / 2;

            for (int col = -1; col * blockW <= w + blockW; col++)
            {
                int bx = col * blockW + xOffset;
                // Preenche o bloco de pedra
                for (int y = by; y < by + blockH; y++)
                {
                    for (int x = bx; x < bx + blockW; x++)
                    {
                        if (x < 0 || y < 0 || x >= w || y >= h) continue;

                        if (y == by || x == bx + blockW - 1)
                            PixelArt.Plot(pixels, w, h, x, y, mortar); // junta
                        else if (y == by + blockH - 1 || x == bx)
                            PixelArt.Plot(pixels, w, h, x, y, stoneLight); // chanfro luz
                        else
                            PixelArt.Plot(pixels, w, h, x, y, stoneFace);
                    }
                }
            }
        }

        return PixelArt.Make(w, h, pixels);
    }

    static Sprite CreateWestTempleSprite(int w, int h)
    {
        var pixels = PixelArt.Clear(w, h);

        Color marble = new Color(0.92f, 0.90f, 0.86f);
        Color marbleLight = new Color(0.97f, 0.96f, 0.94f);
        Color marbleShade = new Color(0.70f, 0.68f, 0.64f);
        Color darkDoor = new Color(0.12f, 0.10f, 0.10f);
        Color warmGlow = new Color(0.98f, 0.82f, 0.45f);
        Color vine = new Color(0.24f, 0.52f, 0.22f);
        Color vineLight = new Color(0.36f, 0.68f, 0.26f);

        int baseX = 20;
        int templeW = 180;

        // Estilóbata (base com 3 degraus de pedra)
        for (int s = 0; s < 3; s++)
        {
            int stepW = templeW + 20 - s * 6;
            int sx = baseX - 10 + s * 3;
            PixelArt.Fill(pixels, w, h, sx, s * 4, stepW, 4, marble);
            PixelArt.Fill(pixels, w, h, sx, s * 4 + 3, stepW, 1, marbleLight);
        }

        int colBaseY = 12;
        int colH = 68;

        // Parede interna e porta central iluminada
        PixelArt.Fill(pixels, w, h, baseX + 16, colBaseY, templeW - 32, colH, marbleShade);
        int doorW = 28;
        int doorH = 50;
        int doorX = baseX + templeW / 2 - doorW / 2;
        PixelArt.Fill(pixels, w, h, doorX, colBaseY, doorW, doorH, darkDoor);
        // Luz quente vazando da porta
        PixelArt.Fill(pixels, w, h, doorX + 4, colBaseY, doorW - 8, doorH - 4, warmGlow);

        // Colunas caneladas na frente
        int colCount = 6;
        int colW = 8;
        int spacing = (templeW - colCount * colW) / (colCount - 1);

        for (int c = 0; c < colCount; c++)
        {
            int cx = baseX + c * (colW + spacing);
            // Base da coluna
            PixelArt.Fill(pixels, w, h, cx - 1, colBaseY, colW + 2, 4, marble);
            // Fuste
            PixelArt.Fill(pixels, w, h, cx, colBaseY + 4, colW, colH - 8, marble);
            PixelArt.Fill(pixels, w, h, cx, colBaseY + 4, 2, colH - 8, marbleLight);
            PixelArt.Fill(pixels, w, h, cx + colW - 2, colBaseY + 4, 2, colH - 8, marbleShade);
            // Capitel clássico
            PixelArt.Fill(pixels, w, h, cx - 2, colBaseY + colH - 4, colW + 4, 4, marbleLight);
        }

        // Entablamento (arquitrave e friso com métopes)
        int entabY = colBaseY + colH;
        int entabH = 14;
        PixelArt.Fill(pixels, w, h, baseX - 4, entabY, templeW + 8, entabH, marble);
        PixelArt.Fill(pixels, w, h, baseX - 4, entabY + entabH - 2, templeW + 8, 2, marbleLight);

        // Frontão triangular completo
        int pedimentBaseY = entabY + entabH;
        int pedimentH = 34;
        for (int ph = 0; ph < pedimentH; ph++)
        {
            int pw = (templeW + 8) - ph * 6;
            if (pw <= 2) break;
            int px = (baseX + templeW / 2) - pw / 2;
            PixelArt.Fill(pixels, w, h, px, pedimentBaseY + ph, pw, 1, ph >= pedimentH - 2 ? marbleLight : marble);
            // Chanfro nas pontas
            PixelArt.Plot(pixels, w, h, px, pedimentBaseY + ph, marbleLight);
            PixelArt.Plot(pixels, w, h, px + pw - 1, pedimentBaseY + ph, marbleShade);
        }

        // Hera descendo pelo frontão e pelas colunas
        for (int x = baseX + 20; x < baseX + templeW - 20; x += 4)
        {
            int y = entabY - (x % 5) * 4;
            PixelArt.Plot(pixels, w, h, x, y, vine);
            PixelArt.Plot(pixels, w, h, x + 1, y, vineLight);
            PixelArt.Plot(pixels, w, h, x, y - 1, vine);
        }

        return PixelArt.Make(w, h, pixels);
    }

    static Sprite CreateEastTempleSprite(int w, int h)
    {
        var pixels = PixelArt.Clear(w, h);

        Color marble = new Color(0.90f, 0.88f, 0.84f);
        Color marbleLight = new Color(0.96f, 0.95f, 0.92f);
        Color marbleShade = new Color(0.68f, 0.66f, 0.62f);
        Color vine = new Color(0.24f, 0.52f, 0.22f);
        Color vineLight = new Color(0.36f, 0.68f, 0.26f);

        int baseX = 20;
        int templeW = 160;

        // Base com degraus
        PixelArt.Fill(pixels, w, h, baseX, 0, templeW, 8, marble);
        PixelArt.Fill(pixels, w, h, baseX, 7, templeW, 1, marbleLight);

        int colBaseY = 8;
        int colH = 68;

        // Colunas: lado esquerdo quebrado, lado direito inteiro
        int[] colX = { baseX + 10, baseX + 42, baseX + 76, baseX + 110, baseX + 140 };
        int[] colHeights = { 28, 48, colH, colH, colH }; // Coluna curta/quebrada na esquerda

        for (int i = 0; i < colX.Length; i++)
        {
            int cx = colX[i];
            int ch = colHeights[i];
            int colW = 8;

            PixelArt.Fill(pixels, w, h, cx, colBaseY, colW, ch, marble);
            PixelArt.Fill(pixels, w, h, cx, colBaseY, 2, ch, marbleLight);
            PixelArt.Fill(pixels, w, h, cx + colW - 2, colBaseY, 2, ch, marbleShade);

            if (ch == colH)
            {
                PixelArt.Fill(pixels, w, h, cx - 2, colBaseY + ch - 4, colW + 4, 4, marbleLight);
            }
        }

        // Entablamento parcial (quebrado do lado esquerdo)
        int entabY = colBaseY + colH;
        int entabStart = baseX + 65;
        int entabW = templeW - 65 + 10;
        PixelArt.Fill(pixels, w, h, entabStart, entabY, entabW, 12, marble);
        PixelArt.Fill(pixels, w, h, entabStart, entabY + 10, entabW, 2, marbleLight);

        // Frontão incompleto (fraturado)
        int pedimentBaseY = entabY + 12;
        int pedimentH = 26;
        for (int ph = 0; ph < pedimentH; ph++)
        {
            int pw = entabW - ph * 5;
            if (pw <= 2) break;
            int px = (entabStart + entabW) - pw;
            PixelArt.Fill(pixels, w, h, px, pedimentBaseY + ph, pw, 1, marble);
        }

        // Mais hera tomando as ruínas
        for (int i = 0; i < 24; i++)
        {
            int vx = baseX + 25 + (i * 5) % 110;
            int vy = colBaseY + (i * 7) % 65;
            PixelArt.Fill(pixels, w, h, vx, vy, 3, 4, vine);
            PixelArt.Plot(pixels, w, h, vx + 1, vy + 2, vineLight);
        }

        return PixelArt.Make(w, h, pixels);
    }

    static Sprite CreateStatueSprite(int w, int h)
    {
        var pixels = PixelArt.Clear(w, h);

        Color marble = new Color(0.92f, 0.92f, 0.94f);
        Color marbleLight = new Color(0.98f, 0.98f, 1.0f);
        Color marbleShade = new Color(0.68f, 0.70f, 0.74f);
        Color pedestalCol = new Color(0.82f, 0.82f, 0.84f);
        Color inscription = new Color(0.50f, 0.52f, 0.56f);

        int cx = w / 2;

        // Pedestal clássico
        int pedW = 28;
        int pedH = 22;
        PixelArt.Fill(pixels, w, h, cx - pedW / 2, 0, pedW, pedH, pedestalCol);
        PixelArt.Fill(pixels, w, h, cx - pedW / 2 - 2, 0, pedW + 4, 3, marbleShade);
        PixelArt.Fill(pixels, w, h, cx - pedW / 2 - 2, pedH - 3, pedW + 4, 3, marbleLight);
        // Inscrição simulada no pedestal
        PixelArt.Fill(pixels, w, h, cx - 8, 8, 16, 2, inscription);
        PixelArt.Fill(pixels, w, h, cx - 6, 12, 12, 2, inscription);

        // Estátua clássica: com um braço levantado até a cabeça
        int statueBaseY = pedH;

        // Pernas e manto (toga)
        PixelArt.Fill(pixels, w, h, cx - 5, statueBaseY, 11, 24, marble);
        PixelArt.Fill(pixels, w, h, cx - 2, statueBaseY, 3, 14, marbleShade); // drapeado

        // Tronco heroico
        int torsoY = statueBaseY + 22;
        PixelArt.Fill(pixels, w, h, cx - 6, torsoY, 12, 18, marble);
        PixelArt.Fill(pixels, w, h, cx - 6, torsoY, 3, 18, marbleLight);
        PixelArt.Fill(pixels, w, h, cx + 3, torsoY, 3, 18, marbleShade);

        // Cabeça
        int headY = torsoY + 18;
        PixelArt.Fill(pixels, w, h, cx - 4, headY, 8, 9, marbleLight);

        // Braço esquerdo levantado até a cabeça
        PixelArt.Fill(pixels, w, h, cx - 9, torsoY + 8, 4, 12, marble);
        PixelArt.Fill(pixels, w, h, cx - 7, torsoY + 18, 5, 4, marbleLight); // mão junto à cabeça

        // Braço direito ao lado do corpo
        PixelArt.Fill(pixels, w, h, cx + 6, torsoY + 2, 4, 14, marble);

        return PixelArt.Make(w, h, pixels);
    }

    static Sprite CreateFountainBasinSprite(int w, int h)
    {
        var pixels = PixelArt.Clear(w, h);

        Color marble = new Color(0.88f, 0.88f, 0.90f);
        Color marbleLight = new Color(0.96f, 0.96f, 0.98f);
        Color marbleShade = new Color(0.66f, 0.68f, 0.72f);
        Color water = new Color(0.38f, 0.72f, 0.94f);

        int cx = w / 2;

        // Bacia inferior larga
        int baseW = 44;
        PixelArt.Fill(pixels, w, h, cx - baseW / 2, 0, baseW, 4, marble);
        PixelArt.Fill(pixels, w, h, cx - baseW / 2, 3, baseW, 1, marbleLight);

        int basinW = 48;
        int basinH = 12;
        PixelArt.Fill(pixels, w, h, cx - basinW / 2, 4, basinW, basinH, marble);
        PixelArt.Fill(pixels, w, h, cx - basinW / 2 + 2, 6, basinW - 4, basinH - 2, water);
        PixelArt.Fill(pixels, w, h, cx - basinW / 2 - 2, 13, basinW + 4, 3, marbleLight); // borda

        // Coluna central
        PixelArt.Fill(pixels, w, h, cx - 4, 16, 8, 10, marble);

        // Bacia superior menor
        int topBasinW = 24;
        PixelArt.Fill(pixels, w, h, cx - topBasinW / 2, 26, topBasinW, 6, marble);
        PixelArt.Fill(pixels, w, h, cx - topBasinW / 2 + 2, 28, topBasinW - 4, 4, water);
        PixelArt.Fill(pixels, w, h, cx - topBasinW / 2 - 1, 31, topBasinW + 2, 2, marbleLight);

        return PixelArt.Make(w, h, pixels);
    }

    static Sprite CreateFountainWaterSprite(int w, int h, int frame)
    {
        var pixels = PixelArt.Clear(w, h);

        Color waterLight = new Color(0.72f, 0.92f, 1.0f, 0.95f);
        Color waterBlue = new Color(0.30f, 0.70f, 0.98f, 0.90f);
        Color waterDark = new Color(0.18f, 0.52f, 0.85f, 0.80f);

        int cx = w / 2;
        int spoutY = 32;

        // Jato central subindo e caindo em arco
        int jetHeight = 14 + (frame == 1 ? 2 : (frame == 2 ? -1 : 0));
        PixelArt.Fill(pixels, w, h, cx - 2, spoutY, 4, jetHeight, waterLight);

        // Quedas d'água laterais em cascata
        int offset = frame * 2;
        for (int y = spoutY + jetHeight - 4; y >= 6; y--)
        {
            float prog = (float)(spoutY + jetHeight - y) / (spoutY + jetHeight - 6);
            int arcX = Mathf.RoundToInt(prog * 18f);

            // Queda esquerda
            PixelArt.Plot(pixels, w, h, cx - arcX, y, waterLight);
            PixelArt.Plot(pixels, w, h, cx - arcX - 1, y, waterBlue);

            // Queda direita
            PixelArt.Plot(pixels, w, h, cx + arcX, y, waterLight);
            PixelArt.Plot(pixels, w, h, cx + arcX + 1, y, waterBlue);
        }

        // Gotículas saltando
        int dropOff = (frame % 3) * 2;
        PixelArt.Plot(pixels, w, h, cx - 3 + dropOff, spoutY + jetHeight + 2, waterLight);
        PixelArt.Plot(pixels, w, h, cx + 3 - dropOff, spoutY + jetHeight + 3, waterLight);

        return PixelArt.Make(w, h, pixels);
    }

    static Sprite CreateCypressSprite(int w, int h)
    {
        var pixels = PixelArt.Clear(w, h);

        Color cypLight = new Color(0.24f, 0.50f, 0.20f);
        Color cypMid = new Color(0.14f, 0.38f, 0.16f);
        Color cypDark = new Color(0.08f, 0.24f, 0.10f);
        Color trunk = new Color(0.26f, 0.16f, 0.10f);

        int cx = w / 2;

        // Tronco curto na base
        PixelArt.Fill(pixels, w, h, cx - 2, 0, 4, 10, trunk);

        // Silhueta cônica e esguia de cipreste mediterrâneo
        int foliageH = h - 10;
        for (int y = 6; y < h; y++)
        {
            float t = (float)(y - 6) / foliageH;
            float maxR = Mathf.Sin(t * Mathf.PI * 0.95f) * (w * 0.42f);
            int r = Mathf.Max(1, Mathf.RoundToInt(maxR));

            for (int x = cx - r; x <= cx + r; x++)
            {
                if (x < 0 || x >= w) continue;
                float nx = (float)(x - (cx - r)) / (r * 2f);
                Color c = nx < 0.38f ? cypLight : (nx < 0.72f ? cypMid : cypDark);
                // Textura irregular
                if ((x + y * 3) % 7 == 0) c = cypLight;
                PixelArt.Plot(pixels, w, h, x, y, c);
            }
        }

        return PixelArt.Make(w, h, pixels);
    }

    static Sprite CreateColumnSprite(int w, int h, bool broken)
    {
        var pixels = PixelArt.Clear(w, h);

        Color marble = new Color(0.92f, 0.90f, 0.86f);
        Color marbleLight = new Color(0.97f, 0.96f, 0.94f);
        Color marbleShade = new Color(0.70f, 0.68f, 0.64f);
        Color vine = new Color(0.26f, 0.54f, 0.22f);

        int cx = w / 2;
        int colW = 12;

        // Base com chanfro
        PixelArt.Fill(pixels, w, h, cx - colW / 2 - 2, 0, colW + 4, 4, marble);
        PixelArt.Fill(pixels, w, h, cx - colW / 2 - 2, 3, colW + 4, 1, marbleLight);

        // Fuste com ranhuras verticais
        int topY = broken ? h - 8 : h - 6;
        PixelArt.Fill(pixels, w, h, cx - colW / 2, 4, colW, topY - 4, marble);
        PixelArt.Fill(pixels, w, h, cx - colW / 2, 4, 3, topY - 4, marbleLight);
        PixelArt.Fill(pixels, w, h, cx + colW / 2 - 3, 4, 3, topY - 4, marbleShade);

        if (!broken)
        {
            // Capitel clássico
            PixelArt.Fill(pixels, w, h, cx - colW / 2 - 3, h - 6, colW + 6, 6, marble);
            PixelArt.Fill(pixels, w, h, cx - colW / 2 - 3, h - 2, colW + 6, 2, marbleLight);
        }
        else
        {
            // Topo quebrado irregular
            for (int x = cx - colW / 2; x < cx + colW / 2; x++)
            {
                int rOff = (x * 7) % 5;
                PixelArt.Fill(pixels, w, h, x, topY, 1, rOff, marble);
            }
        }

        // Hera enrolada
        for (int y = 8; y < topY; y += 7)
        {
            PixelArt.Fill(pixels, w, h, cx - colW / 2 - 1, y, 3, 3, vine);
        }

        return PixelArt.Make(w, h, pixels);
    }

    static Sprite CreatePlinthSprite(int w, int h)
    {
        var pixels = PixelArt.Clear(w, h);

        Color marble = new Color(0.90f, 0.88f, 0.84f);
        Color marbleLight = new Color(0.96f, 0.95f, 0.92f);
        Color marbleShade = new Color(0.68f, 0.66f, 0.62f);
        Color vine = new Color(0.24f, 0.50f, 0.20f);

        int cx = w / 2;
        int pw = 20;

        PixelArt.Fill(pixels, w, h, cx - pw / 2 - 2, 0, pw + 4, 4, marbleShade);
        PixelArt.Fill(pixels, w, h, cx - pw / 2, 4, pw, h - 8, marble);
        PixelArt.Fill(pixels, w, h, cx - pw / 2, 4, 3, h - 8, marbleLight);
        PixelArt.Fill(pixels, w, h, cx + pw / 2 - 3, 4, 3, h - 8, marbleShade);
        PixelArt.Fill(pixels, w, h, cx - pw / 2 - 2, h - 4, pw + 4, 4, marbleLight);

        // Hera
        PixelArt.Fill(pixels, w, h, cx - pw / 2 - 1, 12, 3, 6, vine);

        return PixelArt.Make(w, h, pixels);
    }

    static Sprite CreateBushSprite(int w, int h, bool withFlowers)
    {
        var pixels = PixelArt.Clear(w, h);

        Color bLight = new Color(0.32f, 0.66f, 0.28f);
        Color bMid = new Color(0.18f, 0.48f, 0.20f);
        Color bDark = new Color(0.10f, 0.30f, 0.12f);

        Color pink = new Color(0.96f, 0.50f, 0.68f);
        Color yellow = new Color(1.0f, 0.86f, 0.28f);
        Color blue = new Color(0.42f, 0.78f, 0.98f);
        Color white = new Color(0.98f, 0.98f, 0.98f);

        int cx = w / 2;
        int rx = w / 2 - 2;
        int ry = h - 3;

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                float nx = (float)(x - cx) / rx;
                float ny = (float)y / ry;
                if (nx * nx + ny * ny <= 1f)
                {
                    Color c = ny > 0.6f ? bLight : (ny > 0.25f ? bMid : bDark);
                    PixelArt.Plot(pixels, w, h, x, y, c);
                }
            }
        }

        if (withFlowers)
        {
            // Flores coloridas: rosa, amarelo, azul, branco
            int[] fx = { cx - 10, cx - 2, cx + 8, cx - 7, cx + 5 };
            int[] fy = { h - 8, h - 6, h - 9, h - 14, h - 13 };
            Color[] fCols = { pink, yellow, blue, white, pink };

            for (int i = 0; i < fx.Length; i++)
            {
                if (fx[i] >= 1 && fx[i] < w - 1 && fy[i] >= 1 && fy[i] < h - 1)
                {
                    PixelArt.Plot(pixels, w, h, fx[i], fy[i], fCols[i]);
                    PixelArt.Plot(pixels, w, h, fx[i] + 1, fy[i], fCols[i]);
                    PixelArt.Plot(pixels, w, h, fx[i], fy[i] + 1, fCols[i]);
                    PixelArt.Plot(pixels, w, h, fx[i] - 1, fy[i], fCols[i]);
                }
            }
        }

        return PixelArt.Make(w, h, pixels);
    }

    // =========================================================================
    // 7. PLATAFORMAS: DEGRAUS, LAJE, BLOCO DE RUNA E ILHAS FLUTUANTES
    // =========================================================================

    static Sprite CreateStairStepSprite(int w, int h)
    {
        var pixels = PixelArt.Clear(w, h);

        Color wire = new Color(0.74f, 0.96f, 0.42f);
        Color grass = new Color(0.36f, 0.68f, 0.18f);
        Color stone = new Color(0.86f, 0.84f, 0.78f);
        Color stoneLight = new Color(0.94f, 0.93f, 0.88f);
        Color stoneShade = new Color(0.70f, 0.66f, 0.60f);

        // Tampa de grama
        for (int x = 0; x < w; x++)
        {
            PixelArt.Plot(pixels, w, h, x, h - 1, wire);
            PixelArt.Plot(pixels, w, h, x, h - 2, grass);
            PixelArt.Plot(pixels, w, h, x, h - 3, grass);
            if (x % 3 == 0) PixelArt.Plot(pixels, w, h, x, h - 4, grass);
        }

        // Degrau de pedra
        for (int y = 0; y < h - 3; y++)
        {
            for (int x = 0; x < w; x++)
            {
                if (pixels[y * w + x].a > 0f) continue;
                Color c = (x == 0) ? stoneLight : (x == w - 1 || y == 0 ? stoneShade : stone);
                PixelArt.Plot(pixels, w, h, x, y, c);
            }
        }

        return PixelArt.Make(w, h, pixels);
    }

    static Sprite CreateLajeSprite(int w, int h)
    {
        var pixels = PixelArt.Clear(w, h);

        Color wire = new Color(0.74f, 0.96f, 0.42f);
        Color grass = new Color(0.36f, 0.68f, 0.18f);
        Color stone = new Color(0.90f, 0.88f, 0.84f);
        Color stoneLight = new Color(0.96f, 0.95f, 0.92f);
        Color stoneShade = new Color(0.70f, 0.68f, 0.64f);

        // Topo com franja de grama
        for (int x = 0; x < w; x++)
        {
            PixelArt.Plot(pixels, w, h, x, h - 1, wire);
            PixelArt.Plot(pixels, w, h, x, h - 2, grass);
            PixelArt.Plot(pixels, w, h, x, h - 3, grass);
            if (x % 4 == 0) PixelArt.Plot(pixels, w, h, x, h - 4, grass);
        }

        // Laje de mármore esculpido
        for (int y = 0; y < h - 3; y++)
        {
            for (int x = 0; x < w; x++)
            {
                if (pixels[y * w + x].a > 0f) continue;
                Color c = (y == h - 4 || x == 0) ? stoneLight : (y == 0 || x == w - 1 ? stoneShade : stone);
                PixelArt.Plot(pixels, w, h, x, y, c);
            }
        }

        return PixelArt.Make(w, h, pixels);
    }

    static Sprite CreateRuneBlockSprite()
    {
        // 1x1 unidade = 16x16 pixels
        int w = 16;
        int h = 16;
        var pixels = PixelArt.Clear(w, h);

        Color marble = new Color(0.88f, 0.86f, 0.82f);
        Color marbleLight = new Color(0.96f, 0.95f, 0.92f);
        Color marbleShade = new Color(0.68f, 0.65f, 0.60f);

        Color runeGold = new Color(1.0f, 0.84f, 0.28f);
        Color runeCore = new Color(1.0f, 0.96f, 0.70f);
        Color runeGlow = new Color(0.88f, 0.68f, 0.18f);
        Color vine = new Color(0.24f, 0.50f, 0.20f);

        // Bloco de mármore com borda chanfrada
        for (int y = 2; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                Color c = marble;
                if (y == h - 1 || x == 0) c = marbleLight;
                else if (y == 2 || x == w - 1) c = marbleShade;
                PixelArt.Plot(pixels, w, h, x, y, c);
            }
        }

        // Selo em losango dourado (Runa de Aetherion) no centro
        int cx = 8;
        int cy = 9;
        int radius = 4;

        for (int y = cy - radius; y <= cy + radius; y++)
        {
            for (int x = cx - radius; x <= cx + radius; x++)
            {
                int dist = Mathf.Abs(x - cx) + Mathf.Abs(y - cy);
                if (dist == radius)
                {
                    PixelArt.Plot(pixels, w, h, x, y, runeGold);
                }
                else if (dist < radius)
                {
                    Color c = (dist == 0) ? runeCore : runeGlow;
                    PixelArt.Plot(pixels, w, h, x, y, c);
                }
            }
        }

        // Hera curta embaixo do bloco
        PixelArt.Plot(pixels, w, h, 2, 1, vine);
        PixelArt.Plot(pixels, w, h, 2, 0, vine);
        PixelArt.Plot(pixels, w, h, 6, 1, vine);
        PixelArt.Plot(pixels, w, h, 11, 1, vine);
        PixelArt.Plot(pixels, w, h, 12, 0, vine);

        return PixelArt.Make(w, h, pixels);
    }

    static Sprite CreateFloatingIslandSprite(int w, int h)
    {
        var pixels = PixelArt.Clear(w, h);

        Color wire = new Color(0.74f, 0.96f, 0.42f);
        Color grass = new Color(0.36f, 0.68f, 0.18f);
        Color grassDark = new Color(0.22f, 0.46f, 0.12f);
        Color earth = new Color(0.48f, 0.32f, 0.18f);
        Color earthDark = new Color(0.32f, 0.20f, 0.10f);

        Color pink = new Color(0.96f, 0.50f, 0.68f);
        Color yellow = new Color(1.0f, 0.86f, 0.28f);
        Color blue = new Color(0.42f, 0.78f, 0.98f);

        int cx = w / 2;
        int surfaceY = h - 4; // Superfície andável alinhada com o colisor

        // Terra irregular embaixo
        for (int x = 2; x < w - 2; x++)
        {
            float nx = (float)(x - cx) / (w * 0.5f);
            float taper = (1f - nx * nx);
            int bottomY = Mathf.RoundToInt((1f - taper) * (surfaceY - 4));
            // Rugosidade da rocha/terra
            bottomY += ((x * 5) % 4) - 2;
            bottomY = Mathf.Clamp(bottomY, 1, surfaceY - 5);

            for (int y = bottomY; y < surfaceY - 3; y++)
            {
                Color c = (y > surfaceY - 8) ? earth : earthDark;
                PixelArt.Plot(pixels, w, h, x, y, c);
            }
        }

        // Tampa de grama com sobra nas pontas
        for (int x = 0; x < w; x++)
        {
            PixelArt.Plot(pixels, w, h, x, surfaceY, wire);
            PixelArt.Plot(pixels, w, h, x, surfaceY - 1, grass);
            PixelArt.Plot(pixels, w, h, x, surfaceY - 2, grass);
            PixelArt.Plot(pixels, w, h, x, surfaceY - 3, grassDark);
            if (x % 3 == 0) PixelArt.Plot(pixels, w, h, x, surfaceY - 4, grassDark);
        }

        // Flores em cima da grama (ficam fora do colisor)
        int[] fx = { cx - 14, cx - 6, cx + 4, cx + 12 };
        Color[] fCols = { pink, yellow, blue, pink };

        for (int i = 0; i < fx.Length; i++)
        {
            int px = fx[i];
            if (px >= 2 && px < w - 2)
            {
                PixelArt.Plot(pixels, w, h, px, surfaceY + 1, fCols[i]);
                PixelArt.Plot(pixels, w, h, px + 1, surfaceY + 1, fCols[i]);
                PixelArt.Plot(pixels, w, h, px, surfaceY + 2, fCols[i]);
            }
        }

        return PixelArt.Make(w, h, pixels);
    }

    static Sprite CreateOvalShadowSprite(int w, int h)
    {
        var pixels = PixelArt.Clear(w, h);
        int cx = w / 2;
        int cy = h / 2;
        Color shadow = new Color(0.06f, 0.10f, 0.14f, 0.28f);

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                float nx = (float)(x - cx) / (w * 0.5f);
                float ny = (float)(y - cy) / (h * 0.5f);
                if (nx * nx + ny * ny <= 1f)
                {
                    float factor = 1f - (nx * nx + ny * ny);
                    PixelArt.Plot(pixels, w, h, x, y, new Color(shadow.r, shadow.g, shadow.b, shadow.a * factor));
                }
            }
        }

        return PixelArt.Make(w, h, pixels);
    }
}

/// <summary>
/// Camada de parallax que também deriva suavemente ao longo do tempo (ex: nuvens soltas).
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
/// Anima o borrifo e os jatos de água da fonte clássica.
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
