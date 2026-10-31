using System;
using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class TrimBossRoom
{
    public static string Run()
    {
        var sb = new StringBuilder();
        GameObject room = null;
        foreach (var t in UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (t.name == "Zone_BossRoom") room = t.gameObject;
        if (room == null) return "Không thấy Zone_BossRoom";
        var box = room.GetComponent<BoxCollider2D>();
        var tr = room.transform;
        Vector2 sc = tr.lossyScale;
        Vector2 size = Vector2.Scale(box.size, sc);
        Vector2 c = (Vector2)tr.position + Vector2.Scale(box.offset, sc);
        sb.AppendLine("Trước: x[" + (c.x - size.x / 2) + "," + (c.x + size.x / 2) + "] y[" + (c.y - size.y / 2) + "," + (c.y + size.y / 2) + "]");
        float minX = c.x - size.x / 2f;
        float newMaxX = 87f;
        float newCenterX = (minX + newMaxX) / 2f;
        float newWidth = newMaxX - minX;
        Undo.RecordObject(tr, "Trim BossRoom");
        Undo.RecordObject(box, "Trim BossRoom");
        // dời tâm theo trục x (giữ offset), đổi bề rộng theo scale
        float offsetX = box.offset.x * sc.x;
        tr.position = new Vector3(newCenterX - offsetX, tr.position.y, tr.position.z);
        box.size = new Vector2(newWidth / sc.x, box.size.y);
        Physics2D.SyncTransforms();
        Vector2 size2 = Vector2.Scale(box.size, tr.lossyScale);
        Vector2 c2 = (Vector2)tr.position + Vector2.Scale(box.offset, tr.lossyScale);
        sb.AppendLine("Sau: x[" + (c2.x - size2.x / 2) + "," + (c2.x + size2.x / 2) + "] y[" + (c2.y - size2.y / 2) + "," + (c2.y + size2.y / 2) + "]");
        var boss = GameObject.Find("Floor01/Content/Boss_Floor01");
        if (boss != null)
            sb.AppendLine("Boss " + boss.transform.position + " trong phòng? " + box.OverlapPoint(boss.transform.position));

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene());

        Type v = null;
        foreach (var asm in AppDomain.CurrentDomain.GetAssemblies()) { v = asm.GetType("Game.Editor.ZoneLayoutValidator", false); if (v != null) break; }
        var errors = new List<string>(); var warnings = new List<string>();
        v.GetMethod("Validate").Invoke(null, new object[] { errors, warnings });
        sb.AppendLine("Validator lỗi=" + errors.Count);
        foreach (var e in errors) sb.AppendLine("  " + e);
        foreach (var w in warnings) sb.AppendLine("  CB: " + w);
        return sb.ToString();
    }
}
