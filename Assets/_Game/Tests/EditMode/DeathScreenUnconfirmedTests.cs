using System.Collections.Generic;
using System.Reflection;
using Game.Core.Character;
using Game.UI.Death;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Tests.EditMode
{
    /// <summary>
    /// Màn "chưa lưu được cái chết" (docs/09 §1.3): nhân vật vẫn tính là đã chết; chỉ có Thử lại + Thoát,
    /// không có nút nào cho chơi tiếp hay tạo tài khoản mới. Xác nhận được → màn "BẠN ĐÃ CHẾT" như cũ.
    /// Controller để INACTIVE khi tạo nên Awake/OnEnable không chạy (Awake tạo AccountResetService thật — không cần cho test chữ);
    /// các field [SerializeField] được gán bằng reflection.
    /// </summary>
    public class DeathScreenUnconfirmedTests
    {
        private readonly List<GameObject> _created = new List<GameObject>();

        private DeathScreenController _controller;
        private GameObject _panel;
        private Text _title;
        private Text _details;
        private Button _newAccount;
        private Button _quit;
        private Button _retry;

        private T Make<T>(string name) where T : Component
        {
            var go = new GameObject(name);
            _created.Add(go);
            return go.AddComponent<T>();
        }

        private void SetField(string name, object value)
        {
            FieldInfo field = typeof(DeathScreenController).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(field, $"DeathScreenController không còn field '{name}'");
            field.SetValue(_controller, value);
        }

        [SetUp]
        public void SetUp()
        {
            var host = new GameObject("DeathScreen");
            host.SetActive(false);
            _created.Add(host);
            _controller = host.AddComponent<DeathScreenController>();

            _panel = new GameObject("Panel");
            _created.Add(_panel);
            _panel.SetActive(false);

            _title = Make<Text>("Title");
            _details = Make<Text>("Details");
            _newAccount = Make<Button>("NewAccount");
            _quit = Make<Button>("Quit");
            _retry = Make<Button>("Retry");

            // Trạng thái đầu giống sau Awake: Thử lại ẩn; "Tạo tài khoản mới" đang hiện để chứng minh màn này ẩn nó đi.
            _retry.gameObject.SetActive(false);
            _newAccount.gameObject.SetActive(true);
            _quit.gameObject.SetActive(true);

            SetField("panelRoot", _panel);
            SetField("titleText", _title);
            SetField("detailsText", _details);
            SetField("newAccountButton", _newAccount);
            SetField("quitButton", _quit);
            SetField("retryButton", _retry);
        }

        [TearDown]
        public void TearDown()
        {
            foreach (GameObject go in _created)
            {
                if (go != null) Object.DestroyImmediate(go);
            }
            _created.Clear();
        }

        [Test]
        public void ShowUnconfirmed_UsesTheNewTitleAndExplanation()
        {
            _controller.ShowUnconfirmed(new DeathInfo { isDead = true });

            Assert.IsTrue(_panel.activeSelf);
            Assert.AreEqual("BẠN ĐÃ BỊ HẠ GỤC", _title.text);
            Assert.AreEqual(
                "Chưa lưu được kết quả vì mất kết nối. Nhân vật vẫn tính là đã chết.\nHãy kết nối lại, game sẽ tự lưu.",
                _details.text);
        }

        [Test]
        public void ShowUnconfirmed_KeepsRetryAndQuit_AndHidesNewAccount()
        {
            _controller.ShowUnconfirmed(new DeathInfo { isDead = true });

            Assert.IsTrue(_retry.gameObject.activeSelf, "Phải có nút Thử lại");
            Assert.IsTrue(_retry.interactable);
            Assert.IsTrue(_quit.gameObject.activeSelf, "Phải có nút Thoát");
            Assert.IsFalse(_newAccount.gameObject.activeSelf,
                "Cái chết chưa chốt: không có nút tạo tài khoản mới, và không có nút nào cho chơi tiếp");
        }

        [Test]
        public void ShowUnconfirmed_CalledAgain_ReenablesRetry_AndRestoresTheText()
        {
            // Mỗi lần thử lại (tự động hoặc bấm tay) vẫn lỗi, bootstrap phát lại Unconfirmed → màn phải trở về trạng thái chờ.
            _controller.ShowUnconfirmed(new DeathInfo { isDead = true });
            _retry.interactable = false;
            _details.text = "Đang thử lưu lại...";

            _controller.ShowUnconfirmed(new DeathInfo { isDead = true });

            Assert.IsTrue(_retry.interactable);
            Assert.AreEqual(DeathScreenController.UnconfirmedDetails, _details.text);
        }

        [Test]
        public void Show_AfterUnconfirmed_SwitchesToTheNormalDeathScreen()
        {
            _controller.ShowUnconfirmed(new DeathInfo { isDead = true });

            _controller.Show(new DeathInfo
            {
                isDead = true,
                deathCause = "Enemy",
                finalLevel = 5,
                finalHighestFloor = 2,
                totalPlayTimeSeconds = 125
            });

            Assert.AreEqual("BẠN ĐÃ CHẾT", _title.text);
            Assert.IsFalse(_retry.gameObject.activeSelf, "Đã xác nhận → không còn gì để thử lại");
            StringAssert.Contains("Quái vật", _details.text);
            StringAssert.Contains("02:05", _details.text);
            StringAssert.DoesNotContain("mất kết nối", _details.text);
        }
    }
}
