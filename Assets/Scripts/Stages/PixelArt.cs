using UnityEngine;

public static class PixelArt
{
    public const float Ppu = 16f;

    public static Sprite Make(int width, int height, Color[] pixels)
    {
        var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Point;
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.SetPixels(pixels);
        texture.Apply(false, false);
        return Sprite.Create(texture, new Rect(0f, 0f, width, height), new Vector2(0.5f, 0f), Ppu);
    }

    public static Color[] Clear(int width, int height)
    {
        return new Color[width * height];
    }

    public static void Plot(Color[] pixels, int width, int height, int x, int y, Color color)
    {
        if (x < 0 || y < 0 || x >= width || y >= height)
            return;
        int i = y * width + x;
        if (color.a >= 1f || pixels[i].a <= 0f)
        {
            pixels[i] = color;
            return;
        }

        Color under = pixels[i];
        float a = color.a + under.a * (1f - color.a);
        pixels[i] = new Color(
            (color.r * color.a + under.r * under.a * (1f - color.a)) / a,
            (color.g * color.a + under.g * under.a * (1f - color.a)) / a,
            (color.b * color.a + under.b * under.a * (1f - color.a)) / a,
            a);
    }

    public static void Fill(Color[] pixels, int width, int height, int x, int y, int w, int h, Color color)
    {
        for (int yy = y; yy < y + h; yy++)
            for (int xx = x; xx < x + w; xx++)
                Plot(pixels, width, height, xx, yy, color);
    }

    public static GameObject Place(string name, Sprite sprite, Vector3 position, int order, float parallax)
    {
        var go = new GameObject(name);
        go.transform.position = position;
        var renderer = go.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.sortingOrder = order;
        if (parallax > 0f)
            go.AddComponent<ParallaxLayer>().Setup(parallax);
        return go;
    }
}
