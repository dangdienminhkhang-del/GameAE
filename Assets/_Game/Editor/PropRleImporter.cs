#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Game.EditorTools
{
    /// <summary>Giải mã sprite vật thể từ props.rle.txt (RLE theo bảng màu ấm khớp nhà/cây), xuất PNG 20 PPU, pivot đáy giữa.</summary>
    public static class PropRleImporter
    {
        const string Src = "Assets/_Game/Art/Environment/Anime/World/props.rle.txt";
        const string Dir = "Assets/_Game/Art/Environment/Anime/World/Props";
        static readonly Dictionary<char, Color32> Pal = Build();

        static Dictionary<char, Color32> Build()
        {
            var d = new Dictionary<char, Color32>();
            void A(char c, int r, int g, int b, int a = 255) { d[c] = new Color32((byte)r, (byte)g, (byte)b, (byte)a); }
            A('O', 43, 27, 31); A('a', 74, 60, 64); A('b', 110, 94, 92); A('c', 151, 134, 118); A('d', 187, 169, 143); A('e', 222, 206, 176);
            A('f', 31, 95, 143); A('g', 47, 134, 184); A('h', 93, 180, 214); A('i', 168, 228, 238); A('j', 255, 255, 255);
            A('k', 58, 34, 22); A('l', 92, 58, 34); A('m', 132, 85, 47); A('n', 168, 122, 69);
            A('p', 122, 31, 38); A('q', 168, 46, 51); A('r', 212, 74, 68); A('s', 232, 217, 184); A('t', 246, 236, 210);
            A('u', 184, 138, 46); A('v', 232, 184, 74); A('w', 255, 224, 138);
            A('x', 168, 116, 42); A('y', 209, 154, 60); A('z', 236, 192, 96); A('A', 248, 222, 136);
            A('B', 196, 54, 26); A('C', 242, 122, 36); A('D', 255, 192, 64); A('E', 255, 242, 160);
            A('F', 45, 42, 51); A('G', 75, 71, 85); A('H', 111, 106, 122);
            A('I', 46, 94, 46); A('J', 74, 138, 56); A('K', 112, 176, 76); A('L', 74, 46, 32); A('M', 106, 68, 48);
            A('N', 232, 90, 138); A('P', 246, 211, 74); A('Q', 168, 112, 224);
            A('R', 47, 51, 66); A('S', 75, 80, 104); A('T', 109, 115, 144); A('U', 154, 160, 188); A('V', 196, 201, 222);
            A('W', 60, 120, 210); A('X', 110, 180, 250); A('Y', 190, 230, 255);
            A('Z', 26, 16, 32, 92); A('o', 86, 120, 60);
            return d;
        }

        [MenuItem("Game/Import World Props (RLE)")]
        public static string Import()
        {
            if (!AssetDatabase.IsValidFolder(Dir)) AssetDatabase.CreateFolder("Assets/_Game/Art/Environment/Anime/World", "Props");
            var lines = File.ReadAllLines(Src);
            var log = new StringBuilder();
            for (int i = 0; i + 1 < lines.Length; i += 2)
            {
                var head = lines[i].Trim().Split(' ');
                if (head.Length < 3) { i--; continue; }
                string name = head[0]; int w = int.Parse(head[1]), h = int.Parse(head[2]);
                var rows = lines[i + 1].Trim().Split('|');
                if (rows.Length != h) { log.Append(name + " ROWS " + rows.Length + "!=" + h + "; "); continue; }
                var px = new Color32[w * h]; bool bad = false;
                for (int y = 0; y < h && !bad; y++)
                {
                    int x = 0; var r = rows[y]; int k = 0;
                    while (k < r.Length)
                    {
                        char c = r[k++]; int n = 0; bool has = false;
                        while (k < r.Length && char.IsDigit(r[k])) { n = n * 10 + (r[k++] - '0'); has = true; }
                        if (!has) n = 1;
                        for (int t = 0; t < n; t++)
                        {
                            if (x >= w) { bad = true; break; }
                            if (c != '.') { if (!Pal.TryGetValue(c, out var col)) { bad = true; log.Append(name + " sym " + c + "; "); break; } px[(h - 1 - y) * w + x] = col; }
                            x++;
                        }
                        if (bad) break;
                    }
                    if (x != w) { bad = true; log.Append(name + " row" + y + " width " + x + "!=" + w + "; "); }
                }
                if (bad) continue;
                var tex = new Texture2D(w, h, TextureFormat.RGBA32, false); tex.SetPixels32(px); tex.Apply();
                string path = Dir + "/" + name + ".png";
                File.WriteAllBytes(path, tex.EncodeToPNG()); Object.DestroyImmediate(tex);
                log.Append(name + " ok; ");
            }
            AssetDatabase.Refresh();
            foreach (var g in AssetDatabase.FindAssets("t:Texture2D", new[] { Dir }))
            {
                var p = AssetDatabase.GUIDToAssetPath(g);
                var ti = (TextureImporter)AssetImporter.GetAtPath(p);
                ti.textureType = TextureImporterType.Sprite; ti.spriteImportMode = SpriteImportMode.Single;
                ti.spritePixelsPerUnit = 20; ti.filterMode = FilterMode.Point; ti.textureCompression = TextureImporterCompression.Uncompressed;
                ti.mipmapEnabled = false; ti.alphaIsTransparency = true;
                var s = new TextureImporterSettings(); ti.ReadTextureSettings(s); s.spriteAlignment = (int)SpriteAlignment.BottomCenter; ti.SetTextureSettings(s);
                ti.SaveAndReimport();
            }
            Debug.Log("[PropRle] " + log);
            return log.ToString();
        }
    }
}
#endif
