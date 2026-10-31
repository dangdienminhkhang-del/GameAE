using System.Text;
using UnityEngine;

public static class DumpRects
{
    public static string Run()
    {
        var sb = new StringBuilder();
        var canvas = GameObject.Find("Canvas");
        var scaler = canvas.GetComponent<UnityEngine.UI.CanvasScaler>();
        sb.AppendLine("CanvasScaler mode=" + scaler.uiScaleMode + " ref=" + scaler.referenceResolution + " match=" + scaler.matchWidthOrHeight);
        foreach (string n in new[] { "ResetGuestButton", "PlayPvEButton", "PlayPvPButton", "SettingsButton", "QuitButton" })
        {
            var t = canvas.transform.Find(n) as RectTransform;
            if (t == null) { sb.AppendLine(n + ": không thấy"); continue; }
            var corners = new Vector3[4];
            t.GetWorldCorners(corners);
            sb.AppendLine(n + ": anchor=" + t.anchorMin + "-" + t.anchorMax + " pos=" + t.anchoredPosition + " size=" + t.sizeDelta + " label=" + t.GetComponentInChildren<UnityEngine.UI.Text>().text);
        }
        return sb.ToString();
    }
}
