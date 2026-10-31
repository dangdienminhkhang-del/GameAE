using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Editor
{
    public static class ApplyBatchE
    {
        private const string FontRegularPath = "Assets/_Game/Art/UI/Fonts/BeVietnamPro-Regular.ttf";
        private const string FontSemiBoldPath = "Assets/_Game/Art/UI/Fonts/BeVietnamPro-SemiBold.ttf";
        private const string FontBoldPath = "Assets/_Game/Art/UI/Fonts/BeVietnamPro-Bold.ttf";

        [MenuItem("Game/UI/Apply Batch E")]
        public static void Execute()
        {
            AssetDatabase.Refresh();

            var fontRegular = AssetDatabase.LoadAssetAtPath<Font>(FontRegularPath);
            var fontSemiBold = AssetDatabase.LoadAssetAtPath<Font>(FontSemiBoldPath) ?? fontRegular;
            var fontBold = AssetDatabase.LoadAssetAtPath<Font>(FontBoldPath) ?? fontSemiBold;

            if (fontSemiBold == null)
            {
                Debug.LogError($"[ApplyBatchE] Không tìm thấy font tại {FontSemiBoldPath}!");
                return;
            }

            Debug.Log($"[ApplyBatchE] Đã tải phông chữ Be Vietnam Pro: Regular={fontRegular != null}, SemiBold={fontSemiBold != null}, Bold={fontBold != null}");

            // 1. Áp dụng cho MainMenu
            ApplyToScene("Assets/_Game/Scenes/MainMenu.unity", fontSemiBold, fontBold);

            // 2. Áp dụng cho PvE_Gameplay
            ApplyToScene("Assets/_Game/Scenes/PvE_Gameplay.unity", fontSemiBold, fontBold);

            // 3. Giữ PvE_Gameplay là scene kích hoạt
            EditorSceneManager.OpenScene("Assets/_Game/Scenes/PvE_Gameplay.unity", OpenSceneMode.Single);

            AssetDatabase.SaveAssets();
            Debug.Log("[ApplyBatchE] Đã áp dụng toàn bộ phông chữ tiếng Việt Be Vietnam Pro cho các Scene!");
        }

        private static void ApplyToScene(string scenePath, Font regularOrSemiBold, Font bold)
        {
            var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            var texts = Object.FindObjectsByType<Text>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            int count = 0;

            foreach (var txt in texts)
            {
                string objName = txt.gameObject.name.ToLower();
                bool isTitleOrBold = objName.Contains("title") 
                                  || objName.Contains("header") 
                                  || objName.Contains("logo") 
                                  || objName.Contains("damage") 
                                  || objName.Contains("rank")
                                  || objName.Contains("notice")
                                  || txt.fontStyle == FontStyle.Bold;

                txt.font = isTitleOrBold ? bold : regularOrSemiBold;
                EditorUtility.SetDirty(txt);
                count++;
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log($"[ApplyBatchE] Đã cập nhật phông chữ cho {count} thành phần Text trong {scene.name}.");
        }
    }
}
