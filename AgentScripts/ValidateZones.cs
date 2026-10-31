using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

public static class ValidateZones
{
    public static string Run()
    {
        var sb = new StringBuilder();
        Type v = null;
        foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
        {
            v = asm.GetType("Game.Editor.ZoneLayoutValidator", false);
            if (v != null) break;
        }
        var errors = new List<string>();
        var warnings = new List<string>();
        v.GetMethod("Validate").Invoke(null, new object[] { errors, warnings });
        sb.AppendLine("LỖI: " + errors.Count);
        foreach (var e in errors) sb.AppendLine("  " + e);
        foreach (var w in warnings) sb.AppendLine("  Cảnh báo: " + w);

        Physics2D.SyncTransforms();
        var pts = new (string, Vector2, Vector2)[]
        {
            ("Entry -> FarmA", new Vector2(0f, 0f), new Vector2(21.5f, 0.5f)),
            ("FarmA -> FarmB", new Vector2(21.5f, 0.5f), new Vector2(21.5f, 17.5f)),
            ("FarmB -> FarmC", new Vector2(21.5f, 17.5f), new Vector2(41.5f, 17.5f)),
            ("FarmA -> Cove", new Vector2(21.5f, 0.5f), new Vector2(20.5f, -13.5f)),
            ("FarmC -> Boss", new Vector2(41.5f, 17.5f), new Vector2(67.5f, -11.5f)),
            ("Entry -> Boss", new Vector2(0f, 0f), new Vector2(67.5f, -11.5f)),
        };
        foreach (var (name, a, b) in pts)
        {
            var hits = Physics2D.LinecastAll(a, b);
            int solid = 0;
            foreach (var h in hits) if (!h.collider.isTrigger && h.collider.gameObject.name != "Player") solid++;
            sb.AppendLine("Linecast " + name + ": dài " + Vector2.Distance(a, b).ToString("0.0") + " (" + (Vector2.Distance(a, b) / 4.5f).ToString("0.0") + " s), vật chắn cứng=" + solid);
        }
        return sb.ToString();
    }
}
