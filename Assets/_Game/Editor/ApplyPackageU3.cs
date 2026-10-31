using System.IO;
using Game.UI.Common;
using Game.UI.Floor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Editor
{
    public static class ApplyPackageU3
    {
        [MenuItem("Game/UI/Apply Package U3")]
        public static void Execute()
        {
            ApplyPrefabFloorSelectRow();
            ApplyToPvEGameplay();
            EditorSceneManager.OpenScene("Assets/_Game/Scenes/PvE_Gameplay.unity", OpenSceneMode.Single);
            Debug.Log("[ApplyPackageU3] Successfully applied Package U3!");
        }

        public static void ApplyPrefabFloorSelectRow()
        {
            string prefabPath = "Assets/_Game/Prefabs/UI/FloorSelectRow.prefab";
            var root = PrefabUtility.LoadPrefabContents(prefabPath);
            try
            {
                var rt = root.GetComponent<RectTransform>();
                if (rt != null)
                {
                    rt.sizeDelta = new Vector2(rt.sizeDelta.x, 120f);
                }

                var le = root.GetComponent<LayoutElement>();
                if (le == null) le = root.AddComponent<LayoutElement>();
                le.minHeight = 120f;
                le.preferredHeight = 120f;

                var texts = root.GetComponentsInChildren<Text>(true);
                foreach (var txt in texts)
                {
                    if (txt.name.Contains("FloorName") || txt.name.Contains("Name"))
                    {
                        txt.fontSize = 32;
                        txt.fontStyle = FontStyle.Bold;
                    }
                    else if (txt.name.Contains("Status") || txt.name.Contains("Level"))
                    {
                        txt.fontSize = 28;
                    }
                    else if (txt.name.Contains("Text") || txt.name.Contains("Select") || txt.name.Contains("Label"))
                    {
                        txt.fontSize = 30;
                    }
                }

                var btn = root.GetComponentInChildren<Button>(true);
                if (btn != null)
                {
                    var btnRt = btn.GetComponent<RectTransform>();
                    if (btnRt != null)
                    {
                        btnRt.sizeDelta = new Vector2(Mathf.Max(btnRt.sizeDelta.x, 160f), 120f);
                    }
                }

                PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
                Debug.Log("[ApplyPackageU3] Updated FloorSelectRow.prefab: height=120, fontSize>=30, selectButton height=120.");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        public static void ApplyToPvEGameplay()
        {
            var scene = EditorSceneManager.OpenScene("Assets/_Game/Scenes/PvE_Gameplay.unity", OpenSceneMode.Single);
            var canvasObj = GameObject.Find("Canvas");

            // 1. SettingsPanel Box clamp
            Transform settingsTf = canvasObj.transform.Find("SettingsPanel");
            if (settingsTf != null)
            {
                Transform boxTf = settingsTf.Find("Box") ?? settingsTf;
                EnsurePanelClamp(boxTf, new Vector2(860f, 780f));
                Transform closeTf = boxTf.Find("CloseButton");
                if (closeTf != null)
                {
                    var crt = closeTf.GetComponent<RectTransform>();
                    crt.anchorMin = crt.anchorMax = crt.pivot = new Vector2(1f, 1f);
                    crt.anchoredPosition = new Vector2(-10f, -10f);
                    crt.sizeDelta = new Vector2(120f, 120f);
                    var txt = closeTf.GetComponentInChildren<Text>();
                    if (txt != null) { txt.text = "X"; txt.fontSize = 36; }
                }
            }

            // 2. PausePanel Box clamp
            Transform pauseTf = canvasObj.transform.Find("PausePanel");
            if (pauseTf != null)
            {
                Transform boxTf = pauseTf.Find("Box") ?? pauseTf;
                EnsurePanelClamp(boxTf, new Vector2(760f, 760f));
                foreach (var b in boxTf.GetComponentsInChildren<Button>(true))
                {
                    var brt = b.GetComponent<RectTransform>();
                    if (brt != null) brt.sizeDelta = new Vector2(Mathf.Max(brt.sizeDelta.x, 360f), 120f);
                    var txt = b.GetComponentInChildren<Text>();
                    if (txt != null) txt.fontSize = 32;
                }
            }

            // 3. QuestPanel center + clamp + Close button
            Transform questTf = canvasObj.transform.Find("QuestPanel");
            if (questTf != null)
            {
                var qrt = questTf.GetComponent<RectTransform>();
                qrt.anchorMin = qrt.anchorMax = qrt.pivot = new Vector2(0.5f, 0.5f);
                qrt.anchoredPosition = Vector2.zero;
                qrt.sizeDelta = new Vector2(860f, 680f);
                EnsurePanelClamp(questTf, new Vector2(860f, 680f));

                Transform titleTf = questTf.Find("Title");
                if (titleTf != null)
                {
                    var trt = titleTf.GetComponent<RectTransform>();
                    trt.anchorMin = new Vector2(0f, 1f);
                    trt.anchorMax = new Vector2(1f, 1f);
                    trt.pivot = new Vector2(0f, 1f);
                    trt.offsetMin = new Vector2(24f, -70f);
                    trt.offsetMax = new Vector2(-140f, -10f);
                    var txt = titleTf.GetComponent<Text>();
                    if (txt != null) { txt.fontSize = 44; txt.fontStyle = FontStyle.Bold; }
                }

                Transform closeTf = questTf.Find("CloseButton");
                if (closeTf != null)
                {
                    var crt = closeTf.GetComponent<RectTransform>();
                    crt.anchorMin = crt.anchorMax = crt.pivot = new Vector2(1f, 1f);
                    crt.anchoredPosition = new Vector2(-10f, -10f);
                    crt.sizeDelta = new Vector2(120f, 120f);
                    var txt = closeTf.GetComponentInChildren<Text>();
                    if (txt != null) { txt.text = "X"; txt.fontSize = 36; }
                }

                Transform acceptTf = questTf.Find("AcceptButton");
                if (acceptTf != null)
                {
                    var art = acceptTf.GetComponent<RectTransform>();
                    art.sizeDelta = new Vector2(art.sizeDelta.x, 120f);
                    var txt = acceptTf.GetComponentInChildren<Text>();
                    if (txt != null) txt.fontSize = 32;
                }

                Transform turnInTf = questTf.Find("TurnInButton");
                if (turnInTf != null)
                {
                    var trt = turnInTf.GetComponent<RectTransform>();
                    trt.sizeDelta = new Vector2(trt.sizeDelta.x, 120f);
                    var txt = turnInTf.GetComponentInChildren<Text>();
                    if (txt != null) txt.fontSize = 32;
                }

                Transform bodyTf = questTf.Find("Body");
                if (bodyTf != null)
                {
                    var txt = bodyTf.GetComponent<Text>();
                    if (txt != null) txt.fontSize = 28;
                }
            }

            // 4. FloorSelectCanvas PanelRoot center + clamp + Close button
            var floorCanvasObj = GameObject.Find("FloorSelectCanvas");
            if (floorCanvasObj != null)
            {
                Transform panelRootTf = floorCanvasObj.transform.Find("PanelRoot");
                if (panelRootTf != null)
                {
                    var prt = panelRootTf.GetComponent<RectTransform>();
                    prt.anchorMin = prt.anchorMax = prt.pivot = new Vector2(0.5f, 0.5f);
                    prt.anchoredPosition = Vector2.zero;
                    prt.sizeDelta = new Vector2(860f, 680f);
                    EnsurePanelClamp(panelRootTf, new Vector2(860f, 680f));

                    Transform headerTf = panelRootTf.Find("Header");
                    if (headerTf != null)
                    {
                        var hrt = headerTf.GetComponent<RectTransform>();
                        hrt.anchorMin = new Vector2(0f, 1f);
                        hrt.anchorMax = new Vector2(1f, 1f);
                        hrt.pivot = new Vector2(0f, 1f);
                        hrt.offsetMin = new Vector2(24f, -70f);
                        hrt.offsetMax = new Vector2(-140f, -10f);
                        var txt = headerTf.GetComponentInChildren<Text>();
                        if (txt != null) { txt.fontSize = 44; txt.fontStyle = FontStyle.Bold; }
                    }

                    Transform closeTf = panelRootTf.Find("CloseButton");
                    if (closeTf != null)
                    {
                        var crt = closeTf.GetComponent<RectTransform>();
                        crt.anchorMin = crt.anchorMax = crt.pivot = new Vector2(1f, 1f);
                        crt.anchoredPosition = new Vector2(-10f, -10f);
                        crt.sizeDelta = new Vector2(120f, 120f);
                        var txt = closeTf.GetComponentInChildren<Text>();
                        if (txt != null) { txt.text = "X"; txt.fontSize = 36; }
                    }

                    Transform listContainer = panelRootTf.Find("ListContainer");
                    if (listContainer != null)
                    {
                        var lrt = listContainer.GetComponent<RectTransform>();
                        lrt.anchorMin = Vector2.zero;
                        lrt.anchorMax = Vector2.one;
                        lrt.offsetMin = new Vector2(20f, 20f);
                        lrt.offsetMax = new Vector2(-20f, -90f);
                    }
                }
            }

            // 5. InventoryPanel PanelRoot center + clamp
            Transform invTf = canvasObj.transform.Find("InventoryPanel");
            if (invTf != null)
            {
                var irt = invTf.GetComponent<RectTransform>();
                irt.anchorMin = irt.anchorMax = irt.pivot = new Vector2(0.5f, 0.5f);
                irt.anchoredPosition = Vector2.zero;
                irt.sizeDelta = new Vector2(860f, 720f);
                EnsurePanelClamp(invTf, new Vector2(860f, 720f));
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[ApplyPackageU3] Applied panels clamp and unified layout to PvE_Gameplay.");
        }

        private static void EnsurePanelClamp(Transform panelTf, Vector2 designSize)
        {
            if (panelTf == null) return;
            var clamp = panelTf.GetComponent<PanelSizeClamp>();
            if (clamp == null) clamp = panelTf.gameObject.AddComponent<PanelSizeClamp>();
            clamp.DesignSize = designSize;
            EditorUtility.SetDirty(panelTf.gameObject);
        }
    }
}
