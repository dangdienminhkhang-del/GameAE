using Game.PvE.SafeZone;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game.PvE.Blacksmith
{
    /// <summary>
    /// Tự dựng Thợ rèn khi vào scene gameplay (không cần sửa scene). Đặt gần Vùng an toàn của thị trấn,
    /// bên trái (Quán trọ đặt bên phải) để hai NPC không chồng nhau.
    /// </summary>
    public static class BlacksmithRuntimeSpawner
    {
        private static Sprite _squareSprite;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void OnSceneLoaded()
        {
            SceneManager.sceneLoaded += (scene, mode) => TrySpawn();
            TrySpawn();
        }

        public static void TrySpawn()
        {
            string currentScene = SceneManager.GetActiveScene().name;
            if (currentScene != "PvE_Gameplay" && currentScene != "Floor02") return;
            if (Object.FindAnyObjectByType<BlacksmithInteractable>() != null) return;

            Vector3 spawnPos = Vector3.zero;
            var safeZone = Object.FindAnyObjectByType<SafeZoneTrigger>();
            if (safeZone != null)
            {
                spawnPos = safeZone.transform.position + new Vector3(-3f, 1f, 0f);
            }
            else
            {
                var player = Object.FindAnyObjectByType<Player.PlayerController>();
                if (player != null) spawnPos = player.transform.position + new Vector3(-3f, 0f, 0f);
            }

            var go = new GameObject("Blacksmith_Lisbeth");
            go.transform.position = spawnPos;

            var col = go.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = 2.0f;

            go.AddComponent<BlacksmithInteractable>();

            // Hình đơn giản (khối màu cam cám) + nhãn tên để người chơi nhận ra.
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = GetSquareSprite();
            sr.color = new Color(0.9f, 0.5f, 0.2f, 1f);
            sr.sortingOrder = 2;
            go.transform.localScale = new Vector3(1.1f, 1.4f, 1f);

            AddLabel(go.transform);
        }

        private static Sprite GetSquareSprite()
        {
            if (_squareSprite != null) return _squareSprite;
            var tex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            tex.SetPixel(0, 0, Color.white);
            tex.Apply();
            tex.filterMode = FilterMode.Point;
            _squareSprite = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
            return _squareSprite;
        }

        private static void AddLabel(Transform parent)
        {
            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font == null) return;

            var labelGo = new GameObject("Label");
            labelGo.transform.SetParent(parent, false);
            // Cha đang bị scale (1.1, 1.4) nên chia lại để chữ không bị méo.
            labelGo.transform.localPosition = new Vector3(0f, 0.85f, 0f);
            labelGo.transform.localScale = new Vector3(1f / 1.1f, 1f / 1.4f, 1f);

            var mesh = labelGo.AddComponent<TextMesh>();
            mesh.font = font;
            mesh.text = "Thợ rèn";
            mesh.fontSize = 64;
            mesh.characterSize = 0.08f;
            mesh.anchor = TextAnchor.MiddleCenter;
            mesh.alignment = TextAlignment.Center;
            mesh.color = Color.white;

            var renderer = labelGo.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = font.material;
            renderer.sortingOrder = 3;
        }
    }
}
