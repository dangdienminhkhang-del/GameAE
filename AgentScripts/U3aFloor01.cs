using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Script một lần (nằm ngoài Assets): nối P9.7a vào PvE_Gameplay (Tầng 1).
public static class U3aFloor01
{
    private static Type T(string name)
    {
        foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
        {
            var t = asm.GetType(name, false);
            if (t != null) return t;
        }
        throw new Exception("Không thấy kiểu " + name);
    }

    private static GameObject MakeZone(Transform parent, string objName, Vector2 center, Vector2 size, string id, string display, int kind, int priority, string music)
    {
        var go = new GameObject(objName);
        go.transform.SetParent(parent, false);
        go.transform.position = new Vector3(center.x, center.y, 0f);
        var box = go.AddComponent<BoxCollider2D>();
        box.isTrigger = true;
        box.size = size;
        var zv = go.AddComponent(T("Game.PvE.Zones.ZoneVolume"));
        var so = new SerializedObject(zv);
        so.FindProperty("zoneId").stringValue = id;
        so.FindProperty("displayName").stringValue = display;
        so.FindProperty("kind").enumValueIndex = kind;
        so.FindProperty("priority").intValue = priority;
        so.FindProperty("musicId").stringValue = music;
        so.ApplyModifiedPropertiesWithoutUndo();
        return go;
    }

    public static string Run()
    {
        var log = new StringBuilder();
        Scene scene = SceneManager.GetActiveScene();
        var content = GameObject.Find("Floor01/Content");
        if (content == null) return "KHÔNG thấy Floor01/Content";
        if (content.transform.Find("Zones") != null) return "Zones đã có, dừng để không tạo trùng.";

        // 1) Zones dưới nội dung tầng (bị bật/tắt cùng tầng).
        var zones = new GameObject("Zones");
        zones.transform.SetParent(content.transform, false);
        var town = MakeZone(zones.transform, "Zone_Town", new Vector2(0.5f, 0.5f), new Vector2(16f, 10f), "zone.floor_01_town", "Thị trấn", 0, 0, "music.town");
        var farm = MakeZone(zones.transform, "Zone_Farm", new Vector2(33.25f, 3f), new Vector2(49.5f, 42f), "zone.floor_01_farm", "Vùng Farm", 1, 0, "music.gameplay");
        log.AppendLine("Zone_Town center=(0.5,0.5) size=(16,10) -> x[-7.5,8.5] y[-4.5,5.5] (trong Safe Zone x[-8,9] y[-5,6])");
        log.AppendLine("Zone_Farm center=(33.25,3) size=(49.5,42) -> x[8.5,58] y[-18,24]");

        // 2) Spawner: gán spawnZone = Farm.
        var farmVolume = farm.GetComponent(T("Game.PvE.Zones.ZoneVolume"));
        foreach (string n in new[] { "Spawner_FarmA", "Spawner_FarmB", "Spawner_FarmC", "Spawner_Cove" })
        {
            var sp = GameObject.Find("Floor01/Content/" + n);
            var so = new SerializedObject(sp.GetComponent("EnemySpawner"));
            so.FindProperty("spawnZone").objectReferenceValue = farmVolume;
            so.ApplyModifiedPropertiesWithoutUndo();
            log.AppendLine(n + ".spawnZone = Zone_Farm");
        }

        // 3) Hệ điều phối ở ngoài nội dung tầng.
        var sys = new GameObject("ZoneSystem");
        var tracker = sys.AddComponent(T("Game.PvE.Zones.ZoneTracker"));
        var camCtl = sys.AddComponent(T("Game.PvE.Zones.ZoneCameraController"));
        sys.AddComponent(T("Game.PvE.Zones.ZoneMusicRouter"));
        var player = GameObject.FindGameObjectWithTag("Player");
        var tso = new SerializedObject(tracker);
        tso.FindProperty("player").objectReferenceValue = player != null ? player.transform : null;
        tso.ApplyModifiedPropertiesWithoutUndo();
        var cam = GameObject.Find("Main Camera");
        var cso = new SerializedObject(camCtl);
        cso.FindProperty("cameraFollow").objectReferenceValue = cam.GetComponent("CameraFollow");
        cso.ApplyModifiedPropertiesWithoutUndo();
        log.AppendLine("ZoneSystem (gốc scene): ZoneTracker.player=Player, ZoneCameraController.cameraFollow=Main Camera/CameraFollow, ZoneMusicRouter (bosses tự tìm)");

        // 4) Banner trên Canvas HUD.
        var canvas = GameObject.Find("Canvas");
        var refText = GameObject.Find("Canvas/PlayerHUD/LevelText").GetComponent<Text>();
        var banner = new GameObject("ZoneBanner", typeof(RectTransform), typeof(CanvasGroup));
        banner.transform.SetParent(canvas.transform, false);
        var brt = (RectTransform)banner.transform;
        brt.anchorMin = new Vector2(0.5f, 1f); brt.anchorMax = new Vector2(0.5f, 1f); brt.pivot = new Vector2(0.5f, 1f);
        brt.anchoredPosition = new Vector2(0f, -90f); brt.sizeDelta = new Vector2(700f, 80f);
        var lbl = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        lbl.transform.SetParent(banner.transform, false);
        var lrt = (RectTransform)lbl.transform;
        lrt.anchorMin = Vector2.zero; lrt.anchorMax = Vector2.one; lrt.offsetMin = Vector2.zero; lrt.offsetMax = Vector2.zero;
        var text = lbl.GetComponent<Text>();
        text.font = refText.font; text.fontSize = 44; text.fontStyle = FontStyle.Bold;
        text.alignment = TextAnchor.MiddleCenter; text.color = Color.white; text.raycastTarget = false; text.text = "";
        lbl.AddComponent<Shadow>().effectDistance = new Vector2(2f, -2f);
        var zb = banner.AddComponent(T("Game.UI.Zones.ZoneBanner"));
        var bso = new SerializedObject(zb);
        bso.FindProperty("label").objectReferenceValue = text;
        bso.ApplyModifiedPropertiesWithoutUndo();
        banner.GetComponent<CanvasGroup>().alpha = 0f;
        banner.GetComponent<CanvasGroup>().blocksRaycasts = false;
        banner.GetComponent<CanvasGroup>().interactable = false;
        log.AppendLine("Canvas/ZoneBanner: neo trên-giữa (0,-90) 700x80, Text 44 đậm trắng + Shadow, raycastTarget tắt");

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        log.AppendLine("Đã lưu scene " + scene.path);
        return log.ToString();
    }
}
