using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEngine;

public static class RoleProbe
{
    private static StringBuilder _log = new StringBuilder();
    private static Transform _player;
    private static MonoBehaviour _spitter;
    private static float _t0, _lastSample;
    private static int _phase;
    private static string _path;
    private static float _phaseStart;
    private static MonoBehaviour _healthComp;
    private static bool _running;

    private static object Get(object o, string name)
    {
        var t = o.GetType();
        for (; t != null; t = t.BaseType)
        {
            var p = t.GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (p != null) return p.GetValue(o);
            var f = t.GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (f != null) return f.GetValue(o);
        }
        return null;
    }

    private static string RoleOf(MonoBehaviour enemy)
    {
        object def = Get(enemy, "Definition") ?? Get(enemy, "definition") ?? Get(enemy, "_definition");
        if (def == null) return "?";
        var role = Get(def, "Role") ?? Get(def, "role");
        return role != null ? role.ToString() : "?";
    }

    private static string StateOf(MonoBehaviour enemy)
    {
        foreach (string n in new[] { "State", "CurrentState", "_state", "state" })
        {
            var v = Get(enemy, n);
            if (v != null) return v.ToString();
        }
        return "?";
    }

    private static double Hp(MonoBehaviour h)
    {
        var v = Get(h, "CurrentHealth") ?? Get(h, "currentHealth") ?? Get(h, "_currentHealth");
        return v != null ? Convert.ToDouble(v) : double.NaN;
    }

    public static string Start()
    {
        _path = Path.Combine(Directory.GetParent(Application.dataPath).FullName, "AgentScripts", "probe_log.txt");
        _log.Clear();
        var playerGo = GameObject.FindGameObjectWithTag("Player");
        if (playerGo == null) return "không thấy Player";
        _player = playerGo.transform;
        _healthComp = playerGo.GetComponents<MonoBehaviour>().FirstOrDefault(m => double.IsNaN(Hp(m)) == false);

        var enemies = UnityEngine.Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Exclude, FindObjectsSortMode.None).Where(m => m.GetType().Name == "EnemyController").ToList();
        var info = new StringBuilder();
        info.AppendLine("Enemy đang bật: " + enemies.Count);
        foreach (var e in enemies) info.AppendLine("  " + e.name + " role=" + RoleOf(e) + " state=" + StateOf(e) + " pos=" + e.transform.position);
        _spitter = enemies.FirstOrDefault(e => RoleOf(e) == "Ranged");
        info.AppendLine("Spitter: " + (_spitter != null ? _spitter.name : "KHÔNG CÓ") + " health=" + (_healthComp != null ? _healthComp.GetType().Name + "=" + Hp(_healthComp) : "?"));
        if (_spitter == null) return info.ToString();

        // Đặt người chơi cách spitter 6.0 theo trục y (trong tầm bắn 6, trong tầm phát hiện 7).
        Vector3 sp = _spitter.transform.position;
        Teleport(new Vector2(sp.x, sp.y - 6.0f));
        _phase = 0;
        _phaseStart = Time.time;
        _t0 = Time.time;
        _lastSample = -1f;
        if (!_running) { EditorApplication.update += Tick; _running = true; }
        info.AppendLine("Bắt đầu quan sát. Player=" + _player.position);
        return info.ToString();
    }

    private static void Teleport(Vector2 p)
    {
        var rb = _player.GetComponent<Rigidbody2D>();
        if (rb != null) { rb.position = p; rb.linearVelocity = Vector2.zero; }
        _player.position = new Vector3(p.x, p.y, _player.position.z);
        Physics2D.SyncTransforms();
    }

    private static void Tick()
    {
        if (!Application.isPlaying || _spitter == null || _player == null) { Stop(); return; }
        float now = Time.time;
        if (now - _lastSample < 0.25f) return;
        _lastSample = now;

        var pool = Type.GetType("Game.PvE.Enemy.Projectile.EnemyProjectilePool, Game.PvE");
        object cur = pool != null ? pool.GetProperty("Current").GetValue(null) : null;
        int active = cur != null ? Convert.ToInt32(pool.GetProperty("ActiveCount").GetValue(cur)) : -1;
        double hp = _healthComp != null ? Hp(_healthComp) : double.NaN;
        float dist = Vector2.Distance(_spitter.transform.position, _player.position);
        _log.AppendLine("t=" + (now - _t0).ToString("0.00") + " phase=" + _phase + " dist=" + dist.ToString("0.00") + " spitterState=" + StateOf(_spitter) + " projActive=" + active + " playerHp=" + hp.ToString("0") + " spitterPos=" + _spitter.transform.position + " playerPos=" + _player.position);

        float inPhase = now - _phaseStart;
        if (_phase == 0 && inPhase > 9f)
        {
            // Giai đoạn 1: đưa người chơi sát quái (1.8) để xem quái lùi.
            Vector3 sp = _spitter.transform.position;
            Teleport(new Vector2(sp.x, sp.y - 1.8f));
            _phase = 1; _phaseStart = now;
            _log.AppendLine("--- chuyển giai đoạn 1: người chơi sát quái (1.8) ---");
        }
        else if (_phase == 1 && inPhase > 6f)
        {
            Teleport(new Vector2(0.5f, 0.5f)); // về Safe Zone
            _phase = 2; _phaseStart = now;
            _log.AppendLine("--- giai đoạn 2: về Safe Zone ---");
        }
        else if (_phase == 2 && inPhase > 4f)
        {
            _log.AppendLine("--- xong ---");
            Stop();
        }
        if (hp < 25 && _phase < 2)
        {
            Teleport(new Vector2(0.5f, 0.5f));
            _phase = 2; _phaseStart = now;
            _log.AppendLine("--- HP thấp (" + hp + "), rút về Safe Zone ---");
        }
        File.WriteAllText(_path, _log.ToString());
    }

    private static void Stop()
    {
        EditorApplication.update -= Tick;
        _running = false;
        if (_log.Length > 0 && _path != null) File.WriteAllText(_path, _log.ToString());
    }
}
