using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// HUD 16-bit central no topo da tela, 100% visivel dentro do quadro do mundo.
public static class StageHud
{
    public const float BarWidth = 340f;
    public sealed class View
    {
        public Text Name, LifeValue, DefenseValue, DefenseLabel, SpecialValue, SupremeValueText;
        public Image Life, Defense, Special, Supreme, SupremeGlow, SupremeParticles, SupremeBanner;
        public Sprite SpecialNormal, SpecialReadySprite;
        public Sprite[] SupremeFullFrames, GlowFrames, ParticleFrames, BannerKeyFrames, BannerTouchFrames;
    }
    static Sprite _circle;
    static readonly Dictionary<string, Sprite> Portraits = new Dictionary<string, Sprite>();
    static readonly Dictionary<string, Sprite> HudSprites = new Dictionary<string, Sprite>();
    public static View Build(Transform parent, CharacterData hero, string phase)
    {
        // UiKit usa referência 1920×1080; cada pixel do pack 384×216 vale cinco unidades UI.
        var go = new GameObject("HudHorizontalV1", typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var root = (RectTransform)go.transform;
        root.anchorMin = root.anchorMax = new Vector2(.5f, 1f);
        root.pivot = new Vector2(.5f, 1f);
        root.sizeDelta = new Vector2(1920, 1080);
        root.anchoredPosition = Vector2.zero;
        var v = new View();
        string id = hero != null ? hero.Id : "guerreiro";
        // Posições reorganizadas para acomodar as duas barras:
        int icon = 26;
        int life = 52;
        int escudo = 130;
        int special = 208;
        int supreme = 286;

        Art(root, "icon_" + id + "_20x20", icon, 4, 20, 20);
        Art(root, "label_vida", life - 1, 5, 17, 7);
        v.Life = HudBar(root, "vida", life);
        v.LifeValue = HudValue(root, life, 24);
        Art(root, id == "mago" ? "label_campo" : "label_escudo", escudo - 1, 5,
            id == "mago" ? 21 : 25, 7);
        v.Defense = HudBar(root, "escudo", escudo);
        v.DefenseValue = HudValue(root, escudo, 24);
        v.DefenseLabel = HudValue(root, escudo, 32);
        v.DefenseLabel.gameObject.SetActive(false);

        // ESPECIAL
        Art(root, "label_especial", special - 1, 5, 33, 7);
        v.Special = HudBar(root, "especial", special);
        v.SpecialNormal = HudSprite("bar_fill_especial_" + id + "_68x6");
        v.SpecialReadySprite = HudSprite("bar_fill_especial_" + id + "_pronto_68x6");
        if (v.SpecialNormal != null) v.Special.sprite = v.SpecialNormal;
        v.SpecialValue = HudValue(root, special, 24);

        // SUPREMO
        v.SupremeFullFrames = HudFrames("bar_fill_supremo_cheio_4f_68x6", 68, 6);
        v.GlowFrames = HudFrames("bar_brilho_supremo_4f_88x24", 88, 24);
        v.ParticleFrames = HudFrames("particulas_supremo_6f_96x30", 96, 30);
        v.BannerKeyFrames = HudFrames("banner_supremo_pronto_teclado_2f_91x19", 91, 19);
        v.BannerTouchFrames = HudFrames("banner_supremo_pronto_toque_2f_91x19", 91, 19);

        // 1. SupremoBrilho em (278,7) 88x24
        v.SupremeGlow = Art(root, "SupremoBrilho", 278, 7, 88, 24);
        if (v.GlowFrames != null && v.GlowFrames.Length > 0) v.SupremeGlow.sprite = v.GlowFrames[0];
        v.SupremeGlow.gameObject.SetActive(false);

        // 2. label_supremo_29x7 em (285,5) 29x7
        Art(root, "label_supremo_29x7", 285, 5, 29, 7);

        // 3. bar_frame_supremo_76x12 em (284,13) 76x12
        Art(root, "bar_frame_supremo_76x12", 284, 13, 76, 12);

        // 4. fill com o nome bar_fill_supremo_68x6 em (288,16) 68x6, pivô (0,1)
        v.Supreme = Art(root, "bar_fill_supremo_68x6", 288, 16, 68, 6);
        v.Supreme.rectTransform.pivot = new Vector2(0, 1);

        // 5. SupremoParticulas em (274,0) 96x30
        v.SupremeParticles = Art(root, "SupremoParticulas", 274, 0, 96, 30);
        if (v.ParticleFrames != null && v.ParticleFrames.Length > 0) v.SupremeParticles.sprite = v.ParticleFrames[0];
        v.SupremeParticles.gameObject.SetActive(false);

        // 6. BannerSupremo em (279,26) 91x19
        v.SupremeBanner = Art(root, "BannerSupremo", 279, 26, 91, 19);
        if (v.BannerKeyFrames != null && v.BannerKeyFrames.Length > 0) v.SupremeBanner.sprite = v.BannerKeyFrames[0];
        v.SupremeBanner.gameObject.SetActive(false);

        // 7. SupremeValue em (supreme=286, 26)
        v.SupremeValueText = HudValue(root, supreme, 26);
        v.SupremeValueText.text = "0 / 100";

        v.Name = HudText(root, 60, 37, 264, 8, 24);
        var title = Label(parent,phase,17,Vector2.zero,new Vector2(700,24));
        title.color = new Color(.92f, .88f, .98f);
        title.gameObject.name = "TituloFase"; Top(title.rectTransform,224);
        return v;
    }
    public static Sprite[] HudFrames(string name, int w, int h)
    {
        var texture = Resources.Load<Texture2D>("HudHorizontal/" + name);
        if (texture == null) return null;
        texture.filterMode = FilterMode.Point;
        texture.wrapMode = TextureWrapMode.Clamp;
        int count = texture.width / w;
        var frames = new Sprite[count];
        for (int i = 0; i < count; i++)
        {
            frames[i] = Sprite.Create(texture, new Rect(i * w, 0, w, h),
                new Vector2(.5f, .5f), 100f, 0, SpriteMeshType.FullRect);
            frames[i].name = name + "_" + i;
        }
        return frames;
    }
    static Sprite HudSprite(string name)
    {
        if (HudSprites.TryGetValue(name, out var sprite)) return sprite;
        var texture = Resources.Load<Texture2D>("HudHorizontal/" + name);
        if (texture == null) return null;
        texture.filterMode = FilterMode.Point;
        texture.wrapMode = TextureWrapMode.Clamp;
        sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height),
            new Vector2(.5f, .5f), 100f, 0, SpriteMeshType.FullRect);
        sprite.name = name;
        HudSprites[name] = sprite;
        return sprite;
    }
    static Image Art(RectTransform root, string name, int x, int y, int w, int h)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(root, false);
        var rect = (RectTransform)go.transform;
        rect.anchorMin = rect.anchorMax = new Vector2(0, 1);
        rect.pivot = new Vector2(0, 1);
        rect.anchoredPosition = new Vector2(x * 5, -y * 5);
        rect.sizeDelta = new Vector2(w * 5, h * 5);
        var image = go.GetComponent<Image>();
        image.sprite = HudSprite(name);
        image.raycastTarget = false;
        return image;
    }
    static Image HudBar(RectTransform root, string kind, int x)
    {
        Art(root, "bar_frame_72x10", x, 14, 72, 10);
        var fill = Art(root, "bar_fill_" + kind + "_68x6", x + 2, 16, 68, 6);
        var rect = fill.rectTransform;
        rect.pivot = new Vector2(0, 1);
        return fill;
    }
    static Text HudValue(RectTransform root, int x, int y) => HudText(root, x, y, 72, 8, 22);
    static Text HudText(RectTransform root, int x, int y, int w, int h, int font)
    {
        var label = UiKit.Label(root, "", font, Vector2.zero, Color.white, new Vector2(w * 5, h * 5));
        var rect = label.rectTransform;
        rect.anchorMin = rect.anchorMax = new Vector2(0, 1);
        rect.pivot = new Vector2(0, 1);
        rect.anchoredPosition = new Vector2(x * 5, -y * 5);
        label.raycastTarget = false;
        return label;
    }
    static Image Bar(Transform parent,string name,string label,float y,Color color,out Text value,out Text caption)
    {
        var fill = UiKit.Bar(parent,name,new Vector2(82,y),new Vector2(BarWidth,28),new Color(.08f,.07f,.14f,1),color);
        var track = fill.transform.parent;
        Outline(track.gameObject,2);
        caption = Label(track,label,16,new Vector2(-98,0),new Vector2(120,26));
        caption.alignment = TextAnchor.MiddleLeft;
        value = Label(track,"",16,new Vector2(54,0),new Vector2(200,26));
        value.alignment = TextAnchor.MiddleRight;
        return fill;
    }
    public static void Footer(Transform parent,string hint)
    {
        var panel = UiKit.Panel(parent,"ControlesRodape",new Vector2(1780,36),Vector2.zero,new Color(.05f,.04f,.09f,.92f));
        panel.rectTransform.anchorMin=panel.rectTransform.anchorMax=new Vector2(.5f,0);
        panel.rectTransform.pivot=new Vector2(.5f,0);panel.rectTransform.anchoredPosition=new Vector2(0,8);
        Label(panel.transform,hint,17,Vector2.zero,new Vector2(1750,34));
    }
    static Text Label(Transform p,string text,int size,Vector2 pos,Vector2 dimensions)
    {
        var label=UiKit.Label(p,text,size,pos,Color.white,dimensions);
        Outline(label.gameObject,1);label.raycastTarget=false;return label;
    }
    static void Top(RectTransform r,float inset)
    {
        r.anchorMin=r.anchorMax=new Vector2(.5f,1);r.pivot=new Vector2(.5f,1);r.anchoredPosition=new Vector2(0,-inset);
    }
    static void Outline(GameObject go,float size)
    {
        var o=go.AddComponent<Outline>();o.effectColor=Color.black;o.effectDistance=new Vector2(size,-size);
    }
    static Sprite Circle()
    {
        if(_circle!=null)return _circle;
        var tex=new Texture2D(80,80,TextureFormat.RGBA32,false){filterMode=FilterMode.Point};
        var pixels=new Color32[6400];
        for(int y=0;y<80;y++)for(int x=0;x<80;x++)pixels[y*80+x]=(new Vector2(x-39.5f,y-39.5f).sqrMagnitude<=1600f)?new Color32(255,255,255,255):new Color32(0,0,0,0);
        tex.SetPixels32(pixels);tex.Apply();_circle=Sprite.Create(tex,new Rect(0,0,80,80),new Vector2(.5f,.5f));return _circle;
    }
    public static Sprite Portrait(CharacterData hero)
    {
        string id=hero!=null?hero.Id:"guerreiro";
        if(Portraits.TryGetValue(id,out var sprite))return sprite;
        string folder=id=="mago"?"Mago":id=="arqueiro"?"Arqueiro":"Guerreiro";
        var tex=Resources.Load<Texture2D>(folder+"/sheet");
        if(tex==null)return null;
        int x0=64,x1=0,y0=64,y1=0;
        if(tex.isReadable)
        {
            var pixels=tex.GetPixels(0,tex.height-64,64,64);
            for(int y=0;y<64;y++)for(int x=0;x<64;x++)if(pixels[y*64+x].a>.1f){x0=Mathf.Min(x0,x);x1=Mathf.Max(x1,x);y0=Mathf.Min(y0,y);y1=Mathf.Max(y1,y);}
        }
        else{x0=12;x1=48;y0=4;y1=40;}
        int width=x1-x0+5;int headHeight=Mathf.CeilToInt((y1-y0+1)*.72f);
        var rect=new Rect(Mathf.Max(0,x0-2),tex.height-64+y1-headHeight+1,width,headHeight+2);
        sprite=Sprite.Create(tex,rect,new Vector2(.5f,.5f),32,0,SpriteMeshType.FullRect);
        sprite.name="Retrato_"+id;Portraits[id]=sprite;return sprite;
    }
}
