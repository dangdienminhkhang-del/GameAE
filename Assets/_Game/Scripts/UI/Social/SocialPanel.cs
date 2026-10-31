using System.Collections.Generic;
using Game.Core.Events;
using Game.Core.Social;
using Game.PvE.Social;
using Game.UI.Common;
using Game.UI.Pause;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Game.UI.Social
{
    /// <summary>
    /// Bảng giao diện Xã Hội: Tổ Đội (Party 6 người), Bang Hội (Guild) và Bạn Bè theo chuẩn SAO.
    /// Dựng bằng code thuần dưới Canvas lúc chạy, không cần prefab.
    /// Kế thừa IExclusivePanel để hỗ trợ phím Back trên mobile và đóng độc quyền.
    /// </summary>
    public class SocialPanel : MonoBehaviour, IExclusivePanel, IPanelRoot
    {
        public const int Priority = 39;

        private enum SocialTab { Party = 0, Guild = 1, Friends = 2 }

        private static readonly Color Gold = new Color(0.95f, 0.76f, 0.31f, 1f);
        private static readonly Color Ink = new Color(0.07f, 0.08f, 0.13f, 0.97f);
        private static readonly Color RowBg = new Color(0.14f, 0.16f, 0.24f, 1f);
        private static readonly Color TabActive = new Color(0.22f, 0.55f, 0.8f, 1f);
        private static readonly Color TabInactive = new Color(0.18f, 0.2f, 0.28f, 1f);
        private static readonly Color HpGreen = new Color(0.18f, 0.82f, 0.35f, 1f);

        private Font _font;
        private GameObject _root;
        private SocialTab _currentTab = SocialTab.Party;

        private Button _tabPartyBtn;
        private Button _tabGuildBtn;
        private Button _tabFriendsBtn;
        private Transform _contentContainer;

        public int BackPriority => Priority;
        public bool IsOpen => _root != null && _root.activeSelf;
        public GameObject PanelRoot => _root;
        public void CloseFromBack() => Close();
        public void CloseExclusive() => Close();

        private static SocialPanel s_instance;
        public static SocialPanel Instance => s_instance;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoInit()
        {
            SceneManager.sceneLoaded += (scene, mode) => EnsureOnCanvas();
            EnsureOnCanvas();
        }

        public static void EnsureOnCanvas()
        {
            string currentScene = SceneManager.GetActiveScene().name;
            if (currentScene != "PvE_Gameplay" && currentScene != "Floor02") return;
            if (s_instance != null) return;

            var canvas = GameObject.Find("/Canvas");
            if (canvas == null)
            {
                var canvasComp = FindAnyObjectByType<Canvas>();
                if (canvasComp != null) canvas = canvasComp.gameObject;
            }

            if (canvas != null && canvas.transform.Find("SocialPanel") == null)
            {
                var panelGo = new GameObject("SocialPanel", typeof(RectTransform), typeof(SocialPanel));
                panelGo.transform.SetParent(canvas.transform, false);
            }
        }

        private void Awake()
        {
            s_instance = this;
            _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            Build();
            if (_root != null) _root.SetActive(false);
        }

        private void OnEnable()
        {
            BackKeyRouterHost.Router.Register(this);
            GameEvents.OnSocialPanelRequested += Open;
        }

        private void OnDisable()
        {
            BackKeyRouterHost.Router.Unregister(this);
            GameEvents.OnSocialPanelRequested -= Open;
        }

        private void Update()
        {
            if (!IsOpen) return;

            Keyboard kb = Keyboard.current;
            if (kb != null && kb.escapeKey.wasPressedThisFrame)
            {
                Close();
            }
        }

        public void Open()
        {
            SwitchTab(SocialTab.Party);
            if (_root != null) _root.SetActive(true);
        }

        public void Close()
        {
            if (_root != null) _root.SetActive(false);
        }

        private void SwitchTab(SocialTab tab)
        {
            _currentTab = tab;
            UpdateTabButtons();
            RefreshTabContent();
        }

        private void UpdateTabButtons()
        {
            SetBtnColor(_tabPartyBtn, _currentTab == SocialTab.Party);
            SetBtnColor(_tabGuildBtn, _currentTab == SocialTab.Guild);
            SetBtnColor(_tabFriendsBtn, _currentTab == SocialTab.Friends);
        }

        private void SetBtnColor(Button btn, bool active)
        {
            if (btn != null && btn.targetGraphic is Image img)
            {
                img.color = active ? TabActive : TabInactive;
            }
        }

        private void RefreshTabContent()
        {
            foreach (Transform child in _contentContainer)
            {
                Destroy(child.gameObject);
            }

            switch (_currentTab)
            {
                case SocialTab.Party:
                    BuildPartyTab();
                    break;
                case SocialTab.Guild:
                    BuildGuildTab();
                    break;
                case SocialTab.Friends:
                    BuildFriendsTab();
                    break;
            }
        }

        private void BuildPartyTab()
        {
            var mgr = SocialManager.Instance;
            var members = mgr != null ? mgr.PartyMembers : new List<PartyMember>();

            var header = NewText("Header", _contentContainer, $"TỔ ĐỘI HIỆN TẠI ({members.Count}/{PartyRules.MaxPartySize} Thành Viên - Chuẩn SAO)", 20, Gold, TextAnchor.MiddleLeft);
            header.gameObject.AddComponent<LayoutElement>().minHeight = 35;

            for (int i = 0; i < members.Count; i++)
            {
                var m = members[i];
                var row = NewUi($"Member_{m.playerId}", _contentContainer);
                row.AddComponent<LayoutElement>().minHeight = 55;
                row.AddComponent<Image>().color = RowBg;

                string role = m.isLeader ? "[Trưởng Nhóm]" : "[Thành Viên]";
                string info = $"{role} {m.playerName} (Lv.{m.level})";
                var t = NewText("Info", row.transform, info, 18, Color.white, TextAnchor.MiddleLeft);
                SetAnchors(t.rectTransform, 0.04f, 0.5f, 0.65f, 0.95f);

                // Thanh HP
                var hpBg = NewUi("HpBg", row.transform);
                SetAnchors(hpBg.GetComponent<RectTransform>(), 0.04f, 0.15f, 0.65f, 0.45f);
                hpBg.AddComponent<Image>().color = new Color(0.2f, 0.2f, 0.25f, 1f);

                var hpFill = NewUi("HpFill", hpBg.transform);
                Stretch(hpFill.GetComponent<RectTransform>());
                var img = hpFill.AddComponent<Image>();
                img.color = HpGreen;
                img.type = Image.Type.Filled;
                img.fillMethod = Image.FillMethod.Horizontal;
                img.fillAmount = m.maxHp > 0 ? (float)(m.currentHp / m.maxHp) : 1f;

                var hpTxt = NewText("HpTxt", hpBg.transform, $"{m.currentHp:0}/{m.maxHp:0}", 12, Color.white, TextAnchor.MiddleCenter);
                Stretch(hpTxt.rectTransform);
            }

            // Nút mời bạn bè mẫu (Silica/Lisbeth)
            if (members.Count < PartyRules.MaxPartySize)
            {
                var inviteBtn = NewButton("InviteSample", _contentContainer, "+ Mời thêm Silica (Lv.12) vào đội", 18, new Color(0.2f, 0.6f, 0.4f, 1f), () =>
                {
                    if (mgr != null)
                    {
                        mgr.TryInviteMember("silica", "Silica (Thuần Thú Sư)", 12, 210);
                        RefreshTabContent();
                    }
                });
                inviteBtn.gameObject.AddComponent<LayoutElement>().minHeight = 45;
            }

            // Nút rời đội
            if (members.Count > 1)
            {
                var leaveBtn = NewButton("LeavePartyBtn", _contentContainer, "Rời Khỏi Tổ Đội", 18, new Color(0.7f, 0.25f, 0.25f, 1f), () =>
                {
                    if (mgr != null)
                    {
                        mgr.LeaveParty();
                        RefreshTabContent();
                    }
                });
                leaveBtn.gameObject.AddComponent<LayoutElement>().minHeight = 45;
            }
        }

        private void BuildGuildTab()
        {
            var header = NewText("Header", _contentContainer, "BANG HỘI: Huyết Kỵ Đoàn (Knights of the Blood - KoB)", 22, Gold, TextAnchor.MiddleLeft);
            header.gameObject.AddComponent<LayoutElement>().minHeight = 40;

            string guildDetails = "Thủ Lĩnh: Heathcliff (Paladin Lá Chắn Thánh)\n" +
                                 "Phó Thủ Lĩnh: Asuna (Tia Chớp Bạc)\n" +
                                 "Thành Viên: 4/30  |  Quy mô: Bang Hội Chinh Phục Tầng Cao (Clearer Guild)\n\n" +
                                 "Danh sách thành viên ưu tú:\n" +
                                 " • Heathcliff (Cấp 55) - Trực tuyến\n" +
                                 " • Asuna (Cấp 48) - Trực tuyến (Đang đi cùng bạn)\n" +
                                 " • Daizen (Cấp 44) - Trực tuyến\n" +
                                 " • Godfree (Cấp 42) - Ngoại tuyến\n\n" +
                                 "[LƯU Ý DỊCH VỤ ONLINE]: Tính năng Bang hội mạng thời gian thực cần kích hoạt UGS Groups / Cloud Code trên Dashboard.";

            var desc = NewText("Desc", _contentContainer, guildDetails, 18, Color.white, TextAnchor.UpperLeft);
            desc.gameObject.AddComponent<LayoutElement>().minHeight = 220;
        }

        private void BuildFriendsTab()
        {
            var header = NewText("Header", _contentContainer, "DANH SÁCH BẠN BÈ (FRIENDS LIST)", 22, Gold, TextAnchor.MiddleLeft);
            header.gameObject.AddComponent<LayoutElement>().minHeight = 40;

            string[] friends =
            {
                "Klein - Phong Lâm Hỏa Sơn  |  Cấp 14  |  Trực tuyến (Tầng 1)",
                "Agil - Tiệm Rìu Chiến      |  Cấp 15  |  Trực tuyến (Tầng 1)",
                "Lisbeth - Thợ Rèn          |  Cấp 14  |  Trực tuyến (Tầng 2)",
                "Silica - Thuần Thú Sư      |  Cấp 12  |  Ngoại tuyến (Tầng 2)"
            };

            for (int i = 0; i < friends.Length; i++)
            {
                var row = NewUi($"Friend_{i}", _contentContainer);
                row.AddComponent<LayoutElement>().minHeight = 45;
                row.AddComponent<Image>().color = RowBg;

                var t = NewText("Txt", row.transform, friends[i], 18, Color.white, TextAnchor.MiddleLeft);
                SetAnchors(t.rectTransform, 0.04f, 0f, 0.96f, 1f);
            }

            var note = NewText("Note", _contentContainer, "\n[LƯU Ý DỊCH VỤ ONLINE]: Đồng bộ bạn bè qua UGS Friends SDK (Cần bật Friends trên Unity Dashboard).", 16, Color.yellow, TextAnchor.MiddleLeft);
            note.gameObject.AddComponent<LayoutElement>().minHeight = 40;
        }

        private void Build()
        {
            _root = NewUi("SocialPanelRoot", transform);
            Stretch(_root.GetComponent<RectTransform>());
            _root.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.65f);

            GameObject border = NewUi("Border", _root.transform);
            SetAnchors(border.GetComponent<RectTransform>(), 0.15f, 0.12f, 0.85f, 0.88f);
            border.AddComponent<PanelSizeClamp>();
            Image borderImg = border.AddComponent<Image>();
            borderImg.color = Gold;

            Button topClose = NewButton("TopClose", border.transform, "X", 32, new Color(0.85f, 0.25f, 0.25f, 0.95f), Close);
            var tcRt = topClose.GetComponent<RectTransform>();
            tcRt.anchorMin = tcRt.anchorMax = tcRt.pivot = new Vector2(1f, 1f);
            tcRt.anchoredPosition = new Vector2(-8f, -8f);
            tcRt.sizeDelta = new Vector2(60f, 60f);

            GameObject win = NewUi("Window", border.transform);
            RectTransform wr = win.GetComponent<RectTransform>();
            Stretch(wr);
            wr.offsetMin = new Vector2(4, 4);
            wr.offsetMax = new Vector2(-4, -4);
            Image winImg = win.AddComponent<Image>();
            winImg.color = Ink;

            var title = NewText("Title", win.transform, "HỆ THỐNG XÃ HỘI (SOCIAL)", 30, Gold, TextAnchor.MiddleLeft);
            SetAnchors(title.rectTransform, 0.05f, 0.88f, 0.55f, 0.98f);

            // 3 Tab Buttons
            _tabPartyBtn = NewButton("TabParty", win.transform, "Tổ Đội (Party)", 18, TabActive, () => SwitchTab(SocialTab.Party));
            SetAnchors(_tabPartyBtn.GetComponent<RectTransform>(), 0.05f, 0.78f, 0.33f, 0.87f);

            _tabGuildBtn = NewButton("TabGuild", win.transform, "Bang Hội (Guild)", 18, TabInactive, () => SwitchTab(SocialTab.Guild));
            SetAnchors(_tabGuildBtn.GetComponent<RectTransform>(), 0.36f, 0.78f, 0.64f, 0.87f);

            _tabFriendsBtn = NewButton("TabFriends", win.transform, "Bạn Bè (Friends)", 18, TabInactive, () => SwitchTab(SocialTab.Friends));
            SetAnchors(_tabFriendsBtn.GetComponent<RectTransform>(), 0.67f, 0.78f, 0.95f, 0.87f);

            // Container nội dung cuộn được
            GameObject scrollGo = NewUi("ContentScrollView", win.transform);
            SetAnchors(scrollGo.GetComponent<RectTransform>(), 0.05f, 0.05f, 0.95f, 0.75f);

            var vlg = scrollGo.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 8;
            vlg.childForceExpandHeight = false;
            vlg.childControlHeight = true;
            vlg.childControlWidth = true;

            _contentContainer = scrollGo.transform;
        }

        private GameObject NewUi(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go;
        }

        private Text NewText(string name, Transform parent, string text, int size, Color color, TextAnchor anchor)
        {
            var go = NewUi(name, parent);
            var t = go.AddComponent<Text>();
            t.text = text;
            t.font = _font;
            t.fontSize = size;
            t.color = color;
            t.alignment = anchor;
            t.raycastTarget = false;
            return t;
        }

        private Button NewButton(string name, Transform parent, string label, int size, Color color, UnityEngine.Events.UnityAction onClick)
        {
            var go = NewUi(name, parent);
            var img = go.AddComponent<Image>();
            img.color = color;
            var btn = go.AddComponent<Button>();
            btn.targetGraphic = img;
            btn.onClick.AddListener(onClick);

            var text = NewText("Label", go.transform, label, size, Color.white, TextAnchor.MiddleCenter);
            Stretch(text.rectTransform);
            return btn;
        }

        private static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        private static void SetAnchors(RectTransform rt, float minX, float minY, float maxX, float maxY)
        {
            rt.anchorMin = new Vector2(minX, minY);
            rt.anchorMax = new Vector2(maxX, maxY);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }
    }
}
