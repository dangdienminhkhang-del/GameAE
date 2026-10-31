using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// Script một lần (nằm ngoài Assets): hoàn tất U5 (toast cách ZoneBanner >= 80px, sprite mũi tên, gán arrowSprite).
public static class U5Fix
{
    public static string Run()
    {
        var sb = new StringBuilder();

        // 1) Toast nằm dưới ZoneBanner (đáy banner ở 170px ref) ít nhất 80px: top offset 260.
        var cfg = AssetDatabase.LoadMainAssetAtPath("Assets/_Game/Data/Guidance/GuidanceConfig.asset");
        var so = new SerializedObject(cfg);
        float before = so.FindProperty("toastTopOffset").floatValue;
        so.FindProperty("toastTopOffset").floatValue = 260f;
        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(cfg);
        sb.AppendLine("GuidanceConfig.toastTopOffset " + before + " -> 260 (banner đáy 170, cách toast 90px)");

        // 2) Sprite mũi tên 64x64 chỉ sang PHẢI (góc 0 = +X), viền mịn bằng siêu lấy mẫu 4x4.
        string path = "Assets/_Game/Art/Placeholder/QuestArrow.png";
        if (!File.Exists(Path.GetFullPath(path)))
        {
            const int n = 64, ss = 4;
            var tex = new Texture2D(n, n, TextureFormat.RGBA32, false);
            var px = new Color32[n * n];
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    int hit = 0;
                    for (int sy = 0; sy < ss; sy++)
                        for (int sx = 0; sx < ss; sx++)
                        {
                            float fx = (x + (sx + 0.5f) / ss) / n;      // 0 trái .. 1 phải
                            float fy = (y + (sy + 0.5f) / ss) / n;      // 0 dưới .. 1 trên
                            // Thân (hình chữ nhật 0.08..0.55 x 0.38..0.62) + đầu (tam giác 0.5..0.95)
                            bool body = fx >= 0.08f && fx <= 0.55f && Mathf.Abs(fy - 0.5f) <= 0.12f;
                            float headHalf = (0.95f - fx) / 0.45f * 0.30f; // nửa bề rộng giảm dần từ 0.30 (fx=0.5) về 0 (đỉnh fx=0.95)
                            bool head = fx >= 0.5f && fx <= 0.95f && Mathf.Abs(fy - 0.5f) <= headHalf;
                            if (body || head) hit++;
                        }
                    byte a = (byte)Mathf.RoundToInt(255f * hit / (ss * ss));
                    px[y * n + x] = new Color32(255, 255, 255, a);
                }
            tex.SetPixels32(px);
            File.WriteAllBytes(Path.GetFullPath(path), tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(path);
        }
        var imp = (TextureImporter)AssetImporter.GetAtPath(path);
        imp.textureType = TextureImporterType.Sprite;
        imp.spriteImportMode = SpriteImportMode.Single;
        imp.spritePixelsPerUnit = 64;
        imp.filterMode = FilterMode.Bilinear;
        imp.textureCompression = TextureImporterCompression.Uncompressed;
        imp.mipmapEnabled = false;
        imp.alphaIsTransparency = true;
        imp.SaveAndReimport();
        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        sb.AppendLine("Sprite " + path + " = " + (sprite != null ? sprite.name : "NULL"));

        // 3) Gán vào QuestArrow trong scene gameplay.
        foreach (var mb in Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (mb.GetType().Name != "QuestArrow") continue;
            var qso = new SerializedObject(mb);
            qso.FindProperty("arrowSprite").objectReferenceValue = sprite;
            qso.ApplyModifiedPropertiesWithoutUndo();
            sb.AppendLine("QuestArrow.arrowSprite = QuestArrow");
        }
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
        AssetDatabase.SaveAssets();
        sb.AppendLine("Đã lưu scene và asset.");
        return sb.ToString();
    }
}
