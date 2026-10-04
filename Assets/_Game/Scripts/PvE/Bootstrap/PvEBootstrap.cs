using Game.Core.Character;
using Game.Core.Combat;
using Game.Core.Events;
using Game.Core.Floor;
using Game.Core.Input;
using Game.Services;
using UnityEngine;

namespace Game.PvE.Bootstrap
{
    /// <summary>
    /// Kéo tất cả Inspector refs vào đây — Bootstrap tự nối dây toàn bộ hệ thống PvE.
    /// Không giữ reference trực tiếp đến kiểu UI (tuân luật kiến trúc UI → PvE, không ngược lại).
    /// UI components được inject qua interface IInputReader (Joystick/AttackButton)
    /// và qua MonoBehaviour base với tên method SetInitialLevel (PlayerHUD).
    /// </summary>
    public class PvEBootstrap : MonoBehaviour
    {
        [Header("Player")]
        [SerializeField] private Player.PlayerController player;
        [SerializeField] private Player.PlayerInputReader inputReader;
        [SerializeField] private Player.HealthComponent healthComponent;
        [SerializeField] private Player.PlayerAttack playerAttack;

        [Header("Camera")]
        [SerializeField] private Camera.CameraFollow cameraFollow;

        // UI Controls — khai báo MonoBehaviour để Inspector gán được,
        // runtime cast sang IInputReader (từ Game.Core.Input, không vi phạm kiến trúc)
        [Header("UI Controls (assign MonoBehaviour components here)")]
        [SerializeField] private MonoBehaviour joystickBehaviour;
        [SerializeField] private MonoBehaviour attackButtonBehaviour;

        // HUD — dùng MonoBehaviour, gọi SetInitialLevel qua reflection-free SendMessage
        [Header("HUD")]
        [SerializeField] private MonoBehaviour hudBehaviour;

        [Header("Death Service (optional — auto-created nếu bỏ trống)")]
        [SerializeField] private UnityEngine.Object deathServiceComponent;

        [Header("Floor (Wave 1B)")]
        [Tooltip("Gán FloorDatabase asset từ Data/Databases/")]
        [SerializeField] private FloorDatabase floorDatabase;

        private IDeathService _deathService;
        private IFloorLoader _floorLoader;

        private void Awake()
        {
            WireInputReader();
            WireCamera();
            WireHUD();
            WireDeathService();
            WireFloorLoader();
        }

        private void OnDestroy()
        {
            GameEvents.OnPlayerDied -= HandlePlayerDied;
        }

        // ──────────────────────────────────────────────
        // Wiring helpers
        // ──────────────────────────────────────────────

        private void WireInputReader()
        {
            if (inputReader == null) return;

            player?.AssignInputReader(inputReader);
            playerAttack?.AssignInputReader(inputReader);

            if (joystickBehaviour is IInputReader joystickReader)
                joystickReader.SetMoveInput(UnityEngine.Vector2.zero); // init
            // Assign qua public method trên MonoBehaviour (không cần kiểu cụ thể)
            SendAssignInputReader(joystickBehaviour, inputReader);
            SendAssignInputReader(attackButtonBehaviour, inputReader);
        }

        /// Gọi AssignInputReader(IInputReader) qua reflection nhẹ để tránh hard-link UI assembly.
        private static void SendAssignInputReader(MonoBehaviour target, IInputReader reader)
        {
            if (target == null || reader == null) return;
            var method = target.GetType().GetMethod("AssignInputReader",
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            method?.Invoke(target, new object[] { reader });
        }

        private void WireCamera()
        {
            if (cameraFollow != null && player != null)
                cameraFollow.AssignTarget(player.transform);
        }

        private void WireHUD()
        {
            if (hudBehaviour == null || player == null) return;
            int level = player.Stats?.Level ?? 1;

            const System.Reflection.BindingFlags flags =
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance;

            // Gọi qua reflection nhẹ — tránh coupling với Game.UI assembly
            hudBehaviour.GetType().GetMethod("SetInitialLevel", flags)?.Invoke(hudBehaviour, new object[] { level });

            // HUD chỉ nhận HP event từ HealthComponent của player (quái/bia không làm nhảy thanh HP)
            if (healthComponent != null)
                hudBehaviour.GetType().GetMethod("SetHealthSource", flags)?.Invoke(hudBehaviour, new object[] { healthComponent });
        }

        private void WireDeathService()
        {
            _deathService = deathServiceComponent as IDeathService;
            if (_deathService == null)
                _deathService = ServiceProvider.CreateDeathService();

            GameEvents.OnPlayerDied += HandlePlayerDied;
        }

        private void WireFloorLoader()
        {
            if (floorDatabase == null)
            {
                Debug.LogWarning("[PvEBootstrap] FloorDatabase chưa được gán — Floor loading sẽ không hoạt động.", this);
                return;
            }

            bool isDead = _deathService?.CurrentDeathInfo?.isDead ?? false;
            _floorLoader = new Game.PvE.Floor.FloorLoader(
                ServiceProvider.CreateWorldStateService(), floorDatabase, isDead);

            // Tạm comment để compile: cần asmdef Game.PvE → Game.UI reference.
            // TODO (AI Wave 1B): Add Game.UI asmdef reference to Game.PvE, sau đó
            // bỏ comment khối này.
            // Inject vào FloorSelectPanel nếu có trong scene
            // var panel = FindFirstObjectByType<UI.Floor.FloorSelectPanel>();
            // if (panel != null)
            // {
            //     var worldState = ServiceProvider.CreateWorldStateService();
            //     var selector = new PvE.Floor.FloorSelector(floorDatabase, worldState);
            //     panel.Inject(selector, _floorLoader);
            // }
        }

        // ──────────────────────────────────────────────
        // Death handler (Permadeath core)
        // ──────────────────────────────────────────────

        private async void HandlePlayerDied(object sender, DamageInfo lastHit)
        {
            if (_deathService == null) return;

            int floor = 1;   // TODO: lấy từ IWorldStateService khi có
            int level = player != null ? (player.Stats?.Level ?? 1) : 1;
            int playTime = Mathf.FloorToInt(Time.timeSinceLevelLoad);

            DeathReason reason = !string.IsNullOrEmpty(lastHit?.SourceId)
                ? DeathReason.Enemy
                : DeathReason.Trap;

            try
            {
                await _deathService.OnPlayerDiedAsync(
                    reason,
                    lastHit?.SourceId ?? "unknown",
                    level,
                    floor,
                    playTime);
            }
            catch (System.Exception error)
            {
                // async void: nếu để lỗi thoát ra, lỗi bị nuốt và Death Screen không bao giờ hiện.
                // Trạng thái chết đã được service giữ trong bộ nhớ → vẫn hiện Death Screen cho người chơi.
                Debug.LogError($"[Death] Ghi nhận cái chết lỗi: {error}", this);
                GameEvents.RaisePlayerDeathRecorded(this, _deathService.CurrentDeathInfo);
            }
        }
    }
}
