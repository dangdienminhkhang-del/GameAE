using System;
using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;

public static class CheckU3b
{
    public static string Run()
    {
        var sb = new StringBuilder();
        Physics2D.SyncTransforms();
        foreach (var mb in UnityEngine.Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            string tn = mb.GetType().Name;
            if (tn == "ZoneVolume")
            {
                var so = new SerializedObject(mb);
                var t = mb.transform;
                var box = mb.GetComponent<BoxCollider2D>();
                string geo = "";
                if (box != null)
                {
                    Vector2 size = Vector2.Scale(box.size, t.lossyScale);
                    Vector2 c = (Vector2)t.position + Vector2.Scale(box.offset, t.lossyScale);
                    geo = "x[" + (c.x - size.x / 2) + "," + (c.x + size.x / 2) + "] y[" + (c.y - size.y / 2) + "," + (c.y + size.y / 2) + "] trigger=" + box.isTrigger;
                }
                else geo = "KHÔNG có BoxCollider2D (" + (mb.GetComponent<Collider2D>() != null ? mb.GetComponent<Collider2D>().GetType().Name : "không collider") + ")";
                sb.AppendLine("ZoneVolume " + mb.name + " id=" + so.FindProperty("zoneId").stringValue + " kind=" + so.FindProperty("kind").enumValueIndex + " prio=" + so.FindProperty("priority").intValue + " music=" + so.FindProperty("musicId").stringValue + " " + geo + " active=" + mb.gameObject.activeInHierarchy);
            }
            else if (tn == "BossController")
            {
                sb.AppendLine("BossController @ " + mb.name + " pos=" + mb.transform.position + " active=" + mb.gameObject.activeInHierarchy);
                var so = new SerializedObject(mb);
                var it = so.GetIterator(); it.Next(true);
                while (it.NextVisible(false))
                {
                    if (it.name == "m_Script") continue;
                    string v = it.propertyType == SerializedPropertyType.ObjectReference ? (it.objectReferenceValue != null ? it.objectReferenceValue.name : "NULL")
                        : it.propertyType == SerializedPropertyType.Float ? it.floatValue.ToString()
                        : it.propertyType == SerializedPropertyType.Integer ? it.intValue.ToString()
                        : it.propertyType == SerializedPropertyType.Boolean ? it.boolValue.ToString() : it.propertyType.ToString();
                    sb.AppendLine("   " + it.name + " = " + v);
                }
            }
        }
        Type v2 = null;
        foreach (var asm in AppDomain.CurrentDomain.GetAssemblies()) { v2 = asm.GetType("Game.Editor.ZoneLayoutValidator", false); if (v2 != null) break; }
        var errors = new List<string>(); var warnings = new List<string>();
        v2.GetMethod("Validate").Invoke(null, new object[] { errors, warnings });
        sb.AppendLine("VALIDATOR lỗi=" + errors.Count);
        foreach (var e in errors) sb.AppendLine("  " + e);
        foreach (var w in warnings) sb.AppendLine("  CB: " + w);
        return sb.ToString();
    }
}
