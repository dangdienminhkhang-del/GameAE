using System.IO;
using Game.UI.Common;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Editor
{
    public static class ApplyPackageU4
    {
        [MenuItem("Game/UI/Apply Package U4")]
        public static void Execute()
        {
            ApplyToMainMenu();
            ApplyToBoot();
            EditorSceneManager.OpenScene("Assets/_Game/Scenes/PvE_Gameplay.unity", OpenSceneMode.Single);
            Debug.Log("[ApplyPackageU4] Successfully applied Package U4 to MainMenu and Boot!");
        }

        public static void ApplyToMainMenu()
        {
            var scene = EditorSceneManager.OpenScene("Assets/_Game/Scenes/MainMenu.unity", OpenSceneMode.Single);
            var canvasObj = GameObject.Find("Canvas");
            if (canvasObj == null) return;

            // 1. CanvasScaler reference 1920x1080, Match = 1
            var scaler = canvasObj.GetComponent<CanvasScaler>();
            if (scaler != null)
            {
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = UiLayoutConstants.ReferenceResolution;
                scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
                scaler.matchWidthOrHeight = UiLayoutConstants.MatchWidthOrHeight;
            }

            // 2. Fullscreen Background under Canvas
            Transform bgTf = canvasObj.transform.Find("Background");
            if (bgTf == null)
            {
                var bgObj = new GameObject("Background", typeof(RectTransform), typeof(Image), typeof(AspectRatioFitter));
                bgObj.transform.SetParent(canvasObj.transform, false);
                bgObj.transform.SetAsFirstSibling();
                var bgRt = bgObj.GetComponent<RectTransform>();
                bgRt.anchorMin = Vector2.zero;
                bgRt.anchorMax = Vector2.one;
                bgRt.offsetMin = Vector2.zero;
                bgRt.offsetMax = Vector2.zero;
                var arf = bgObj.GetComponent<AspectRatioFitter>();
                arf.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
                arf.aspectRatio = 16f / 9f;
                bgObj.GetComponent<Image>().color = new Color(0.06f, 0.08f, 0.12f, 1f);
                bgObj.SetActive(false); // Sẵn sàng cho Gói U6 gán sprite
            }

            // 3. SafeAreaRoot
            Transform safeAreaRootTf = canvasObj.transform.Find("SafeAreaRoot");
            if (safeAreaRootTf == null)
            {
                var sarObj = new GameObject("SafeAreaRoot", typeof(RectTransform), typeof(SafeAreaFitter));
                sarObj.transform.SetParent(canvasObj.transform, false);
                safeAreaRootTf = sarObj.transform;
            }

            // 4. LogoEmblem & TitlePlate (placeholder objects for U6)
            Transform logoTf = safeAreaRootTf.Find("LogoEmblem");
            if (logoTf == null)
            {
                var logoObj = new GameObject("LogoEmblem", typeof(RectTransform), typeof(Image));
                logoObj.transform.SetParent(safeAreaRootTf, false);
                var lrt = logoObj.GetComponent<RectTransform>();
                lrt.anchorMin = lrt.anchorMax = lrt.pivot = new Vector2(0.5f, 1f);
                lrt.anchoredPosition = new Vector2(0f, -60f);
                lrt.sizeDelta = new Vector2(260f, 260f);
                logoObj.SetActive(false);
            }

            Transform plateTf = safeAreaRootTf.Find("TitlePlate");
            if (plateTf == null)
            {
                var plateObj = new GameObject("TitlePlate", typeof(RectTransform), typeof(Image));
                plateObj.transform.SetParent(safeAreaRootTf, false);
                var prt = plateObj.GetComponent<RectTransform>();
                prt.anchorMin = prt.anchorMax = prt.pivot = new Vector2(0.5f, 1f);
                prt.anchoredPosition = new Vector2(0f, -330f);
                prt.sizeDelta = new Vector2(620f, 140f);
                plateObj.SetActive(false);
            }

            // 5. ResetGuestButton (anchor top-left, height >= 120)
            Transform resetBtnTf = safeAreaRootTf.Find("ResetGuestButton");
            if (resetBtnTf != null)
            {
                var rrt = resetBtnTf.GetComponent<RectTransform>();
                rrt.anchorMin = rrt.anchorMax = rrt.pivot = new Vector2(0f, 1f);
                rrt.anchoredPosition = new Vector2(24f, -24f);
                rrt.sizeDelta = new Vector2(280f, 120f);
                var txt = resetBtnTf.GetComponentInChildren<Text>();
                if (txt != null) txt.fontSize = 30;
            }

            // 6. Menu Button Column (PlayPvE, PlayPvP, Settings, Quit)
            SetupMenuButton(safeAreaRootTf, "PlayPvEButton", 90f);
            SetupMenuButton(safeAreaRootTf, "PlayPvPButton", -50f);
            SetupMenuButton(safeAreaRootTf, "SettingsButton", -190f);
            SetupMenuButton(safeAreaRootTf, "QuitButton", -330f);

            // 7. SettingsPanel clamp
            Transform settingsTf = canvasObj.transform.Find("SettingsPanel");
            if (settingsTf != null)
            {
                Transform boxTf = settingsTf.Find("Root/Box") ?? settingsTf.Find("Box") ?? settingsTf;
                var clamp = boxTf.GetComponent<PanelSizeClamp>();
                if (clamp == null) clamp = boxTf.gameObject.AddComponent<PanelSizeClamp>();
                clamp.DesignSize = new Vector2(860f, 780f);

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

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[ApplyPackageU4] MainMenu updated successfully.");
        }

        private static void SetupMenuButton(Transform parent, string name, float posY)
        {
            Transform t = parent.Find(name);
            if (t == null) return;
            var rt = t.GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = new Vector2(0f, posY);
                rt.sizeDelta = new Vector2(500f, 120f);
            }
            var txt = t.GetComponentInChildren<Text>();
            if (txt != null) txt.fontSize = 36;
        }

        public static void ApplyToBoot()
        {
            var scene = EditorSceneManager.OpenScene("Assets/_Game/Scenes/Boot.unity", OpenSceneMode.Single);
            var canvasObj = GameObject.Find("Canvas");
            if (canvasObj != null)
            {
                var scaler = canvasObj.GetComponent<CanvasScaler>();
                if (scaler != null)
                {
                    scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                    scaler.referenceResolution = UiLayoutConstants.ReferenceResolution;
                    scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
                    scaler.matchWidthOrHeight = UiLayoutConstants.MatchWidthOrHeight;
                }
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[ApplyPackageU4] Boot updated successfully.");
        }
    }
}
