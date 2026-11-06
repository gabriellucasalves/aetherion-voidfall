using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// Somente apresentação: os valores continuam vindo dos sistemas de combate existentes.
public static class StageHud
{
    public const float BarWidth = 380f;
    public sealed class View
    {
        public Text Name, LifeValue, DefenseValue, DefenseLabel, SpecialValue, Supreme;
        public Image Life, Defense, Special;
    }
    static Sprite _circle;
    static readonly Dictionary<string, Sprite> Portraits = new Dictionary<string, Sprite>();
    public static View Build(Transform parent, CharacterData hero, string phase)
    {
        var panel = UiKit.Panel(parent, "HudCentral", new Vector2(640, 198), Vector2.zero, new Color(.05f,.04f,.08f,.96f));
        Top(panel.rectTransform, 18);
        Outline(panel.gameObject, 2);
        var trim = UiKit.Panel(panel.transform,"Filete",new Vector2(600,2),new Vector2(0,97),new Color(.66f,.58f,.8f));
        trim.raycastTarget = false;
        var v = new View();
        v.Name = Label(panel.transform, "", 18, new Vector2(0,78), new Vector2(590,24));
        var rim = UiKit.Panel(panel.transform, "RetratoBorda", new Vector2(86,86), new Vector2(-239,10), Color.black);
        rim.sprite = Circle();
        var mask = UiKit.Panel(rim.transform,"RetratoMascara",new Vector2(80,80),Vector2.zero,new Color(.19f,.17f,.27f));
        mask.sprite = Circle(); mask.gameObject.AddComponent<Mask>().showMaskGraphic = true;
        var portrait = UiKit.Panel(mask.transform,"Retrato",new Vector2(76,76),Vector2.zero,Color.white);
        portrait.sprite = Portrait(hero); portrait.preserveAspect = true;
        portrait.raycastTarget = false;
        v.Supreme = Label(panel.transform,"",16,new Vector2(-239,-58),new Vector2(136,44));
        v.Supreme.gameObject.SetActive(hero != null && hero.Id != "guerreiro");
        v.Life = Bar(panel.transform,"BarraVida","VIDA",40,new Color(.85f,.15f,.18f),out v.LifeValue,out _);
        bool defense = hero != null && hero.Id != "arqueiro";
        v.Defense = Bar(panel.transform,"BarraEscudo",hero != null && hero.Id == "mago" ? "CAMPO" : "ESCUDO",-8,new Color(.35f,.65f,.95f),out v.DefenseValue,out v.DefenseLabel);
        v.Defense.transform.parent.gameObject.SetActive(defense);
        v.Special = Bar(panel.transform,"BarraEspecial","ESPECIAL",defense ? -56 : -8,new Color(1f,.62f,.25f),out v.SpecialValue,out _);
        // A segunda barra é removida por inteiro para o Arqueiro, sem texto solto.
        var title = Label(parent,phase,20,Vector2.zero,new Vector2(800,30));
        title.gameObject.name = "TituloFase"; Top(title.rectTransform,224);
        return v;
    }
    static Image Bar(Transform parent,string name,string label,float y,Color color,out Text value,out Text caption)
    {
        var fill = UiKit.Bar(parent,name,new Vector2(90,y),new Vector2(BarWidth,40),new Color(.075f,.065f,.105f,1),color);
        var track = fill.transform.parent;
        Outline(track.gameObject,2);
        caption = Label(track,label,20,new Vector2(-115,0),new Vector2(132,36));
        caption.alignment = TextAnchor.MiddleLeft;
        value = Label(track,"",20,new Vector2(70,0),new Vector2(224,36));
        value.alignment = TextAnchor.MiddleRight;
        return fill;
    }
    public static void Footer(Transform parent,string hint)
    {
        var panel = UiKit.Panel(parent,"ControlesRodape",new Vector2(1800,42),Vector2.zero,new Color(.05f,.04f,.08f,.96f));
        panel.rectTransform.anchorMin=panel.rectTransform.anchorMax=new Vector2(.5f,0);
        panel.rectTransform.pivot=new Vector2(.5f,0);panel.rectTransform.anchoredPosition=new Vector2(0,18);
        Label(panel.transform,hint,20,Vector2.zero,new Vector2(1770,40));
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
