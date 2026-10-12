using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Game.PvE.Boss;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Game.UI.Boss
{
    /// <summary>
    /// Bảng "Thử lại báo boss" (docs/02 §22, docs/12 §7.2, task P8-5).
    /// Boss đã gục nhưng máy chủ chưa xác nhận → hiện thông báo + nút Thử lại / Đóng.
    ///
    /// - Dựng HOÀN TOÀN bằng code (Canvas Screen Space Overlay riêng, CanvasScaler 1920x1080): không cần kéo gì trong Inspector.
    /// - TỰ có mặt: <see cref="RuntimeInitializeOnLoadMethodAttribute"/> tạo một GameObject (DontDestroyOnLoad) ở lần chạy đầu;
    ///   nó nghe <see cref="SceneManager.sceneLoaded"/> và quét <see cref="BossController"/> (kể cả đang tắt) để đăng ký
    ///   <see cref="BossController.DefeatReportFailed"/> / <see cref="BossController.DefeatReportConfirmed"/>.
    /// - UI chỉ hiển thị và gửi ý định: không có logic thưởng hay World State. Nút Thử lại chỉ gọi
    ///   <see cref="BossController.RetryDefeatReportAsync"/>. Trạng thái nằm ở <see cref="BossDefeatReportPanelModel"/> (thuần, có test).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class BossDefeatReportPanel : MonoBehaviour
    {
        // ── Hằng bố cục / thời gian ────────────────────────────────────────────────

        /// <summary>Cao hơn mọi Canvas khác trong game để bảng luôn nằm trên cùng.</summary>
        private const int CanvasSortingOrder = 1000;

        /// <summary>Quét lại boss mỗi chu kỳ này (giây thực), phòng boss được sinh ra SAU khi scene đã tải.</summary>
        private const float RescanIntervalSeconds = 1f;

        private const float CardWidth = 1200f;
        private const float CardHeight = 460f;
        private const float CardTopMargin = 60f;
        private const float CardPadding = 40f;
        private const float ButtonBottomMargin = 30f;
        private const float ButtonGap = 40f;
        private const float MessageBottomGap = 30f;

        private const float ToastWidth = 900f;
        private const float ToastHeight = 100f;
        private const float ToastTopMargin = 40f;

        private const int MessageFontSize = 38;
        private const int ButtonFontSize = 42;
        private const int ToastFontSize = 36;

        private static readonly Vector2 ReferenceResolution = new Vector2(1920f, 1080f);
        private static readonly Color CardColor = new Color(0.08f, 0.09f, 0.13f, 0.96f);
        private static readonly Color ToastColor = new Color(0.05f, 0.25f, 0.12f, 0.92f);
        private static readonly Color RetryColor = new Color(0.2f, 0.5f, 0.9f, 0.98f);
        private static readonly Color CloseColor = new Color(0.38f, 0.38f, 0.44f, 0.98f);
        private static readonly Color ButtonDisabledColor = new Color(0.45f, 0.45f, 0.45f, 0.7f);

        private static BossDefeatReportPanel s_instance;

        private sealed class Registration
        {
            public BossController Boss;
            public Action<Exception> OnFailed;
            public Action OnConfirmed;
        }

        private readonly BossDefeatReportPanelModel _model = new BossDefeatReportPanelModel();
        private readonly List<Registration> _registrations = new List<Registration>();

        /// <summary>Boss vừa báo thất bại gần nhất: nút Thử lại gửi lại cho boss này.</summary>
        private BossController _activeBoss;
        private float _rescanTimer;

        private Font _font;
        private GameObject _card;
        private GameObject _toast;
        private Button _retryButton;
        private Text _retryLabel;

        // ── Tự có mặt ───────────────────────────────────────────────────────────────

        // Khi tắt Domain Reload (Enter Play Mode Options), biến static còn giữ giá trị của lần Play trước: xóa ở đây.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStaticState()
        {
            s_instance = null;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EnsureExists()
        {
            if (s_instance != null) return;

            var host = new GameObject(nameof(BossDefeatReportPanel));
            DontDestroyOnLoad(host);
            s_instance = host.AddComponent<BossDefeatReportPanel>();
        }

        // ── Lifecycle ───────────────────────────────────────────────────────────────

        private void Awake()
        {
            s_instance = this;
            BuildUi();
            _model.Changed += Render;
            Render();
        }

        private void OnEnable()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
            SceneManager.sceneUnloaded += OnSceneUnloaded;
            ScanForBosses();
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneUnloaded -= OnSceneUnloaded;
            UnregisterAll();
        }

        private void OnDestroy()
        {
            _model.Changed -= Render;
            if (s_instance == this) s_instance = null;
        }

        private void Update()
        {
            float delta = UnityEngine.Time.unscaledDeltaTime;
            _model.Tick(delta);

            _rescanTimer -= delta;
            if (_rescanTimer <= 0f)
            {
                _rescanTimer = RescanIntervalSeconds;
                ScanForBosses();
            }
            else
            {
                PruneDestroyedBosses();
            }
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            ScanForBosses();
        }

        private void OnSceneUnloaded(Scene scene)
        {
            PruneDestroyedBosses();
        }

        // ── Đăng ký / hủy đăng ký với BossController ────────────────────────────────

        private void ScanForBosses()
        {
            PruneDestroyedBosses();

            BossController[] bosses = FindObjectsByType<BossController>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            bool addedAny = false;
            for (int i = 0; i < bosses.Length; i++)
            {
                BossController boss = bosses[i];
                if (boss == null || IsRegistered(boss)) continue;

                Register(boss);
                addedAny = true;
            }

            // Chỉ cảnh báo, không tự tạo EventSystem: scene đã có thì không được tạo thêm.
            if (addedAny && FindFirstObjectByType<EventSystem>() == null)
            {
                Debug.LogWarning("[BossDefeatReportPanel] Scene có Boss nhưng chưa có EventSystem — nút Thử lại/Đóng sẽ không bấm được.");
            }
        }

        private bool IsRegistered(BossController boss)
        {
            for (int i = 0; i < _registrations.Count; i++)
            {
                if (_registrations[i].Boss == boss) return true;
            }
            return false;
        }

        private void Register(BossController boss)
        {
            var registration = new Registration
            {
                Boss = boss,
                OnFailed = error => HandleReportFailed(boss),
                OnConfirmed = () => HandleReportConfirmed(boss)
            };

            boss.DefeatReportFailed += registration.OnFailed;
            boss.DefeatReportConfirmed += registration.OnConfirmed;
            _registrations.Add(registration);
        }

        private static void Unsubscribe(Registration registration)
        {
            // Event là field C# thuần nên gỡ được kể cả khi GameObject của boss đã bị hủy.
            registration.Boss.DefeatReportFailed -= registration.OnFailed;
            registration.Boss.DefeatReportConfirmed -= registration.OnConfirmed;
        }

        /// <summary>Boss bị hủy (đổi scene, Destroy) → gỡ đăng ký; bảng đang chờ boss đó thì ẩn đi vì không còn gì để thử lại.</summary>
        private void PruneDestroyedBosses()
        {
            for (int i = _registrations.Count - 1; i >= 0; i--)
            {
                Registration registration = _registrations[i];
                if (registration.Boss != null) continue;

                Unsubscribe(registration);
                _registrations.RemoveAt(i);
            }

            bool activeBossDestroyed = !ReferenceEquals(_activeBoss, null) && _activeBoss == null;
            if (!activeBossDestroyed) return;

            _activeBoss = null;
            if (_model.State == BossDefeatReportPanelState.Failed || _model.State == BossDefeatReportPanelState.Sending)
            {
                _model.Close();
            }
        }

        private void UnregisterAll()
        {
            for (int i = 0; i < _registrations.Count; i++) Unsubscribe(_registrations[i]);
            _registrations.Clear();
            _activeBoss = null;
        }

        // ── Sự kiện từ boss → model ─────────────────────────────────────────────────

        private void HandleReportFailed(BossController boss)
        {
            _activeBoss = boss;
            _model.ReportFailed();
        }

        private void HandleReportConfirmed(BossController boss)
        {
            // Một boss khác đang chờ thử lại thì không để tín hiệu xác nhận của boss này tắt bảng của nó.
            if (_activeBoss != null && _activeBoss != boss && _model.IsPanelVisible) return;

            if (_activeBoss == boss) _activeBoss = null;
            _model.ReportConfirmed();
        }

        // ── Nút bấm: chỉ gửi ý định ─────────────────────────────────────────────────

        private void OnRetryClicked()
        {
            // Model chặn bấm đúp: chỉ lần bấm đầu (đang Failed) mới đi tiếp.
            if (!_model.TryBeginRetry()) return;

            BossController boss = _activeBoss;
            if (boss == null)
            {
                _model.RetryReturnedWithoutResult();
                return;
            }

            _ = RetryAsync(boss);
        }

        private void OnCloseClicked()
        {
            _model.Close();
        }

        /// <summary>
        /// Gọi BossController.RetryDefeatReportAsync. Kết quả thật tới qua DefeatReportConfirmed / DefeatReportFailed
        /// (đã đăng ký ở trên). Chỉ khi hàm này xong mà không có tín hiệu nào thì mới gỡ trạng thái "Đang gửi...".
        /// </summary>
        private async Task RetryAsync(BossController boss)
        {
            try
            {
                await boss.RetryDefeatReportAsync();
            }
            catch (Exception error)
            {
                Debug.LogWarning($"[BossDefeatReportPanel] Thử lại báo boss bị lỗi: {error.Message}");
                _model.ReportFailed();
                return;
            }

            _model.RetryReturnedWithoutResult();
        }

        // ── Vẽ theo model ───────────────────────────────────────────────────────────

        private void Render()
        {
            if (_card == null || _toast == null) return;

            _card.SetActive(_model.IsPanelVisible);
            _toast.SetActive(_model.IsConfirmedToastVisible);
            _retryButton.interactable = _model.IsRetryInteractable;
            _retryLabel.text = _model.RetryButtonText;
        }

        // ── Dựng UI bằng code ───────────────────────────────────────────────────────

        private void BuildUi()
        {
            _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            var canvasGo = new GameObject("BossDefeatReportCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGo.transform.SetParent(transform, false);

            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = CanvasSortingOrder;

            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = ReferenceResolution;
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            BuildCard(canvasGo.transform);
            BuildToast(canvasGo.transform);
        }

        private void BuildCard(Transform canvasTransform)
        {
            _card = new GameObject("Card", typeof(RectTransform), typeof(Image));
            _card.transform.SetParent(canvasTransform, false);

            var cardRt = (RectTransform)_card.transform;
            cardRt.anchorMin = new Vector2(0.5f, 1f);
            cardRt.anchorMax = new Vector2(0.5f, 1f);
            cardRt.pivot = new Vector2(0.5f, 1f);
            cardRt.anchoredPosition = new Vector2(0f, -CardTopMargin);
            cardRt.sizeDelta = new Vector2(CardWidth, CardHeight);
            _card.GetComponent<Image>().color = CardColor; // raycastTarget bật: chạm vào bảng không lọt xuống nút chơi bên dưới

            float buttonTop = ButtonBottomMargin + BossDefeatReportPanelModel.ButtonHeightPixels;

            // Thông báo: phần trên của bảng, chừa chỗ cho hàng nút ở dưới.
            Text message = CreateText("Message", _card.transform, BossDefeatReportPanelModel.FailedMessage, MessageFontSize, TextAnchor.MiddleCenter);
            var messageRt = (RectTransform)message.transform;
            messageRt.anchorMin = Vector2.zero;
            messageRt.anchorMax = Vector2.one;
            messageRt.offsetMin = new Vector2(CardPadding, buttonTop + MessageBottomGap);
            messageRt.offsetMax = new Vector2(-CardPadding, -CardPadding);

            // Hai nút nằm cạnh nhau ở đáy bảng, mỗi nút cao ButtonHeightPixels (≥ 96 để chạm bằng ngón tay).
            _retryButton = CreateButton("RetryButton", _card.transform, BossDefeatReportPanelModel.RetryLabel, RetryColor, true, out _retryLabel);
            _retryButton.onClick.AddListener(OnRetryClicked);

            Button closeButton = CreateButton("CloseButton", _card.transform, BossDefeatReportPanelModel.CloseLabel, CloseColor, false, out Text closeLabel);
            closeButton.onClick.AddListener(OnCloseClicked);
        }

        private void BuildToast(Transform canvasTransform)
        {
            _toast = new GameObject("ConfirmedToast", typeof(RectTransform), typeof(Image));
            _toast.transform.SetParent(canvasTransform, false);

            var toastRt = (RectTransform)_toast.transform;
            toastRt.anchorMin = new Vector2(0.5f, 1f);
            toastRt.anchorMax = new Vector2(0.5f, 1f);
            toastRt.pivot = new Vector2(0.5f, 1f);
            toastRt.anchoredPosition = new Vector2(0f, -ToastTopMargin);
            toastRt.sizeDelta = new Vector2(ToastWidth, ToastHeight);

            var background = _toast.GetComponent<Image>();
            background.color = ToastColor;
            background.raycastTarget = false; // dòng thông báo không được chặn thao tác chơi

            Text text = CreateText("Text", _toast.transform, BossDefeatReportPanelModel.ConfirmedMessage, ToastFontSize, TextAnchor.MiddleCenter);
            var textRt = (RectTransform)text.transform;
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.offsetMin = new Vector2(CardPadding / 2f, 0f);
            textRt.offsetMax = new Vector2(-CardPadding / 2f, 0f);
        }

        private Text CreateText(string name, Transform parent, string content, int fontSize, TextAnchor alignment)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);

            var text = go.GetComponent<Text>();
            text.font = _font;
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.color = Color.white;
            text.raycastTarget = false;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            text.text = content;
            return text;
        }

        private Button CreateButton(string name, Transform parent, string label, Color color, bool isLeft, out Text labelText)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);

            // Nút trái chiếm nửa trái, nút phải chiếm nửa phải của hàng nút; neo ở đáy bảng, cao đúng ButtonHeightPixels.
            float halfGap = ButtonGap / 2f;
            var rt = (RectTransform)go.transform;
            rt.anchorMin = new Vector2(isLeft ? 0f : 0.5f, 0f);
            rt.anchorMax = new Vector2(isLeft ? 0.5f : 1f, 0f);
            rt.pivot = new Vector2(0.5f, 0f);
            rt.offsetMin = new Vector2(isLeft ? CardPadding : halfGap, ButtonBottomMargin);
            rt.offsetMax = new Vector2(isLeft ? -halfGap : -CardPadding, ButtonBottomMargin + BossDefeatReportPanelModel.ButtonHeightPixels);

            var image = go.GetComponent<Image>();
            image.color = color;

            var button = go.GetComponent<Button>();
            button.targetGraphic = image;
            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(0.9f, 0.9f, 0.9f, 1f);
            colors.pressedColor = new Color(0.72f, 0.72f, 0.72f, 1f);
            colors.disabledColor = ButtonDisabledColor;
            button.colors = colors;

            labelText = CreateText("Label", go.transform, label, ButtonFontSize, TextAnchor.MiddleCenter);
            var labelRt = (RectTransform)labelText.transform;
            labelRt.anchorMin = Vector2.zero;
            labelRt.anchorMax = Vector2.one;
            labelRt.offsetMin = Vector2.zero;
            labelRt.offsetMax = Vector2.zero;

            return button;
        }
    }
}
