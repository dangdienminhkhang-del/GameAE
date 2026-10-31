using System;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// Script một lần (nằm ngoài Assets): nối P9.7a vào Floor02.unity (cùng quy ước với Tầng 1).
public static class U3aFloor02
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

    private static Component MakeZone(Transform parent, string objName, Vector2 center, Vector2 size, string id, string display, int kind, string music)
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
        so.FindProperty("priority").intValue = 0;
        so.FindProperty("musicId").stringValue = music;
        so.ApplyModifiedPropertiesWithoutUndo();
        return zv;
    }

    public static string Run()
    {
        var log = new StringBuilder();
        Scene scene = SceneManager.GetActiveScene();
        var floor = GameObject.Find("Floor02");
        Transform content = floor.transform.Find("Content");
        if (content.Find("Zones") != null) return "Zones đã có, dừng.";

        var zones = new GameObject("Zones");
        zones.transform.SetParent(content, false);
        MakeZone(zones.transform, "Zone_Town", new Vector2(100f, 0.6f), new Vector2(8f, 5.6f), "zone.floor_02_town", "Thị trấn", 0, "music.town");
        var farm = MakeZone(zones.transform, "Zone_Farm", new Vector2(115.5f, 0.6f), new Vector2(23f, 14f), "zone.floor_02_farm", "Vùng Farm", 1, "music.gameplay");
        log.AppendLine("Floor02 Zone_Town center=(100,0.6) size=(8,5.6) -> x[96,104] y[-2.2,3.4] (trong Safe Zone x[95.8,104.2] y[-2.4,3.6])");
        log.AppendLine("Floor02 Zone_Farm center=(115.5,0.6) size=(23,14) -> x[104,127] y[-6.4,7.6]");

        var sp = content.Find("EnemySpawner");
        var sso = new SerializedObject(sp.GetComponent("EnemySpawner"));
        sso.FindProperty("spawnZone").objectReferenceValue = farm;
        sso.ApplyModifiedPropertiesWithoutUndo();
        log.AppendLine("Floor02/Content/EnemySpawner.spawnZone = Zone_Farm");

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        log.AppendLine("Đã lưu " + scene.path);
        return log.ToString();
    }
}
