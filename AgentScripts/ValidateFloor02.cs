using System;
using System.Collections.Generic;
using System.Text;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class ValidateFloor02
{
    public static string Run()
    {
        var sb = new StringBuilder();
        var content = GameObject.Find("Floor02").transform.Find("Content").gameObject;
        bool was = content.activeSelf;
        content.SetActive(true);
        Physics2D.SyncTransforms();
        Type v = null;
        foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
        {
            v = asm.GetType("Game.Editor.ZoneLayoutValidator", false);
            if (v != null) break;
        }
        var errors = new List<string>();
        var warnings = new List<string>();
        v.GetMethod("Validate").Invoke(null, new object[] { errors, warnings });
        sb.AppendLine("Floor02 LỖI: " + errors.Count);
        foreach (var e in errors) sb.AppendLine("  " + e);
        foreach (var w in warnings) sb.AppendLine("  Cảnh báo: " + w);
        content.SetActive(was);
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
        sb.AppendLine("Content.activeSelf trả về " + content.activeSelf);
        return sb.ToString();
    }
}
