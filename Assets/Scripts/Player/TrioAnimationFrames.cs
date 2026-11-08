using UnityEngine;

// Sheets aprovadas: células 64px, pés em y=61. Mantém o PPU local já usado pelos heróis.
public static class TrioAnimationFrames
{
    public static Sprite[] Vfx(string name, int width, int height, int count, Vector2 pivot)
    {
        var texture = Resources.Load<Texture2D>("TrioVfx/" + name);
        if (texture == null || texture.width < width * count || texture.height < height) return null;
        texture.filterMode = FilterMode.Point;
        texture.wrapMode = TextureWrapMode.Clamp;
        var frames = new Sprite[count];
        for (int i = 0; i < count; i++)
            frames[i] = Sprite.Create(texture, new Rect(i * width, 0, width, height),
                pivot, PixelArt.Ppu, 0, SpriteMeshType.FullRect);
        return frames;
    }
    public static Sprite[] Load(string hero, string action, int count)
    {
        var texture = Resources.Load<Texture2D>("TrioAnimations/" + hero + "/" + action);
        if (texture == null || texture.width < count * 64 || texture.height < 64)
            return null;
        texture.filterMode = FilterMode.Point;
        texture.wrapMode = TextureWrapMode.Clamp;
        var frames = new Sprite[count];
        for (int i = 0; i < count; i++)
            frames[i] = Sprite.Create(texture, new Rect(i * 64, 0, 64, 64),
                new Vector2(26f / 64f, 3f / 64f), PixelArt.Ppu, 0, SpriteMeshType.FullRect);
        return frames;
    }

    public static void Copy(Sprite[] target, int start, string hero, string action, int count)
    {
        var frames = Load(hero, action, count);
        if (frames == null) return;
        for (int i = 0; i < count && start + i < target.Length; i++)
            target[start + i] = frames[i];
    }
}
