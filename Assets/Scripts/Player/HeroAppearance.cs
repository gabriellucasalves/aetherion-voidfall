using UnityEngine;

public static class HeroAppearance
{
    public static void Build(Transform parent, CharacterData hero)
    {
        if (hero != null && hero.Id == "guerreiro" && GuerreiroVisual.Attach(parent))
            return;

        if (hero != null && hero.Id == "mago" && MagoVisual.Attach(parent))
            return;

        Color body = hero != null ? hero.Accent : Color.white;
        Vector2 bodySize = BodySize(hero);
        Vector2 headSize = new Vector2(bodySize.x * 0.55f, bodySize.x * 0.55f);

        CreateQuad(parent, "Corpo", Vector3.zero, bodySize, body);
        CreateQuad(parent, "Cabeca", new Vector3(0.06f, bodySize.y * 0.55f, 0f), headSize, Color.Lerp(body, Color.white, 0.25f));
        CreateQuad(parent, "Perna", new Vector3(0.04f, -bodySize.y * 0.42f, 0f), new Vector2(bodySize.x * 0.55f, bodySize.y * 0.28f), Color.Lerp(body, Color.black, 0.28f));

        if (hero != null && hero.Id == "guerreiro")
        {
            CreateQuad(parent, "Escudo", new Vector3(0.55f, 0.05f, 0f), new Vector2(0.28f, 0.7f), new Color(0.85f, 0.25f, 0.12f));
            CreateQuad(parent, "Espada", new Vector3(-0.55f, 0.25f, 0f), new Vector2(0.16f, 0.95f), new Color(1f, 0.7f, 0.25f));
        }
        else if (hero != null && hero.Id == "mago")
        {
            CreateQuad(parent, "Cajado", new Vector3(-0.42f, 0.2f, 0f), new Vector2(0.12f, 1.1f), new Color(0.35f, 0.4f, 0.85f));
            CreateQuad(parent, "Orbe", new Vector3(-0.42f, 0.85f, 0f), new Vector2(0.22f, 0.22f), new Color(0.55f, 0.75f, 1f));
        }
        else if (hero != null && hero.Id == "anjo")
        {
            CreateQuad(parent, "AsaE", new Vector3(-0.55f, 0.2f, 0f), new Vector2(0.45f, 0.35f), new Color(1f, 0.9f, 0.55f));
            CreateQuad(parent, "AsaD", new Vector3(0.55f, 0.2f, 0f), new Vector2(0.45f, 0.35f), new Color(1f, 0.9f, 0.55f));
        }
    }

    static Vector2 BodySize(CharacterData hero)
    {
        if (hero == null)
            return new Vector2(0.7f, 0.9f);
        if (hero.Id == "guerreiro")
            return new Vector2(0.95f, 1.05f);
        if (hero.Id == "mago")
            return new Vector2(0.65f, 1.15f);
        return new Vector2(0.7f, 0.95f);
    }

    static void CreateQuad(Transform parent, string name, Vector3 local, Vector2 size, Color color)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = local;
        var renderer = go.AddComponent<SpriteRenderer>();
        renderer.sprite = WhiteSprite();
        renderer.color = color;
        renderer.sortingOrder = 10;
        go.transform.localScale = new Vector3(size.x, size.y, 1f);
    }

    static Sprite WhiteSprite()
    {
        var texture = Texture2D.whiteTexture;
        return Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), texture.width);
    }
}
