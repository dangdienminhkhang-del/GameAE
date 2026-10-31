using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Tilemaps;

public static class PathCheck
{
    public static string Run()
    {
        var sb = new StringBuilder();
        Physics2D.SyncTransforms();
        var tmGo = GameObject.Find("Grid/Tilemap");
        var tm = tmGo.GetComponent<Tilemap>();
        tm.CompressBounds();
        var b = tm.cellBounds;
        sb.AppendLine("Tilemap x[" + b.xMin + "," + b.xMax + ") y[" + b.yMin + "," + b.yMax + ")");
        // Tile theo cột trong phòng boss (x 80..90) ở dải y -22..-1
        var cols = new StringBuilder();
        for (int x = 80; x <= 90; x++)
        {
            int n = 0;
            for (int y = -23; y <= 0; y++) if (tm.HasTile(new Vector3Int(x, y, 0))) n++;
            cols.Append(x + ":" + n + " ");
        }
        sb.AppendLine("Số tile theo cột x (y -23..0): " + cols);

        int W = 140, H = 90, ox = -30, oy = -40; // lưới 1x1 phủ x[-30,110) y[-40,50)
        var blocked = new bool[W * H];
        for (int i = 0; i < W; i++)
            for (int j = 0; j < H; j++)
            {
                var p = new Vector2(ox + i + 0.5f, oy + j + 0.5f);
                foreach (var c in Physics2D.OverlapCircleAll(p, 0.3f))
                {
                    if (c.isTrigger) continue;
                    if (c is TilemapCollider2D || c is CompositeCollider2D) { blocked[i + j * W] = true; break; }
                }
            }
        Func<Vector2, int> idx = p => (int)Math.Floor(p.x - ox) + (int)Math.Floor(p.y - oy) * W;
        var dist = new float[W * H];
        for (int i = 0; i < dist.Length; i++) dist[i] = float.PositiveInfinity;
        int start = idx(new Vector2(0.5f, 0.5f));
        dist[start] = 0;
        var set = new SortedSet<(float, int)> { (0f, start) };
        int[] dx = { 1, -1, 0, 0, 1, 1, -1, -1 };
        int[] dy = { 0, 0, 1, -1, 1, -1, 1, -1 };
        while (set.Count > 0)
        {
            var (d, cur) = set.Min; set.Remove(set.Min);
            if (d > dist[cur]) continue;
            int cx = cur % W, cy = cur / W;
            for (int k = 0; k < 8; k++)
            {
                int nx = cx + dx[k], ny = cy + dy[k];
                if (nx < 0 || ny < 0 || nx >= W || ny >= H) continue;
                int ni = nx + ny * W;
                if (blocked[ni]) continue;
                if (k >= 4 && (blocked[(cx + dx[k]) + cy * W] || blocked[cx + (cy + dy[k]) * W])) continue;
                float nd = d + (k < 4 ? 1f : 1.414f);
                if (nd < dist[ni]) { dist[ni] = nd; set.Add((nd, ni)); }
            }
        }
        int reach = 0, open = 0;
        for (int i = 0; i < dist.Length; i++) { if (!blocked[i]) open++; if (!float.IsInfinity(dist[i])) reach++; }
        sb.AppendLine("Ô đi được=" + open + ", với tới từ điểm vào=" + reach);
        var targets = new (string, Vector2)[]
        {
            ("FarmA", new Vector2(21.5f, 0.5f)), ("FarmB", new Vector2(21.5f, 17.5f)), ("FarmC", new Vector2(41.5f, 17.5f)), ("Cove", new Vector2(20.5f, -13.5f)),
            ("Boss(76,-11.5)", new Vector2(76f, -11.5f)), ("Approach giữa (60,-11.5)", new Vector2(60f, -11.5f)),
            ("Phòng boss trái (64,-11.5)", new Vector2(64f, -11.5f)), ("Phòng boss phải (86,-11.5)", new Vector2(86f, -11.5f)), ("Phòng boss (89,-11.5)", new Vector2(89f, -11.5f)),
        };
        foreach (var (name, p) in targets)
        {
            int i = idx(p);
            float d = (i >= 0 && i < dist.Length) ? dist[i] : float.PositiveInfinity;
            sb.AppendLine(name + ": " + (float.IsInfinity(d) ? "KHÔNG với tới / bị chắn" : "quãng đường " + d.ToString("0.0") + " = " + (d / 4.5f).ToString("0.0") + " s"));
        }
        return sb.ToString();
    }
}
