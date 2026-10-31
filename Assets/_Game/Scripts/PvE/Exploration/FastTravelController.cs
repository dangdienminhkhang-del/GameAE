using System.Collections;
using Game.Core.Events;
using Game.Core.World;
using Game.PvE.Player;
using UnityEngine;

namespace Game.PvE.Exploration
{
    /// <summary>
    /// Dịch chuyển nhanh tới Đèn Hiệu đã thắp (docs/17 §6.2). Luật nằm ở <see cref="FastTravelRules"/>.
    /// Kênh 3 giây; huỷ nếu người chơi trúng đòn hoặc tự di chuyển. Đặt trên Content của tầng.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class FastTravelController : MonoBehaviour
    {
        private static readonly Color Gold = new Color(1f, 0.85f, 0.35f);
        private static readonly Color Warn = new Color(1f, 0.45f, 0.35f);

        private float _lastDamageTime = -999f;
        private Coroutine _channel;
        private Transform _player;

        public static FastTravelController Instance { get; private set; }

        public bool IsChanneling { get; private set; }

        /// <summary>0..1 trong lúc đang kênh.</summary>
        public float ChannelProgress { get; private set; }

        public string DestinationName { get; private set; }

        private void OnEnable()
        {
            Instance = this;
            GameEvents.OnHealthChanged += HandleHealthChanged;
        }

        private void OnDisable()
        {
            GameEvents.OnHealthChanged -= HandleHealthChanged;
            if (Instance == this) Instance = null;
            CancelChannel(null);
        }

        private void HandleHealthChanged(object sender, HealthChangedArgs args)
        {
            if (args == null || args.Delta >= 0.0 || !(sender is HealthComponent)) return;
            _lastDamageTime = Time.time;
            if (IsChanneling) CancelChannel("Dịch chuyển bị huỷ: bạn bị tấn công!");
        }

        /// <summary>Yêu cầu dịch chuyển. Trả về kết quả kiểm luật; Ok = đã bắt đầu kênh.</summary>
        public FastTravelRules.Result Request(BeaconWaypoint destination)
        {
            if (destination == null || IsChanneling) return FastTravelRules.Result.DestinationLocked;
            if (_player == null) _player = MapExplorer.FindPlayer();
            if (_player == null) return FastTravelRules.Result.PlayerDead;

            var health = _player.GetComponent<HealthComponent>();
            var info = FloorMapInfo.Current;
            var result = FastTravelRules.Check(
                health != null && health.IsDead,
                Time.time - _lastDamageTime,
                destination.IsLit,
                info != null && info.FloorId == destination.FloorId,
                Vector2.Distance(_player.position, destination.ArrivalPoint));

            if (result != FastTravelRules.Result.Ok)
            {
                GameEvents.RaiseCombatNotice(FastTravelRules.Describe(result), Warn);
                return result;
            }

            _channel = StartCoroutine(Channel(destination));
            return result;
        }

        public void CancelChannel(string reason)
        {
            if (_channel != null) StopCoroutine(_channel);
            _channel = null;
            if (IsChanneling && !string.IsNullOrEmpty(reason)) GameEvents.RaiseCombatNotice(reason, Warn);
            IsChanneling = false;
            ChannelProgress = 0f;
            DestinationName = null;
        }

        private IEnumerator Channel(BeaconWaypoint dest)
        {
            IsChanneling = true;
            ChannelProgress = 0f;
            DestinationName = dest.DisplayName;
            Vector2 start = _player.position;
            float t = 0f;
            GameEvents.RaiseCombatNotice($"Đang dịch chuyển tới {dest.DisplayName}... đứng yên {FastTravelRules.ChannelSeconds:0} giây", Gold);

            while (t < FastTravelRules.ChannelSeconds)
            {
                yield return null;
                if (_player == null) { CancelChannel(null); yield break; }
                t += Time.deltaTime;
                ChannelProgress = Mathf.Clamp01(t / FastTravelRules.ChannelSeconds);
                float moved = Vector2.Distance(_player.position, start);
                if (FastTravelRules.ChannelBrokenByMovement(moved))
                {
                    CancelChannel("Dịch chuyển bị huỷ: bạn đã di chuyển.");
                    yield break;
                }
            }

            Vector2 point = dest.ArrivalPoint;
            _player.position = point;
            var rb = _player.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.position = point;
                rb.linearVelocity = Vector2.zero;
            }
            GameEvents.RaiseCombatNotice($"Đã dịch chuyển tới {dest.DisplayName}", Gold);

            IsChanneling = false;
            ChannelProgress = 0f;
            DestinationName = null;
            _channel = null;
        }
    }
}
