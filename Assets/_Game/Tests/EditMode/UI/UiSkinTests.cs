using Game.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Tests.EditMode.UI
{
    [TestFixture]
    public class UiSkinTests
    {
        private GameObject _go;
        private Image _img;

        [SetUp]
        public void SetUp()
        {
            _go = new GameObject("TestImage");
            _img = _go.AddComponent<Image>();
        }

        [TearDown]
        public void TearDown()
        {
            if (_go != null) Object.DestroyImmediate(_go);
        }

        [Test]
        public void ArtPixel_IsThree()
        {
            Assert.AreEqual(3, UiSkin.ArtPixel, "UiSkin.ArtPixel phải bằng 3 (1 art pixel = 3 canvas pixels).");
        }

        [Test]
        public void BasicSprites_AreAvailable()
        {
            Assert.IsTrue(UiSkin.Available, "UiSkin.Available phải trả về true khi có đủ Panel, Button, Frame.");
        }

        [Test]
        public void NewHelperMethods_ApplySuccessfully()
        {
            Assert.IsTrue(UiSkin.Slot(_img), "UiSkin.Slot phải áp dụng thành công.");
            Assert.IsNotNull(_img.sprite);
            Assert.AreEqual(Image.Type.Sliced, _img.type);

            Assert.IsTrue(UiSkin.Row(_img), "UiSkin.Row phải áp dụng thành công.");
            Assert.IsTrue(UiSkin.Header(_img), "UiSkin.Header phải áp dụng thành công.");
            Assert.IsTrue(UiSkin.Toast(_img), "UiSkin.Toast phải áp dụng thành công.");
            Assert.IsTrue(UiSkin.Panel(_img), "UiSkin.Panel phải áp dụng thành công.");
            Assert.IsTrue(UiSkin.Button(_img), "UiSkin.Button phải áp dụng thành công.");
            Assert.IsTrue(UiSkin.IconButton(_img), "UiSkin.IconButton phải áp dụng thành công.");
            Assert.IsTrue(UiSkin.Inset(_img), "UiSkin.Inset phải áp dụng thành công.");
        }

        [Test]
        public void CoinIcon_IsNotNull()
        {
            Assert.IsNotNull(UiSkin.CoinIcon, "UiSkin.CoinIcon không được null.");
        }
    }
}
