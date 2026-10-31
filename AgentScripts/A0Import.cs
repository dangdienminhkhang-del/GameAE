using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEngine;

// Script một lần (nằm ngoài Assets): nhập mẫu đồ họa A0 từ docs/files.zip vào Assets/_Game/Art.
public static class A0Import
{
    private const string Art = "Assets/_Game/Art";

    public static string Run()
    {
        var log = new StringBuilder();
        string proj = Directory.GetParent(Application.dataPath).FullName;
        string tmp = Path.Combine(Path.GetTempPath(), "a0_import_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tmp);
        Extract(Path.Combine(proj, "docs", "files.zip"), tmp);
        string sample = Path.Combine(tmp, "sample");
        Extract(Path.Combine(tmp, "Art_A0_Sample.zip"), sample);
        string src = Path.Combine(sample, "Art_A0_Sample");

        string player = Art + "/Characters/Player";
        string enemies = Art + "/Characters/Enemies";
        string props = Art + "/Props";
        string floor = Art + "/Environment/Floor01";
        foreach (string d in new[] { player, enemies, props, floor }) EnsureFolder(d);

        Copy(src, "Player_Body_Idle.png", player + "/Player_Body_Idle_E.png");
        Copy(src, "Player_Body_Walk.png", player + "/Player_Body_Walk_E.png");
        Copy(src, "Player_Weapon_RustedAnchorBlade_Idle.png", player + "/Player_Weapon_RustedAnchorBlade_Idle_E.png");
        Copy(src, "Player_Weapon_RustedAnchorBlade_Walk.png", player + "/Player_Weapon_RustedAnchorBlade_Walk_E.png");
        Composite(player + "/Player_Body_Idle_E.png", player + "/Player_Weapon_RustedAnchorBlade_Idle_E.png", player + "/Player_Composite_Idle_E.png");
        Composite(player + "/Player_Body_Walk_E.png", player + "/Player_Weapon_RustedAnchorBlade_Walk_E.png", player + "/Player_Composite_Walk_E.png");
        Copy(src, "Enemy_Slime_Idle.png", enemies + "/Enemy_Slime_Idle_E.png");
        Copy(src, "Prop_BeaconLamp.png", props + "/Prop_BeaconLamp.png");
        Copy(src, "Tiles_Floor01_Harbor.png", floor + "/Tiles_Floor01_Harbor.png");
        AssetDatabase.Refresh();

        foreach (string n in new[] { "Player_Body_Idle_E", "Player_Body_Walk_E", "Player_Weapon_RustedAnchorBlade_Idle_E", "Player_Weapon_RustedAnchorBlade_Walk_E", "Player_Composite_Idle_E", "Player_Composite_Walk_E" })
            log.AppendLine(Slice(player + "/" + n + ".png", 48, 48, new Vector2(0.5f, 2f / 48f), n));
        log.AppendLine(Slice(enemies + "/Enemy_Slime_Idle_E.png", 32, 32, new Vector2(0.5f, 2f / 32f), "Enemy_Slime_Idle_E"));
        log.AppendLine(Slice(props + "/Prop_BeaconLamp.png", 16, 48, new Vector2(0.5f, 2f / 48f), "Prop_BeaconLamp"));
        log.AppendLine(Slice(floor + "/Tiles_Floor01_Harbor.png", 32, 32, new Vector2(0.5f, 0.5f), "Tiles_Floor01_Harbor"));
        AssetDatabase.SaveAssets();
        return log.ToString();
    }

    private static void Extract(string zipPath, string destDir)
    {
        Directory.CreateDirectory(destDir);
        var psi = new System.Diagnostics.ProcessStartInfo("powershell",
            "-NoProfile -NonInteractive -Command \"Expand-Archive -LiteralPath '" + zipPath + "' -DestinationPath '" + destDir + "' -Force\"")
        { UseShellExecute = false, CreateNoWindow = true, RedirectStandardError = true };
        using (var p = System.Diagnostics.Process.Start(psi))
        {
            string err = p.StandardError.ReadToEnd();
            p.WaitForExit();
            if (p.ExitCode != 0) throw new Exception("Giải nén lỗi: " + err);
        }
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        string parent = Path.GetDirectoryName(path).Replace('\\', '/');
        if (!AssetDatabase.IsValidFolder(parent)) EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
    }

    private static void Copy(string srcDir, string file, string dest)
    {
        string full = Path.GetFullPath(dest);
        File.Copy(Path.Combine(srcDir, file), full, true);
    }

    private static Color32[] Load(string assetPath, out int w, out int h)
    {
        var t = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        t.LoadImage(File.ReadAllBytes(Path.GetFullPath(assetPath)), false);
        w = t.width; h = t.height;
        Color32[] px = t.GetPixels32();
        UnityEngine.Object.DestroyImmediate(t);
        return px;
    }

    private static void Composite(string bodyPath, string weaponPath, string outPath)
    {
        Color32[] b = Load(bodyPath, out int bw, out int bh);
        Color32[] w = Load(weaponPath, out int ww, out int wh);
        if (bw != ww || bh != wh) throw new Exception("Khác kích thước: " + bodyPath);
        var o = new Color32[b.Length];
        for (int i = 0; i < b.Length; i++)
        {
            float wa = w[i].a / 255f, ba = b[i].a / 255f;
            float oa = wa + ba * (1f - wa);
            if (oa <= 0f) { o[i] = new Color32(0, 0, 0, 0); continue; }
            float r = (w[i].r * wa + b[i].r * ba * (1f - wa)) / oa;
            float g = (w[i].g * wa + b[i].g * ba * (1f - wa)) / oa;
            float bl = (w[i].b * wa + b[i].b * ba * (1f - wa)) / oa;
            o[i] = new Color32((byte)Mathf.Round(r), (byte)Mathf.Round(g), (byte)Mathf.Round(bl), (byte)Mathf.Round(oa * 255f));
        }
        var t = new Texture2D(bw, bh, TextureFormat.RGBA32, false);
        t.SetPixels32(o);
        File.WriteAllBytes(Path.GetFullPath(outPath), t.EncodeToPNG());
        UnityEngine.Object.DestroyImmediate(t);
    }

    private static string Slice(string path, int fw, int fh, Vector2 pivot, string baseName)
    {
        var imp = (TextureImporter)AssetImporter.GetAtPath(path);
        if (imp == null) return "KHÔNG THẤY importer: " + path;
        imp.textureType = TextureImporterType.Sprite;
        imp.spriteImportMode = SpriteImportMode.Multiple;
        imp.spritePixelsPerUnit = 32;
        imp.filterMode = FilterMode.Point;
        imp.textureCompression = TextureImporterCompression.Uncompressed;
        imp.mipmapEnabled = false;
        imp.alphaIsTransparency = true;
        var s = new TextureImporterSettings();
        imp.ReadTextureSettings(s);
        s.spriteMeshType = SpriteMeshType.FullRect;
        s.spriteAlignment = (int)SpriteAlignment.Custom;
        s.spritePivot = pivot;
        imp.SetTextureSettings(s);
        imp.SaveAndReimport();

        var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        int cols = tex.width / fw;
        var factory = new SpriteDataProviderFactories();
        factory.Init();
        var dp = factory.GetSpriteEditorDataProviderFromObject(imp);
        dp.InitSpriteEditorDataProvider();
        var rects = new List<SpriteRect>();
        for (int i = 0; i < cols; i++)
        {
            rects.Add(new SpriteRect
            {
                name = baseName + "_" + i,
                rect = new Rect(i * fw, 0, fw, fh),
                alignment = SpriteAlignment.Custom,
                pivot = pivot,
                spriteID = GUID.Generate()
            });
        }
        dp.SetSpriteRects(rects.ToArray());
        var nameProv = dp.GetDataProvider<ISpriteNameFileIdDataProvider>();
        nameProv.SetNameFileIdPairs(rects.Select(r => new SpriteNameFileIdPair(r.name, r.spriteID)));
        dp.Apply();
        imp.SaveAndReimport();
        return path + " -> " + cols + " khung " + fw + "x" + fh;
    }
}
