using System.Collections.Generic;
using UnityEngine;

// Retratos do cut-in. O sheet do herói entra com o contrato do jogo:
// PPU 15, Point, pivô (0.5, 3/64) no corpo inteiro. O close dos olhos é um recorte
// desses pixels (sem reamostrar). Se o sheet faltar, o painel usa arte gerada.
public static class HeroCutInSprites
{
    const int Cell = 64;
    const int Cols = 7;
    const float Ppu = 15f;
    const float FootPivot = 3f / 64f;

    struct HeadCrop
    {
        public int X;
        public int YTop;
        public int W;
        public int H;
    }

    static readonly Dictionary<string, Sprite> Cache = new Dictionary<string, Sprite>();

    public static Sprite Frame(string heroId, int index)
    {
        string key = "frame:" + heroId + ":" + index;
        if (Cache.TryGetValue(key, out var cached) && cached != null)
            return cached;

        var texture = LoadSheet(heroId);
        if (texture == null)
            return null;

        int col = index % Cols;
        int row = index / Cols;
        int x = col * Cell;
        int y = texture.height - (row + 1) * Cell;
        if (x < 0 || y < 0 || x + Cell > texture.width || y + Cell > texture.height)
            return null;

        var sprite = Sprite.Create(
            texture,
            new Rect(x, y, Cell, Cell),
            new Vector2(0.5f, FootPivot),
            Ppu,
            0,
            SpriteMeshType.FullRect);
        Cache[key] = sprite;
        return sprite;
    }

    public static Sprite Bust(string heroId, int index)
    {
        string key = "bust:" + heroId + ":" + index;
        if (Cache.TryGetValue(key, out var cached) && cached != null)
            return cached;

        var trimmed = TrimCell(heroId, index);
        if (trimmed == null)
            trimmed = PlaceholderBust(heroId);
        if (trimmed != null)
            Cache[key] = trimmed;
        return trimmed;
    }

    public static Sprite Eyes(string heroId, bool bright)
    {
        string key = "eyes:" + heroId + ":" + (bright ? "1" : "0");
        if (Cache.TryGetValue(key, out var cached) && cached != null)
            return cached;

        var sprite = EyesFromSheet(heroId, bright) ?? PlaceholderEyes(heroId, bright);
        if (sprite != null)
            Cache[key] = sprite;
        return sprite;
    }

    public static Sprite PlaceholderBust(string heroId)
    {
        string key = "phbust:" + heroId;
        if (Cache.TryGetValue(key, out var cached) && cached != null)
            return cached;

        const int w = 32;
        const int h = 48;
        var pixels = new Color32[w * h];
        Fill(pixels, w, 0, 0, w, h, new Color32(0, 0, 0, 0));

        if (heroId == "mago")
        {
            Fill(pixels, w, 6, 6, 4, 30, new Color32(90, 70, 48, 255));
            Fill(pixels, w, 4, 32, 8, 6, new Color32(180, 140, 255, 255));
            Fill(pixels, w, 10, 4, 14, 22, new Color32(48, 32, 96, 255));
            Fill(pixels, w, 12, 24, 12, 16, new Color32(28, 16, 58, 255));
            Fill(pixels, w, 14, 30, 8, 8, new Color32(8, 4, 16, 255));
            Fill(pixels, w, 16, 33, 3, 3, new Color32(230, 210, 255, 255));
        }
        else if (heroId == "arqueiro")
        {
            Fill(pixels, w, 4, 14, 6, 20, new Color32(120, 72, 36, 255));
            Fill(pixels, w, 13, 2, 8, 16, new Color32(72, 48, 28, 255));
            Fill(pixels, w, 12, 16, 10, 16, new Color32(36, 110, 58, 255));
            Fill(pixels, w, 13, 30, 8, 10, new Color32(210, 170, 120, 255));
            Fill(pixels, w, 14, 34, 2, 2, new Color32(110, 240, 180, 255));
            Fill(pixels, w, 18, 34, 2, 2, new Color32(110, 240, 180, 255));
            Fill(pixels, w, 22, 28, 6, 3, new Color32(255, 214, 64, 255));
        }
        else
        {
            Fill(pixels, w, 8, 8, 8, 18, new Color32(150, 24, 28, 255));
            Fill(pixels, w, 18, 6, 8, 16, new Color32(140, 20, 24, 255));
            Fill(pixels, w, 11, 4, 10, 14, new Color32(70, 50, 40, 255));
            Fill(pixels, w, 10, 16, 14, 14, new Color32(70, 74, 84, 255));
            Fill(pixels, w, 10, 28, 14, 12, new Color32(58, 62, 72, 255));
            Fill(pixels, w, 6, 34, 5, 8, new Color32(196, 176, 130, 255));
            Fill(pixels, w, 22, 34, 5, 8, new Color32(196, 176, 130, 255));
            Fill(pixels, w, 12, 32, 10, 3, new Color32(16, 12, 14, 255));
            Fill(pixels, w, 13, 33, 3, 2, new Color32(255, 220, 80, 255));
            Fill(pixels, w, 18, 33, 3, 2, new Color32(255, 220, 80, 255));
        }

        var sprite = SupremeFx.MakeSprite(w, h, pixels, Ppu, new Vector2(0.5f, FootPivot));
        Cache[key] = sprite;
        return sprite;
    }

    static Sprite EyesFromSheet(string heroId, bool bright)
    {
        var texture = LoadSheet(heroId);
        if (texture == null || !texture.isReadable)
            return null;

        var crop = CropFor(heroId);
        int index = 0;
        int col = index % Cols;
        int row = index / Cols;
        int cellBottom = texture.height - (row + 1) * Cell;
        int x = col * Cell + crop.X;
        int y = cellBottom + (Cell - crop.YTop - crop.H);
        if (x < 0 || y < 0 || x + crop.W > texture.width || y + crop.H > texture.height)
            return null;

        Color32[] pixels;
        try
        {
            pixels = texture.GetPixels32();
        }
        catch (System.Exception)
        {
            return null;
        }

        int stride = texture.width;
        var cut = new Color32[crop.W * crop.H];
        int opaque = 0;
        for (int iy = 0; iy < crop.H; iy++)
        {
            for (int ix = 0; ix < crop.W; ix++)
            {
                var sample = pixels[(y + iy) * stride + (x + ix)];
                if (sample.a > 16)
                    opaque++;
                if (bright && sample.a > 16)
                    sample = Lerp32(sample, new Color32(255, 255, 255, 255), heroId == "mago" ? 0.15f : 0.62f);
                cut[iy * crop.W + ix] = sample;
            }
        }

        if (opaque < 12)
            return null;

        if (bright && heroId == "mago")
            PaintStars(cut, crop.W, crop.H);

        return SupremeFx.MakeSprite(crop.W, crop.H, cut, Ppu, new Vector2(0.5f, 0.5f));
    }

    static Sprite TrimCell(string heroId, int index)
    {
        var texture = LoadSheet(heroId);
        if (texture == null)
            return null;

        int col = index % Cols;
        int row = index / Cols;
        int x0 = col * Cell;
        int y0 = texture.height - (row + 1) * Cell;
        if (x0 < 0 || y0 < 0 || x0 + Cell > texture.width || y0 + Cell > texture.height)
            return null;

        if (!texture.isReadable)
        {
            return Sprite.Create(texture, new Rect(x0, y0, Cell, Cell), new Vector2(0.5f, FootPivot), Ppu, 0, SpriteMeshType.FullRect);
        }

        Color32[] pixels;
        try
        {
            pixels = texture.GetPixels32();
        }
        catch (System.Exception)
        {
            return Sprite.Create(texture, new Rect(x0, y0, Cell, Cell), new Vector2(0.5f, FootPivot), Ppu, 0, SpriteMeshType.FullRect);
        }

        int stride = texture.width;
        int minX = Cell, minY = Cell, maxX = -1, maxY = -1;
        for (int iy = 0; iy < Cell; iy++)
        {
            for (int ix = 0; ix < Cell; ix++)
            {
                if (pixels[(y0 + iy) * stride + (x0 + ix)].a <= 16)
                    continue;
                if (ix < minX) minX = ix;
                if (iy < minY) minY = iy;
                if (ix > maxX) maxX = ix;
                if (iy > maxY) maxY = iy;
            }
        }

        if (maxX < minX)
            return null;

        minX = Mathf.Max(0, minX - 1);
        minY = Mathf.Max(0, minY - 1);
        maxX = Mathf.Min(Cell - 1, maxX + 1);
        maxY = Mathf.Min(Cell - 1, maxY + 1);
        int w = maxX - minX + 1;
        int h = maxY - minY + 1;
        var cut = new Color32[w * h];
        for (int iy = 0; iy < h; iy++)
        for (int ix = 0; ix < w; ix++)
            cut[iy * w + ix] = pixels[(y0 + minY + iy) * stride + (x0 + minX + ix)];

        return SupremeFx.MakeSprite(w, h, cut, Ppu, new Vector2(0.5f, 0.5f));
    }

    static Sprite PlaceholderEyes(string heroId, bool bright)
    {
        const int w = 48;
        const int h = 32;
        var pixels = new Color32[w * h];
        Fill(pixels, w, 0, 0, w, h, new Color32(0, 0, 0, 0));
        var eye = bright ? new Color32(255, 255, 255, 255) : new Color32(255, 214, 64, 255);

        if (heroId == "mago")
        {
            Fill(pixels, w, 6, 4, 36, 24, new Color32(18, 10, 32, 255));
            Fill(pixels, w, 10, 8, 28, 16, new Color32(6, 4, 12, 255));
            eye = bright ? new Color32(230, 210, 255, 255) : new Color32(80, 50, 120, 255);
            PaintStars(pixels, w, h);
            Fill(pixels, w, 16, 14, 4, 4, eye);
            Fill(pixels, w, 28, 14, 4, 4, eye);
        }
        else if (heroId == "arqueiro")
        {
            Fill(pixels, w, 4, 16, 40, 12, new Color32(18, 48, 28, 255));
            Fill(pixels, w, 8, 8, 32, 14, new Color32(12, 28, 18, 255));
            eye = bright ? new Color32(180, 255, 220, 255) : new Color32(40, 90, 60, 255);
            Fill(pixels, w, 14, 12, 6, 4, eye);
            Fill(pixels, w, 28, 12, 6, 4, eye);
            Fill(pixels, w, 34, 18, 8, 3, new Color32(255, 214, 64, 255));
        }
        else
        {
            Fill(pixels, w, 8, 6, 32, 20, new Color32(58, 62, 72, 255));
            Fill(pixels, w, 4, 16, 8, 10, new Color32(190, 170, 130, 255));
            Fill(pixels, w, 36, 16, 8, 10, new Color32(190, 170, 130, 255));
            Fill(pixels, w, 12, 12, 24, 6, new Color32(16, 12, 14, 255));
            Fill(pixels, w, 14, 13, 6, 4, eye);
            Fill(pixels, w, 28, 13, 6, 4, eye);
            Fill(pixels, w, 22, 8, 4, 10, new Color32(16, 12, 14, 255));
        }

        return SupremeFx.MakeSprite(w, h, pixels, Ppu, new Vector2(0.5f, 0.5f));
    }

    static void PaintStars(Color32[] pixels, int w, int h)
    {
        var star = new Color32(255, 250, 255, 255);
        Plot(pixels, w, h, w / 2 - 6, h / 2, star);
        Plot(pixels, w, h, w / 2 - 7, h / 2, star);
        Plot(pixels, w, h, w / 2 - 5, h / 2, star);
        Plot(pixels, w, h, w / 2 - 6, h / 2 + 1, star);
        Plot(pixels, w, h, w / 2 - 6, h / 2 - 1, star);
        Plot(pixels, w, h, w / 2 + 6, h / 2, star);
        Plot(pixels, w, h, w / 2 + 5, h / 2, star);
        Plot(pixels, w, h, w / 2 + 7, h / 2, star);
        Plot(pixels, w, h, w / 2 + 6, h / 2 + 1, star);
        Plot(pixels, w, h, w / 2 + 6, h / 2 - 1, star);
    }

    static HeadCrop CropFor(string heroId)
    {
        if (heroId == "mago")
            return new HeadCrop { X = 8, YTop = 10, W = 28, H = 30 };
        if (heroId == "arqueiro")
            return new HeadCrop { X = 12, YTop = 30, W = 22, H = 20 };
        return new HeadCrop { X = 15, YTop = 34, W = 30, H = 20 };
    }

    static Texture2D LoadSheet(string heroId)
    {
        string folder = heroId == "mago" ? "Mago" : heroId == "arqueiro" ? "Arqueiro" : "Guerreiro";
        var texture = Resources.Load<Texture2D>(folder + "/sheet");
        if (texture == null)
            return null;
        texture.filterMode = FilterMode.Point;
        texture.wrapMode = TextureWrapMode.Clamp;
        return texture;
    }

    static Color32 Lerp32(Color32 a, Color32 b, float t)
    {
        return new Color32(
            (byte)Mathf.Lerp(a.r, b.r, t),
            (byte)Mathf.Lerp(a.g, b.g, t),
            (byte)Mathf.Lerp(a.b, b.b, t),
            a.a);
    }

    static void Fill(Color32[] pixels, int stride, int x, int y, int w, int h, Color32 color)
    {
        int height = pixels.Length / stride;
        for (int iy = 0; iy < h; iy++)
        for (int ix = 0; ix < w; ix++)
            Plot(pixels, stride, height, x + ix, y + iy, color);
    }

    static void Plot(Color32[] pixels, int stride, int height, int x, int y, Color32 color)
    {
        if ((uint)x >= (uint)stride || (uint)y >= (uint)height)
            return;
        pixels[y * stride + x] = color;
    }
}
