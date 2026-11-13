using System;
using System.Collections.Generic;
using UnityEngine;

public static class NazorSheets
{
    public const float Ppu = 32f;
    const string BasePath = "Bosses/DevoradorDoVazio/nazor/";
    const string SheetsPath = BasePath + "final/sheets/";
    const string VfxPath = BasePath + "vfx/";

    static readonly Dictionary<string, Sprite[]> Cache = new Dictionary<string, Sprite[]>();
    static readonly Dictionary<string, VfxEntry> Manifest = new Dictionary<string, VfxEntry>();
    static bool _manifestLoaded;

    public struct VfxEntry
    {
        public string Arquivo;
        public int CelulaW;
        public int CelulaH;
        public int Quadros;
        public float Fps;
        public bool Loop;
        public Vector2 PivotNormalizado;
        public bool Tiled;
    }

    public sealed class FormEntry
    {
        public Vector2 Cell, BodySize, BodyCenter, ContactSize, ContactCenter, Nucleus, NucleusSize, Mouth;
        public Vector2[] Eyes;
    }
    static readonly Dictionary<int, FormEntry> FormCache = new Dictionary<int, FormEntry>();
    public static IReadOnlyDictionary<string, VfxEntry> Entries { get { EnsureManifest(); return Manifest; } }
    public static FormEntry Form(int form)
    {
        form = Mathf.Clamp(form, 1, 3);
        if (FormCache.TryGetValue(form, out var result)) return result;
        var asset = Resources.Load<TextAsset>(BasePath + "final/json/forma" + form);
        if (asset == null) throw new InvalidOperationException("Missing Nazor form JSON: " + form);
        string json = asset.text;
        string body = Block(json, "hurtbox_corpo_sugerida"), core = Block(json, "ponto_fraco_nucleo");
        string contact = Block(json, "hitbox_contato_dano_ao_heroi_sugerida"), origins = Block(json, "pontos_vfx_u_do_centro");
        var eyes = new List<Vector2>();
        string eyeData = ArrayText(origins, "olhos");
        var numbers = System.Text.RegularExpressions.Regex.Matches(eyeData, @"-?\d+(?:\.\d+)?");
        for (int i = 0; i + 1 < numbers.Count; i += 2)
            eyes.Add(new Vector2(ParseNumber(numbers[i].Value), ParseNumber(numbers[i + 1].Value)));
        result = new FormEntry {
            Cell = Pair(json, "celula_px"), BodySize = Pair(Block(json, "hurtbox_corpo_recomendada_final_u"), "tamanho_u"),
            BodyCenter = Pair(body, "centro_u"), ContactSize = Pair(contact, "tamanho_u"), ContactCenter = Pair(contact, "centro_u"),
            Nucleus = Pair(core, "posicao_u_do_centro"), NucleusSize = Pair(core, "hurtbox_ponto_fraco_u"),
            Mouth = Pair(origins, "boca_ou_origem_sonica"), Eyes = eyes.ToArray()
        };
        FormCache[form] = result;
        return result;
    }
    static float ParseNumber(string value) => float.Parse(value, System.Globalization.CultureInfo.InvariantCulture);
    static string Block(string json, string key)
    {
        int start = json.IndexOf("\"" + key + "\"", StringComparison.Ordinal);
        if (start < 0) throw new InvalidOperationException("Missing Nazor JSON property: " + key);
        start = json.IndexOf('{', start);
        return json.Substring(start, FindMatchingBrace(json, start) - start + 1);
    }
    static string ArrayText(string json, string key)
    {
        int start = json.IndexOf("\"" + key + "\"", StringComparison.Ordinal);
        if (start < 0) throw new InvalidOperationException("Missing Nazor array: " + key);
        start = json.IndexOf('[', start);
        int depth = 0;
        for (int i = start; i < json.Length; i++)
        {
            if (json[i] == '[') depth++;
            if (json[i] == ']' && --depth == 0) return json.Substring(start, i - start + 1);
        }
        return "";
    }
    static Vector2 Pair(string json, string key)
    {
        var numbers = System.Text.RegularExpressions.Regex.Matches(ArrayText(json, key), @"-?\d+(?:\.\d+)?");
        return new Vector2(ParseNumber(numbers[0].Value), ParseNumber(numbers[1].Value));
    }
    public static Vector2 Pivot(string resourcePath)
    {
        var frames = Get(resourcePath);
        if (frames == null || frames.Length == 0) return new Vector2(.5f, .5f);
        return new Vector2(frames[0].pivot.x / frames[0].rect.width, frames[0].pivot.y / frames[0].rect.height);
    }

    public static void EnsureManifest()
    {
        if (_manifestLoaded) return;
        _manifestLoaded = true;

        var textAsset = Resources.Load<TextAsset>("Bosses/DevoradorDoVazio/nazor/specs/vfx_manifest");
        if (textAsset == null)
        {
            Debug.LogWarning("[NazorSheets] vfx_manifest não encontrado em Resources");
            return;
        }

        ParseManifest(textAsset.text);
    }

    static void ParseManifest(string json)
    {
        // Parser leve específico para a estrutura conhecida de vfx_manifest.json
        int idx = 0;
        while (idx < json.Length)
        {
            int keyStart = json.IndexOf('"', idx);
            if (keyStart < 0) break;
            int keyEnd = json.IndexOf('"', keyStart + 1);
            if (keyEnd < 0) break;
            string key = json.Substring(keyStart + 1, keyEnd - keyStart - 1);
            int colon = json.IndexOf(':', keyEnd + 1);
            if (colon < 0) break;
            int braceOpen = json.IndexOf('{', colon);
            if (braceOpen < 0) break;

            int braceClose = FindMatchingBrace(json, braceOpen);
            if (braceClose < 0) break;

            string block = json.Substring(braceOpen, braceClose - braceOpen + 1);
            var entry = ParseEntry(block);
            Manifest[key] = entry;

            idx = braceClose + 1;
        }
    }

    static int FindMatchingBrace(string text, int openPos)
    {
        int depth = 0;
        for (int i = openPos; i < text.Length; i++)
        {
            if (text[i] == '{') depth++;
            else if (text[i] == '}')
            {
                depth--;
                if (depth == 0) return i;
            }
        }
        return -1;
    }

    static VfxEntry ParseEntry(string block)
    {
        var e = new VfxEntry { Fps = 12f, Loop = true, PivotNormalizado = new Vector2(0.5f, 0.5f) };
        e.Arquivo = ExtractString(block, "arquivo");
        int[] celula = ExtractIntArray(block, "celula_px");
        if (celula != null && celula.Length >= 2) { e.CelulaW = celula[0]; e.CelulaH = celula[1]; }
        e.Quadros = ExtractInt(block, "quadros", 1);
        e.Fps = ExtractFloat(block, "fps", 12f);
        e.Loop = ExtractBool(block, "loop", true);
        e.Tiled = ExtractBool(block, "tiled", false);
        int[] pivot = ExtractIntArray(block, "pivot_px");
        if (pivot != null && pivot.Length >= 2 && e.CelulaW > 0 && e.CelulaH > 0)
        {
            e.PivotNormalizado = new Vector2((float)pivot[0] / e.CelulaW, 1f - (float)pivot[1] / e.CelulaH);
        }
        return e;
    }

    static string ExtractString(string block, string prop)
    {
        int p = block.IndexOf("\"" + prop + "\"", StringComparison.Ordinal);
        if (p < 0) return null;
        int colon = block.IndexOf(':', p);
        if (colon < 0) return null;
        int q1 = block.IndexOf('"', colon);
        if (q1 < 0) return null;
        int q2 = block.IndexOf('"', q1 + 1);
        if (q2 < 0) return null;
        return block.Substring(q1 + 1, q2 - q1 - 1);
    }

    static int ExtractInt(string block, string prop, int def)
    {
        int p = block.IndexOf("\"" + prop + "\"", StringComparison.Ordinal);
        if (p < 0) return def;
        int colon = block.IndexOf(':', p);
        if (colon < 0) return def;
        int comma = block.IndexOfAny(new[] { ',', '\n', '}', '\r' }, colon + 1);
        if (comma < 0) comma = block.Length;
        string val = block.Substring(colon + 1, comma - colon - 1).Trim();
        return int.TryParse(val, out int res) ? res : def;
    }

    static float ExtractFloat(string block, string prop, float def)
    {
        int p = block.IndexOf("\"" + prop + "\"", StringComparison.Ordinal);
        if (p < 0) return def;
        int colon = block.IndexOf(':', p);
        if (colon < 0) return def;
        int comma = block.IndexOfAny(new[] { ',', '\n', '}', '\r' }, colon + 1);
        if (comma < 0) comma = block.Length;
        string val = block.Substring(colon + 1, comma - colon - 1).Trim();
        return float.TryParse(val, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float res) ? res : def;
    }

    static bool ExtractBool(string block, string prop, bool def)
    {
        int p = block.IndexOf("\"" + prop + "\"", StringComparison.Ordinal);
        if (p < 0) return def;
        int colon = block.IndexOf(':', p);
        if (colon < 0) return def;
        int comma = block.IndexOfAny(new[] { ',', '\n', '}', '\r' }, colon + 1);
        if (comma < 0) comma = block.Length;
        string val = block.Substring(colon + 1, comma - colon - 1).Trim().ToLowerInvariant();
        if (val.StartsWith("true")) return true;
        if (val.StartsWith("false")) return false;
        return def;
    }

    static int[] ExtractIntArray(string block, string prop)
    {
        int p = block.IndexOf("\"" + prop + "\"", StringComparison.Ordinal);
        if (p < 0) return null;
        int bOpen = block.IndexOf('[', p);
        if (bOpen < 0) return null;
        int bClose = block.IndexOf(']', bOpen);
        if (bClose < 0) return null;
        string inside = block.Substring(bOpen + 1, bClose - bOpen - 1);
        var parts = inside.Split(new[] { ',', ' ', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        var list = new List<int>();
        foreach (var part in parts)
        {
            if (int.TryParse(part, out int val)) list.Add(val);
        }
        return list.ToArray();
    }

    public static bool TryGetVfxEntry(string sheetName, out VfxEntry entry)
    {
        EnsureManifest();
        string clean = sheetName.Replace(".png", "");
        clean = clean.Substring(clean.LastIndexOf('/') + 1);
        if (clean.StartsWith(VfxPath)) clean = clean.Substring(VfxPath.Length);
        if (clean.StartsWith("vfx/")) clean = clean.Substring(4);
        return Manifest.TryGetValue(clean, out entry);
    }

    public static Sprite[] Get(string resourcePath)
    {
        if (string.IsNullOrEmpty(resourcePath)) return null;

        // Limpa extensão
        string path = resourcePath.EndsWith(".png") ? resourcePath.Substring(0, resourcePath.Length - 4) : resourcePath;

        if (Cache.TryGetValue(path, out var cached)) return cached;

        // Resolução flexível de caminhos
        Texture2D tex = null;
        string fullPath = path;

        if (!path.Contains("/"))
        {
            // Tenta final/sheets, depois vfx
            tex = Resources.Load<Texture2D>(SheetsPath + path);
            if (tex != null) fullPath = SheetsPath + path;
            else
            {
                tex = Resources.Load<Texture2D>(VfxPath + path);
                if (tex != null) fullPath = VfxPath + path;
            }
        }
        else
        {
            tex = Resources.Load<Texture2D>(path);
            if (tex == null && !path.StartsWith("Bosses/"))
            {
                tex = Resources.Load<Texture2D>(BasePath + path);
                if (tex != null) fullPath = BasePath + path;
            }
        }

        if (tex == null)
        {
            Debug.LogWarning("[NazorSheets] Não foi possível carregar: " + resourcePath);
            return null;
        }

        tex.filterMode = FilterMode.Point;
        tex.wrapMode = TextureWrapMode.Clamp;

        // Determina número de frames
        int frames = ParseFrameCount(path);
        Vector2 pivot = new Vector2(0.5f, 0.5f);

        // Se estiver no manifesto de VFX, obtém dados de lá
        if (TryGetVfxEntry(path, out var vfxEntry))
        {
            if (vfxEntry.Quadros > 0) frames = vfxEntry.Quadros;
            pivot = vfxEntry.PivotNormalizado;
        }

        int fw = tex.width / Mathf.Max(1, frames);
        int fh = tex.height;

        var sprites = new Sprite[frames];
        for (int i = 0; i < frames; i++)
        {
            sprites[i] = Sprite.Create(tex, new Rect(i * fw, 0, fw, fh), pivot, Ppu, 0, SpriteMeshType.FullRect);
            sprites[i].name = tex.name + "_" + i;
        }

        Cache[path] = sprites;
        if (fullPath != path) Cache[fullPath] = sprites;
        return sprites;
    }

    public static Sprite GetFrame(string resourcePath, int frame)
    {
        var sprites = Get(resourcePath);
        if (sprites == null || sprites.Length == 0) return null;
        return sprites[Mathf.Clamp(frame, 0, sprites.Length - 1)];
    }

    static int ParseFrameCount(string name)
    {
        int fIndex = name.LastIndexOf('f');
        if (fIndex > 0)
        {
            int usIndex = name.LastIndexOf('_', fIndex);
            if (usIndex >= 0 && usIndex < fIndex)
            {
                string numStr = name.Substring(usIndex + 1, fIndex - usIndex - 1);
                if (int.TryParse(numStr, out int num) && num > 0)
                    return num;
            }
        }
        return 1;
    }
}

