using Game.Core.Events;
using Game.Core.Time;
using Game.UI.Time;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Tests.EditMode.UI
{
    [TestFixture]
    public class TimeOfDayHUDTests
    {
        private GameObject _hudObj;
        private TimeOfDayHUD _hud;
        private Text _text;
        private Image _image;
        private Sprite _dawn;
        private Sprite _morning;
        private Sprite _noon;
        private Sprite _afternoon;
        private Sprite _evening;
        private Sprite _night;

        [SetUp]
        public void SetUp()
        {
            _hudObj = new GameObject("TimeHUD_Test");
            _hud = _hudObj.AddComponent<TimeOfDayHUD>();
            _text = _hudObj.AddComponent<Text>();

            var iconObj = new GameObject("Icon_Test");
            iconObj.transform.SetParent(_hudObj.transform);
            _image = iconObj.AddComponent<Image>();

            _hud.Configure(_text, _image);

            _dawn = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 4, 4), Vector2.zero);
            _morning = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 4, 4), Vector2.zero);
            _noon = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 4, 4), Vector2.zero);
            _afternoon = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 4, 4), Vector2.zero);
            _evening = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 4, 4), Vector2.zero);
            _night = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 4, 4), Vector2.zero);

            _hud.SetPhaseSprites(_dawn, _morning, _noon, _afternoon, _evening, _night);
        }

        [TearDown]
        public void TearDown()
        {
            if (_hudObj != null) Object.DestroyImmediate(_hudObj);
            if (_dawn != null) Object.DestroyImmediate(_dawn);
            if (_morning != null) Object.DestroyImmediate(_morning);
            if (_noon != null) Object.DestroyImmediate(_noon);
            if (_afternoon != null) Object.DestroyImmediate(_afternoon);
            if (_evening != null) Object.DestroyImmediate(_evening);
            if (_night != null) Object.DestroyImmediate(_night);
        }

        [Test]
        public void PhaseName_ReturnsCorrectVietnameseString()
        {
            Assert.AreEqual("Sáng sớm", TimeOfDayHUD.PhaseName(TimeOfDay.Dawn));
            Assert.AreEqual("Buổi sáng", TimeOfDayHUD.PhaseName(TimeOfDay.Morning));
            Assert.AreEqual("Buổi trưa", TimeOfDayHUD.PhaseName(TimeOfDay.Noon));
            Assert.AreEqual("Buổi chiều", TimeOfDayHUD.PhaseName(TimeOfDay.Afternoon));
            Assert.AreEqual("Chạng vạng", TimeOfDayHUD.PhaseName(TimeOfDay.Evening));
            Assert.AreEqual("Đêm", TimeOfDayHUD.PhaseName(TimeOfDay.Night));
        }

        [Test]
        public void GetPhaseSprite_ReturnsMatchingSprite()
        {
            Assert.AreSame(_dawn, _hud.GetPhaseSprite(TimeOfDay.Dawn));
            Assert.AreSame(_morning, _hud.GetPhaseSprite(TimeOfDay.Morning));
            Assert.AreSame(_noon, _hud.GetPhaseSprite(TimeOfDay.Noon));
            Assert.AreSame(_afternoon, _hud.GetPhaseSprite(TimeOfDay.Afternoon));
            Assert.AreSame(_evening, _hud.GetPhaseSprite(TimeOfDay.Evening));
            Assert.AreSame(_night, _hud.GetPhaseSprite(TimeOfDay.Night));
        }
    }
}
