using UnityEditor;
using UnityEngine;

public static class ProjSize
{
    public static string Run()
    {
        var go = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Game/Prefabs/Enemies/EnemyProjectile.prefab");
        var sr = go.GetComponentInChildren<SpriteRenderer>();
        Vector2 spriteSize = sr.sprite.bounds.size;
        Vector2 world = Vector2.Scale(spriteSize, go.transform.localScale);
        return "sprite=" + sr.sprite.name + " spriteSize=" + spriteSize + " scale=" + go.transform.localScale + " world=" + world + " (mong muốn đường kính " + (0.25f * 2f) + ")";
    }
}
