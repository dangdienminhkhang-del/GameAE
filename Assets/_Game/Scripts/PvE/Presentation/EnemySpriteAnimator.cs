using System;
using Game.PvE.Enemy;
using UnityEngine;

namespace Game.PvE.Presentation
{
    /// <summary>
    /// Chọn khung hình cho quái theo trạng thái (đứng / di chuyển / lấy đà / ra đòn).
    /// Một prefab Enemy dùng chung cho mọi loại nên bảng sprite tra theo EnemyDefinition.Id.
    /// Chỉ trình bày: nháy trúng đòn, báo đòn (màu) và chết (mờ dần) vẫn do EnemyController xử lý.
    /// </summary>
    [RequireComponent(typeof(EnemyController))]
    public class EnemySpriteAnimator : MonoBehaviour
    {
        [Serializable]
        public class Entry
        {
            public string enemyId;
            public Sprite[] idle;
            public Sprite[] move;
            [Tooltip("Các khung cuối = ra đòn; các khung trước = lấy đà.")]
            public Sprite[] attack;
            public float fps = 8f;
        }

        [SerializeField] private Entry[] entries;
        [SerializeField] private float strikeSeconds = 0.22f;

        private EnemyController _controller;
        private SpriteRenderer _renderer;
        private Rigidbody2D _body;
        private Entry _entry;
        private string _entryId;
        private float _time;
        private float _strikeTimer;
        private bool _wasWindingUp;

        private void Awake()
        {
            _controller = GetComponent<EnemyController>();
            _renderer = GetComponent<SpriteRenderer>();
            _body = GetComponent<Rigidbody2D>();
        }

        private void OnEnable()
        {
            _time = UnityEngine.Random.value * 2f;
            _strikeTimer = 0f;
            _wasWindingUp = false;
        }

        private void LateUpdate()
        {
            if (_renderer == null || _controller == null || _controller.Definition == null) return;
            ResolveEntry(_controller.Definition.Id);
            if (_entry == null || _entry.idle == null || _entry.idle.Length == 0) return;

            _time += Time.deltaTime;
            bool winding = _controller.IsWindingUp;
            if (_wasWindingUp && !winding && !_controller.IsDead && !_controller.IsHurt) _strikeTimer = strikeSeconds;
            _wasWindingUp = winding;
            if (_strikeTimer > 0f) _strikeTimer -= Time.deltaTime;

            Sprite[] set = _entry.idle;
            bool hasAttack = _entry.attack != null && _entry.attack.Length > 1;
            Sprite pick = null;

            if (_controller.IsDead)
            {
                pick = _entry.idle[0];
            }
            else if (hasAttack && _strikeTimer > 0f)
            {
                pick = _entry.attack[_entry.attack.Length - 1];
            }
            else if (hasAttack && winding)
            {
                int windFrames = _entry.attack.Length - 1;
                pick = _entry.attack[Mathf.FloorToInt(_time * _entry.fps * 0.6f) % windFrames];
            }
            else
            {
                bool moving = _body != null && _body.linearVelocity.sqrMagnitude > 0.01f;
                if (moving && _entry.move != null && _entry.move.Length > 0) set = _entry.move;
                pick = set[Mathf.FloorToInt(_time * _entry.fps) % set.Length];
            }

            if (pick != null) _renderer.sprite = pick;
        }

        private void ResolveEntry(string id)
        {
            if (_entryId == id) return;
            _entryId = id;
            _entry = null;
            if (entries == null) return;
            for (int i = 0; i < entries.Length; i++)
            {
                if (entries[i] != null && entries[i].enemyId == id) { _entry = entries[i]; return; }
            }
        }
    }
}
