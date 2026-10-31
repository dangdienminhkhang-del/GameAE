using UnityEngine;
using UnityEngine.UI;

public static class ClickPvE
{
    public static string Run()
    {
        var go = GameObject.Find("Canvas/PlayPvEButton");
        if (go == null) return "không thấy PlayPvEButton";
        var b = go.GetComponent<Button>();
        b.onClick.Invoke();
        return "đã bấm PlayPvEButton interactable=" + b.interactable;
    }
}
