using UnityEngine;

public static class PongSprite
{
    static Sprite _white;

    public static Sprite White
    {
        get
        {
            if (_white != null)
                return _white;

            var texture = Texture2D.whiteTexture;
            _white = Sprite.Create(
                texture,
                new Rect(0f, 0f, texture.width, texture.height),
                new Vector2(0.5f, 0.5f),
                texture.width);
            _white.name = "PongWhite";
            return _white;
        }
    }

    public static SpriteRenderer AddRenderer(GameObject target, Color color, Vector2 size)
    {
        var renderer = target.GetComponent<SpriteRenderer>();
        if (renderer == null)
            renderer = target.AddComponent<SpriteRenderer>();

        renderer.sprite = White;
        renderer.color = color;
        target.transform.localScale = new Vector3(size.x, size.y, 1f);
        return renderer;
    }
}
