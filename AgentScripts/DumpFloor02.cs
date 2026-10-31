using System.Text;
using UnityEditor;
using UnityEngine;

public static class DumpFloor02
{
    public static string Run()
    {
        var sb = new StringBuilder();
        var root = GameObject.Find("Floor02");
        foreach (var t in root.GetComponentsInChildren<Transform>(true))
        {
            sb.Append(t.name + ": pos=" + t.position + " scale=" + t.lossyScale);
            var box = t.GetComponent<BoxCollider2D>();
            if (box != null)
            {
                Vector2 size = Vector2.Scale(box.size, t.lossyScale);
                Vector2 c = (Vector2)t.position + Vector2.Scale(box.offset, t.lossyScale);
                sb.Append(" BOX trigger=" + box.isTrigger + " center=" + c + " size=" + size + " x[" + (c.x - size.x / 2) + "," + (c.x + size.x / 2) + "] y[" + (c.y - size.y / 2) + "," + (c.y + size.y / 2) + "]");
            }
            var sr = t.GetComponent<SpriteRenderer>();
            if (sr != null && sr.sprite != null) sb.Append(" sprite=" + sr.sprite.name + " worldSize=" + Vector2.Scale(sr.sprite.bounds.size, t.lossyScale));
            var sp = t.GetComponent("EnemySpawner");
            if (sp != null)
            {
                var so = new SerializedObject(sp);
                var center = so.FindProperty("spawnCenter").objectReferenceValue as Transform;
                sb.Append(" spawnCenter=" + (center != null ? center.name + "@" + center.position : "null"));
                var entries = so.FindProperty("entries");
                for (int i = 0; i < entries.arraySize; i++)
                {
                    var e = entries.GetArrayElementAtIndex(i);
                    var def = e.FindPropertyRelative("definition").objectReferenceValue;
                    sb.Append(" [" + (def != null ? def.name : "null") + " x" + e.FindPropertyRelative("count").intValue + " r=" + e.FindPropertyRelative("radius").floatValue + "]");
                }
            }
            var reg = t.GetComponent("FloorSpawnRegistrar");
            if (reg != null)
            {
                var rso = new SerializedObject(reg);
                sb.Append(" registrar.enemySpawner=" + (rso.FindProperty("enemySpawner").objectReferenceValue != null) + " additional=" + rso.FindProperty("additionalSpawners").arraySize);
            }
            sb.AppendLine();
        }
        return sb.ToString();
    }
}
