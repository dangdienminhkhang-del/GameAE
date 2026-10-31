using System.IO;
using Game.UI.Common;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Editor
{
    public static class ApplyPackageU1
    {
        [MenuItem("Game/UI/Apply Package U1")]
        public static void Execute()
        {
            ApplyToPvEGameplay();
            ApplyToMainMenu();
            ApplyToBoot();
            EditorSceneManager.OpenScene("Assets/_Game/Scenes/PvE_Gameplay.unity", OpenSceneMode.Single);
            Debug.Log("[ApplyPackageU1] Successfully applied Package U1 to PvE_Gameplay, MainMenu, and Boot!");
        }

        public static void ApplyToPvEGameplay()
        {
            var scene = EditorSceneManager.OpenScene("Assets/_Game/Scenes/PvE_Gameplay.unity", OpenSceneMode.Single);
            
            // 1. Configure CanvasScalers
            var canvases = Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            Canvas mainCanvas = null;
            foreach (var c in canvases)
            {
                var scaler = c.GetComponent<CanvasScaler>();
                if (scaler != null)
                {
                    scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                    scaler.referenceResolution = UiLayoutConstants.ReferenceResolution;
                    scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
                    scaler.matchWidthOrHeight = UiLayoutConstants.MatchWidthOrHeight;
                    EditorUtility.SetDirty(scaler);
                }

                if (c.name == "Canvas") mainCanvas = c;
            }

            if (mainCanvas == null)
            {
                Debug.LogError("[ApplyPackageU1] Main Canvas not found in PvE_Gameplay!");
                return;
            }

            // 2. Ensure SafeAreaRoot
            Transform safeAreaRootTf = mainCanvas.transform.Find("SafeAreaRoot");
            GameObject safeAreaRootObj;
            if (safeAreaRootTf == null)
            {
                safeAreaRootObj = new GameObject("SafeAreaRoot", typeof(RectTransform));
                safeAreaRootObj.transform.SetParent(mainCanvas.transform, false);
            }
            else
            {
                safeAreaRootObj = safeAreaRootTf.gameObject;
            }

            var safeRt = safeAreaRootObj.GetComponent<RectTransform>();
            safeRt.anchorMin = Vector2.zero;
            safeRt.anchorMax = Vector2.one;
            safeRt.offsetMin = Vector2.zero;
            safeRt.offsetMax = Vector2.zero;
            safeRt.pivot = new Vector2(0.5f, 0.5f);

            if (!safeAreaRootObj.TryGetComponent(out SafeAreaFitter _))
            {
                safeAreaRootObj.AddComponent<SafeAreaFitter>();
            }

            // Put SafeAreaRoot just after JoystickTouchZone
            Transform touchZoneTf = mainCanvas.transform.Find("JoystickTouchZone");
            if (touchZoneTf != null)
            {
                safeAreaRootObj.transform.SetSiblingIndex(touchZoneTf.GetSiblingIndex() + 1);
            }

            // 3. Move edge-hugging HUD elements to SafeAreaRoot
            string[] hudElementsToMove = new[]
            {
                "JoystickPanel",
                "AttackButton",
                "PlayerHUD",
                "InventoryButton",
                "FloorButton",
                "PauseButton",
                "PotionButton",
                "PotionStatusLabel",
                "PotionToastLabel",
                "TimeLabel",
                "TalkButton",
                "QuestButton",
                "TechniqueButton",
                "TechniqueButton (3)",
                "TechniqueButton (2)",
                "TechniqueButton (1)",
                "DodgeButton",
                "BossHealthBar",
                "ZoneBanner",
                "WalletHud",
                "CharacterButton"
            };

            foreach (string elemName in hudElementsToMove)
            {
                Transform t = mainCanvas.transform.Find(elemName);
                if (t != null)
                {
                    t.SetParent(safeAreaRootObj.transform, false);
                    EditorUtility.SetDirty(t);
                }
            }

            EditorUtility.SetDirty(safeAreaRootObj);
            EditorUtility.SetDirty(mainCanvas.gameObject);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[ApplyPackageU1] Applied to PvE_Gameplay.");
        }

        public static void ApplyToMainMenu()
        {
            var scene = EditorSceneManager.OpenScene("Assets/_Game/Scenes/MainMenu.unity", OpenSceneMode.Single);

            var canvases = Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            Canvas mainCanvas = null;
            foreach (var c in canvases)
            {
                var scaler = c.GetComponent<CanvasScaler>();
                if (scaler != null)
                {
                    scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                    scaler.referenceResolution = UiLayoutConstants.ReferenceResolution;
                    scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
                    scaler.matchWidthOrHeight = UiLayoutConstants.MatchWidthOrHeight;
                    EditorUtility.SetDirty(scaler);
                }

                if (c.name == "Canvas") mainCanvas = c;
            }

            if (mainCanvas != null)
            {
                Transform safeAreaRootTf = mainCanvas.transform.Find("SafeAreaRoot");
                GameObject safeAreaRootObj;
                if (safeAreaRootTf == null)
                {
                    safeAreaRootObj = new GameObject("SafeAreaRoot", typeof(RectTransform));
                    safeAreaRootObj.transform.SetParent(mainCanvas.transform, false);
                }
                else
                {
                    safeAreaRootObj = safeAreaRootTf.gameObject;
                }

                var safeRt = safeAreaRootObj.GetComponent<RectTransform>();
                safeRt.anchorMin = Vector2.zero;
                safeRt.anchorMax = Vector2.one;
                safeRt.offsetMin = Vector2.zero;
                safeRt.offsetMax = Vector2.zero;
                safeRt.pivot = new Vector2(0.5f, 0.5f);

                if (!safeAreaRootObj.TryGetComponent(out SafeAreaFitter _))
                {
                    safeAreaRootObj.AddComponent<SafeAreaFitter>();
                }

                string[] mainMenuElements = new[]
                {
                    "ResetGuestButton",
                    "PlayPvEButton",
                    "PlayPvPButton",
                    "SettingsButton",
                    "QuitButton",
                    "ButtonContainer",
                    "Buttons"
                };

                foreach (string elemName in mainMenuElements)
                {
                    Transform t = mainCanvas.transform.Find(elemName);
                    if (t != null)
                    {
                        t.SetParent(safeAreaRootObj.transform, false);
                        EditorUtility.SetDirty(t);
                    }
                }

                EditorUtility.SetDirty(safeAreaRootObj);
                EditorUtility.SetDirty(mainCanvas.gameObject);
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[ApplyPackageU1] Applied to MainMenu.");
        }

        public static void ApplyToBoot()
        {
            var scene = EditorSceneManager.OpenScene("Assets/_Game/Scenes/Boot.unity", OpenSceneMode.Single);

            var canvases = Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var c in canvases)
            {
                var scaler = c.GetComponent<CanvasScaler>();
                if (scaler != null)
                {
                    scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                    scaler.referenceResolution = UiLayoutConstants.ReferenceResolution;
                    scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
                    scaler.matchWidthOrHeight = UiLayoutConstants.MatchWidthOrHeight;
                    EditorUtility.SetDirty(scaler);
                }
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[ApplyPackageU1] Applied to Boot.");
        }
    }
}
