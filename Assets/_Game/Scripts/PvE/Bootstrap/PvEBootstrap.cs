using Game.Core.Character;
using Game.Core.Combat;
using Game.Core.Events;
using Game.Core.Floor;
using Game.Core.Input;
using Game.Core.World;
using Game.Services;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game.PvE.Bootstrap
{
    /// <summary>
    /// Kéo tất cả Inspector refs vào đây — Bootstrap tự nối dây toàn bộ hệ thống PvE.
    /// Không giữ reference trực tiếp đến kiểu UI (tuân luật kiến trúc UI → PvE, không ngược lại).
    /// UI components được inject qua interface IInputReader (Joystick/AttackButton)
    /// và qua MonoBehaviour base với tên method SetInitialLevel (PlayerHUD).
    /// UI lấy dịch vụ Floor bằng cách đọc các property chỉ-đọc FloorDatabase / FloorLoader / WorldState.
    /// </summary>
    public class PvEBootstrap : MonoBehaviour
    {
        private static readonly System.Text.RegularExpressions.Regex BossSourceIdPattern =
            new System.Text.RegularExpressions.Regex(@"^boss\.floor_\d{2,}$",
                System.Text.RegularExpressions.RegexOptions.CultureInvariant);
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

        [Header("Combat Balance (Task B1)")]
        [Tooltip("Gán CombatBalanceConfig asset từ Data/Config/")]
        [SerializeField] private Game.Core.Combat.CombatBalanceConfig combatBalanceConfig;

        private IDeathService _deathService;
        private IFloorLoader _floorLoader;
        private IWorldStateService _worldState;
        private DeathRetryLoop _deathRetryLoop;

        // ── Truy cập chỉ-đọc cho UI (UI → PvE; PvE không biết UI) ─────────────

        /// <summary>FloorDatabase đã gán trong Inspector (null nếu chưa gán).</summary>
        public FloorDatabase FloorDatabase => floorDatabase;

        /// <summary>Loader dùng chung cho cả game (null nếu chưa gán FloorDatabase).</summary>
        public IFloorLoader FloorLoader => _floorLoader;

        /// <summary>MỘT IWorldStateService dùng chung cho loader và selector.</summary>
        public IWorldStateService WorldState => _worldState;

        private void Awake()
        {
            WireCombatBalance();
            WireInputReader();
            WireCamera();
            WireHUD();
            WireDeathService();
            WireFloorLoader();
        }

        private void Update()
        {
            // Thời gian THẬT: màn chết có thể đứng timeScale mà vòng tự thử lại vẫn phải chạy. Không chạy thì Tick thoát ngay.
            _deathRetryLoop?.Tick(Time.unscaledDeltaTime);
        }

        private void OnDestroy()
        {
            // Bootstrap bị hủy → dừng vòng tự thử lại (không còn Update nên cũng không còn tick nào).
            _deathRetryLoop?.Stop();

            GameEvents.OnPlayerDied -= HandlePlayerDied;
            GameEvents.OnDeathRetryRequested -= HandleDeathRetryRequested;
            GameEvents.OnDeathRecordUnconfirmed -= HandleDeathRecordUnconfirmed;
            GameEvents.OnPlayerDeathRecorded -= HandleDeathRecorded;
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

            _deathRetryLoop = _deathService != null
                ? new DeathRetryLoop(_deathService, DeathRetryLoop.DefaultIntervalSeconds, HandleDeathRetryFailed)
                : null;

            GameEvents.OnPlayerDied += HandlePlayerDied;
            GameEvents.OnDeathRetryRequested += HandleDeathRetryRequested;
            GameEvents.OnDeathRecordUnconfirmed += HandleDeathRecordUnconfirmed;
            GameEvents.OnPlayerDeathRecorded += HandleDeathRecorded;
        }

        private void WireFloorLoader()
        {
            if (floorDatabase == null)
            {
                Debug.LogWarning("[PvEBootstrap] FloorDatabase chưa được gán — Floor loading sẽ không hoạt động.", this);
                return;
            }

            bool isDead = _deathService?.CurrentDeathInfo?.isDead ?? false;

            // MỘT instance world state dùng chung cho loader (và cho FloorSelector phía UI qua property WorldState).
            _worldState = ServiceProvider.CreateWorldStateService();
            _floorLoader = new Game.PvE.Floor.FloorLoader(_worldState, floorDatabase, isDead);
        }

        // ──────────────────────────────────────────────
        // Death handler (Permadeath core)
        // ──────────────────────────────────────────────

        private async void HandlePlayerDied(object sender, DamageInfo lastHit)
        {
            if (_deathService == null) return;

            int level = player != null ? (player.Stats?.Level ?? 1) : 1;
            int playTime = Mathf.FloorToInt(Time.timeSinceLevelLoad);

            DeathReason reason = ResolveDeathReason(lastHit);

            // Chụp bản ghi NGAY lúc chết (docs/09 §1.3) rồi mới ghi.
            var record = new DeathRecord(
                reason,
                lastHit?.SourceId ?? "unknown",
                level,
                ResolveCurrentFloorNumber(),
                playTime,
                System.DateTimeOffset.UtcNow.ToUnixTimeSeconds());

            try
            {
                // Xác nhận được → service tự phát PlayerDeathRecorded (đúng một lần, chỉ sau khi nguồn chính ghi xong).
                await _deathService.RecordDeathAsync(record);
            }
            catch (SaveUnavailableException error)
            {
                // Chưa xác nhận được: vẫn chặn chơi (service giữ "đã chết" trong bộ nhớ), KHÔNG phát PlayerDeathRecorded.
                Debug.LogWarning($"[Death] Chưa ghi xác nhận được cái chết: {error.Message}", this);
                GameEvents.RaiseDeathRecordUnconfirmed(this, _deathService.CurrentDeathInfo);
            }
            catch (System.Exception error)
            {
                // async void: để lỗi thoát ra thì bị nuốt và người chơi không thấy gì → coi như chưa xác nhận (fail-closed).
                Debug.LogError($"[Death] Ghi nhận cái chết lỗi: {error}", this);
                GameEvents.RaiseDeathRecordUnconfirmed(this, _deathService.CurrentDeathInfo);
            }
        }

        /// <summary>
        /// Phân loại nguyên nhân chết thuần theo source ID (docs/09 §1.3).
        /// Boss phải dùng ID <c>boss.floor_NN</c>; source khác vẫn giữ luật Enemy/Trap cũ.
        /// </summary>
        public static DeathReason ResolveDeathReason(DamageInfo lastHit)
        {
            string sourceId = lastHit?.SourceId;
            if (string.IsNullOrEmpty(sourceId)) return DeathReason.Trap;
            return BossSourceIdPattern.IsMatch(sourceId) ? DeathReason.Boss : DeathReason.Enemy;
        }

        /// <summary>
        /// UI bấm "Thử lại" → ghi lại cái chết đang chờ NGAY (không chờ chu kỳ 5 giây).
        /// Thành công: service phát PlayerDeathRecorded → màn "Bạn đã chết"; lỗi: phát lại Unconfirmed.
        /// Đang có một lần thử dở (kể cả lần tự động) thì không chạy chồng — kết quả lần đó sẽ cập nhật màn.
        /// </summary>
        private void HandleDeathRetryRequested(object sender, System.EventArgs e)
        {
            _ = _deathRetryLoop?.RetryNowAsync();
        }

        /// <summary>
        /// Bắt đầu trạng thái "chưa xác nhận" → bật vòng tự thử lại mỗi 5 giây. Gọi nhiều lần (mỗi lần thử lỗi lại phát
        /// Unconfirmed) vẫn chỉ có MỘT vòng. Luật chết không đổi: vòng này chỉ thử ghi, không bao giờ "tha chết".
        /// </summary>
        private void HandleDeathRecordUnconfirmed(object sender, DeathInfo info)
        {
            _deathRetryLoop?.Start();
        }

        /// <summary>Đã xác nhận được cái chết → dừng vòng tự thử lại (màn "Bạn đã chết" do DeathScreenController hiển thị).</summary>
        private void HandleDeathRecorded(object sender, DeathInfo info)
        {
            _deathRetryLoop?.Stop();
        }

        /// <summary>Một lần thử lại (tự động hoặc bấm tay) vẫn lỗi → giữ màn "chưa lưu được" (vòng tiếp tục chạy).</summary>
        private void HandleDeathRetryFailed(System.Exception error)
        {
            Debug.LogWarning($"[Death] Thử lại ghi cái chết vẫn chưa được: {error.Message}", this);
            GameEvents.RaiseDeathRecordUnconfirmed(this, _deathService.CurrentDeathInfo);
        }

        /// <summary>
        /// Tầng người chơi đang ở lúc chết (docs/09 §1.3). Nguồn tin cậy theo thứ tự:
        /// 1) IFloorLoader.CurrentFloorId (tầng đã load qua loader) → floorNumber trong FloorDatabase;
        /// 2) loader chưa load tầng nào: Floor 1 nằm sẵn trong scene host → FloorDefinition có sceneKey = scene đang chạy.
        /// Không xác định được thì trả 0 ("không rõ") + log cảnh báo — KHÔNG đoán là tầng 1.
        /// </summary>
        private int ResolveCurrentFloorNumber()
        {
            if (floorDatabase == null)
            {
                Debug.LogWarning("[Death] Không có FloorDatabase — không xác định được tầng lúc chết (ghi 0).", this);
                return 0;
            }

            string currentId = _floorLoader?.CurrentFloorId;
            if (!string.IsNullOrEmpty(currentId))
            {
                FloorDefinition loaded = floorDatabase.GetFloor(currentId);
                if (loaded != null && loaded.floorNumber > 0) return loaded.floorNumber;
            }

            string activeScene = SceneManager.GetActiveScene().name;
            foreach (FloorDefinition def in floorDatabase.floors)
            {
                if (def != null && def.floorNumber > 0 && def.sceneKey == activeScene) return def.floorNumber;
            }

            Debug.LogWarning($"[Death] Không xác định được tầng lúc chết (CurrentFloorId='{currentId}', scene='{activeScene}') — ghi 0.", this);
            return 0;
        }

        private void WireCombatBalance()
        {
            if (combatBalanceConfig != null)
            {
                Game.Core.Combat.CombatBalance.DefenseWeight = combatBalanceConfig.defenseWeight;
            }
            else
            {
                // Đặt rõ ràng 1.0: CombatBalance là biến static, không được dựa vào giá trị còn sót từ lần chạy/test trước.
                Game.Core.Combat.CombatBalance.DefenseWeight = 1.0;
                Debug.LogWarning("[PvEBootstrap] CombatBalanceConfig chưa gán — dùng defenseWeight = 1.0 mặc định.", this);
            }
        }
    }

    /// <summary>
    /// Vòng tự thử lại ghi cái chết khi nguồn chính CHƯA xác nhận (docs/09 §1.3, §1.4).
    /// • MỘT vòng duy nhất: <see cref="Start"/> gọi nhiều lần vẫn chỉ là một vòng; <see cref="Stop"/> khi xác nhận được hoặc bootstrap bị hủy.
    /// • Chạy theo <see cref="Tick"/> từ Update của bootstrap (thời gian thật) → không có timer/luồng chạy ngầm; hủy bootstrap là hết tick.
    /// • KHÔNG chạy chồng: còn một lần thử đang dở thì tick lẫn nút "Thử lại" (<see cref="RetryNowAsync"/>) đều bị bỏ qua.
    /// • Không đổi luật chết: chỉ gọi <see cref="IDeathService.RetryRecordAsync"/>; lỗi thì vẫn là "đã chết", chỉ báo lại qua callback.
    /// Thành công được nhận biết bằng việc RetryRecordAsync trả về bình thường (xác nhận xong, hoặc không còn bản chờ).
    /// </summary>
    public sealed class DeathRetryLoop
    {
        /// <summary>Chu kỳ tự thử lại (giây).</summary>
        public const float DefaultIntervalSeconds = 5f;

        private readonly IDeathService _service;
        private readonly float _intervalSeconds;
        private readonly System.Action<System.Exception> _onAttemptFailed;

        private float _elapsedSeconds;
        private bool _running;
        private bool _attempting;

        /// <param name="service">Dịch vụ chết cần thử ghi lại.</param>
        /// <param name="intervalSeconds">Chu kỳ giữa hai lần tự thử; không dương thì dùng mặc định.</param>
        /// <param name="onAttemptFailed">Gọi mỗi khi một lần thử lỗi (để bootstrap phát lại Unconfirmed). Có thể null.</param>
        public DeathRetryLoop(IDeathService service, float intervalSeconds, System.Action<System.Exception> onAttemptFailed)
        {
            _service = service ?? throw new System.ArgumentNullException(nameof(service));
            _intervalSeconds = intervalSeconds > 0f ? intervalSeconds : DefaultIntervalSeconds;
            _onAttemptFailed = onAttemptFailed;
        }

        /// <summary>Vòng đang bật (đang ở trạng thái chưa xác nhận).</summary>
        public bool IsRunning => _running;

        /// <summary>Đang có một lần thử dở (chưa xong).</summary>
        public bool IsAttempting => _attempting;

        /// <summary>Bật vòng. Đang chạy rồi thì không làm gì (không tạo vòng thứ hai, không reset đồng hồ).</summary>
        public void Start()
        {
            if (_running) return;
            _running = true;
            _elapsedSeconds = 0f;
        }

        /// <summary>Tắt vòng (xác nhận được, hoặc bootstrap bị hủy).</summary>
        public void Stop()
        {
            _running = false;
            _elapsedSeconds = 0f;
        }

        /// <summary>
        /// Gọi mỗi frame với thời gian thật. Đủ chu kỳ và không có lần thử nào đang dở thì thử một lần.
        /// Thời gian chỉ tích lũy khi vòng đang bật và không có lần thử dở.
        /// </summary>
        public void Tick(float deltaSeconds)
        {
            if (!_running || _attempting) return;

            _elapsedSeconds += deltaSeconds;
            if (_elapsedSeconds < _intervalSeconds) return;

            _elapsedSeconds = 0f;
            _ = RetryNowAsync();
        }

        /// <summary>
        /// Thử NGAY (nút "Thử lại"), không cần vòng đang bật. Đang có lần thử dở thì bỏ qua (trả về task đã xong).
        /// Task trả về không bao giờ ném lỗi: lỗi được báo qua callback.
        /// </summary>
        public System.Threading.Tasks.Task RetryNowAsync()
        {
            if (_attempting) return System.Threading.Tasks.Task.CompletedTask;

            _attempting = true;
            return AttemptAsync();
        }

        private async System.Threading.Tasks.Task AttemptAsync()
        {
            try
            {
                await _service.RetryRecordAsync();
                Stop(); // trả về bình thường = đã xác nhận (hoặc không còn bản chờ) → hết việc phải thử
            }
            catch (System.Exception error)
            {
                NotifyFailed(error);
            }
            finally
            {
                _attempting = false;
                _elapsedSeconds = 0f; // chu kỳ kế tiếp tính từ lúc lần thử này KẾT THÚC (cả khi bấm tay)
            }
        }

        private void NotifyFailed(System.Exception error)
        {
            try
            {
                _onAttemptFailed?.Invoke(error);
            }
            catch (System.Exception callbackError)
            {
                // Subscriber của sự kiện ném lỗi không được làm chết vòng lặp (Task bị bỏ rơi sẽ nuốt lỗi im lặng).
                Debug.LogException(callbackError);
            }
        }
    }
}
