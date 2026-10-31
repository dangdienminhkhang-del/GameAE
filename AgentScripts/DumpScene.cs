using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

public static class DumpScene
{
    public static string Run()
    {
        var sb = new StringBuilder();
        foreach (string name in new[] { "Player", "Floor01/EntryPoint", "Floor01/Content/SafeZone_Lamp", "Floor01/Content/NPC_Keeper", "Floor01/Content/Boss_Floor01", "Floor01/Content/TrainingDummy", "Floor01/Content/Pickup_Sword", "Floor01/Content/Pickup_Potion", "Floor01/Content/Spawner_FarmA", "Floor01/Content/Spawner_FarmB", "Floor01/Content/Spawner_FarmC", "Floor01/Content/Spawner_Cove", "Main Camera" })
        {
            var go = GameObject.Find(name);
            if (go == null) { sb.AppendLine(name + ": KHÔNG THẤY"); continue; }
            sb.Append(name + ": pos=" + go.transform.position + " scale=" + go.transform.lossyScale);
            var col = go.GetComponent<Collider2D>();
            if (col != null) sb.Append(" collider=" + col.GetType().Name + " trigger=" + col.isTrigger + " bounds center=" + col.bounds.center + " size=" + col.bounds.size);
            var sp = go.GetComponent("EnemySpawner");
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
            sb.AppendLine();
        }
        var player = GameObject.Find("Player");
        if (player != null)
        {
            var pc = player.GetComponent("PlayerController");
            var pso = new SerializedObject(pc);
            var it = pso.GetIterator();
            it.Next(true);
            while (it.NextVisible(false))
                if (it.name.ToLower().Contains("speed")) sb.AppendLine("PlayerController." + it.name + "=" + it.floatValue);
        }
        var tm = GameObject.Find("Grid/Tilemap");
        if (tm != null)
        {
            var t = tm.GetComponent<Tilemap>();
            t.CompressBounds();
            var b = t.cellBounds;
            sb.AppendLine("Tilemap cellBounds min=" + b.min + " max=" + b.max + " cellSize=" + t.cellSize + " localBounds=" + t.localBounds + " tileCount=" + CountTiles(t));
        }
        var cam = GameObject.Find("Main Camera");
        if (cam != null)
        {
            var c = cam.GetComponent<Camera>();
            sb.AppendLine("Camera ortho=" + c.orthographicSize + " aspect16:9 halfWidth=" + (c.orthographicSize * 16f / 9f));
            var cf = cam.GetComponent("CameraFollow");
            var cso = new SerializedObject(cf);
            var ci = cso.GetIterator(); ci.Next(true);
            while (ci.NextVisible(false)) sb.AppendLine("CameraFollow." + ci.name + " (" + ci.propertyType + ")");
        }
        var reg = GameObject.Find("FloorSpawnRegistrar");
        if (reg != null)
        {
            var rso = new SerializedObject(reg.GetComponent("FloorSpawnRegistrar"));
            sb.AppendLine("Registrar.enemySpawner=" + (rso.FindProperty("enemySpawner").objectReferenceValue != null) + " additional=" + rso.FindProperty("additionalSpawners").arraySize);
        }
        return sb.ToString();
    }

    private static int CountTiles(Tilemap t)
    {
        int n = 0;
        foreach (var p in t.cellBounds.allPositionsWithin) if (t.HasTile(p)) n++;
        return n;
    }
}
