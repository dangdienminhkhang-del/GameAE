using Game.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Tests.EditMode.UI
{
    [TestFixture]
    public class FontTests
    {
        private GameObject _go;
        private Text _text;

        [SetUp]
        public void SetUp()
        {
            _go = new GameObject("FontTest_Text");
            _text = _go.AddComponent<Text>();
        }

        [TearDown]
        public void TearDown()
        {
            if (_go != null) Object.DestroyImmediate(_go);
        }

        [Test]
        public void UiSkin_Fonts_CanBeLoadedFromResources()
        {
            var defaultFont = UiSkin.DefaultFont;
            var boldFont = UiSkin.BoldFont;

            Assert.IsNotNull(defaultFont, "UiSkin.DefaultFont không được null khi có font trong Resources/UISkin/Fonts!");
            Assert.IsNotNull(boldFont, "UiSkin.BoldFont không được null!");
        }

        [Test]
        public void UiSkin_ApplyFont_SetsFontOnText()
        {
            bool applied = UiSkin.ApplyFont(_text, bold: false);
            Assert.IsTrue(applied, "UiSkin.ApplyFont phải trả về true khi áp dụng thành công.");
            Assert.AreEqual(UiSkin.DefaultFont, _text.font);

            bool appliedBold = UiSkin.ApplyFont(_text, bold: true);
            Assert.IsTrue(appliedBold);
            Assert.AreEqual(UiSkin.BoldFont, _text.font);
        }

        [Test]
        public void VietnameseContractSentence_AssignsSuccessfully()
        {
            const string contractSentence = "Quyền truy cập đã niêm phong. Hạ Giữ Neo, tuyến kế tiếp mở. Ặ Ễ Ỡ Ữ";
            UiSkin.ApplyFont(_text);
            _text.text = contractSentence;

            Assert.AreEqual(contractSentence, _text.text);
            Assert.AreEqual(contractSentence.Length, _text.text.Length);
        }
    }
}
