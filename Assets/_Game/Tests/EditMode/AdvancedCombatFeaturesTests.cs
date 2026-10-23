using Game.Core.Combat;
using Game.PvE.Camera;
using Game.PvE.Environment;
using Game.PvE.Player;
using NUnit.Framework;
using UnityEngine;

namespace Game.Tests.EditMode
{
    [TestFixture]
    public class AdvancedCombatFeaturesTests
    {
        [Test]
        public void ComboRankMath_CalculatesRanksCorrectly()
        {
            Assert.AreEqual(ComboRank.None, ComboRankMath.GetRank(0));
            Assert.AreEqual(ComboRank.D, ComboRankMath.GetRank(1));
            Assert.AreEqual(ComboRank.D, ComboRankMath.GetRank(4));
            Assert.AreEqual(ComboRank.C, ComboRankMath.GetRank(5));
            Assert.AreEqual(ComboRank.C, ComboRankMath.GetRank(9));
            Assert.AreEqual(ComboRank.B, ComboRankMath.GetRank(10));
            Assert.AreEqual(ComboRank.B, ComboRankMath.GetRank(19));
            Assert.AreEqual(ComboRank.A, ComboRankMath.GetRank(20));
            Assert.AreEqual(ComboRank.A, ComboRankMath.GetRank(34));
            Assert.AreEqual(ComboRank.S, ComboRankMath.GetRank(35));
            Assert.AreEqual(ComboRank.S, ComboRankMath.GetRank(49));
            Assert.AreEqual(ComboRank.SSS, ComboRankMath.GetRank(50));
            Assert.AreEqual(ComboRank.SSS, ComboRankMath.GetRank(100));
        }

        [Test]
        public void ComboRankMath_MultipliersIncreaseWithRank()
        {
            double multNone = ComboRankMath.GetDamageMultiplier(ComboRank.None);
            double multD = ComboRankMath.GetDamageMultiplier(ComboRank.D);
            double multC = ComboRankMath.GetDamageMultiplier(ComboRank.C);
            double multB = ComboRankMath.GetDamageMultiplier(ComboRank.B);
            double multA = ComboRankMath.GetDamageMultiplier(ComboRank.A);
            double multS = ComboRankMath.GetDamageMultiplier(ComboRank.S);
            double multSSS = ComboRankMath.GetDamageMultiplier(ComboRank.SSS);

            Assert.AreEqual(1.0, multNone);
            Assert.AreEqual(1.0, multD);
            Assert.Greater(multC, multD);
            Assert.Greater(multB, multC);
            Assert.Greater(multA, multB);
            Assert.Greater(multS, multA);
            Assert.Greater(multSSS, multS);
        }

        [Test]
        public void ComboTracker_AddHit_IncrementsCountAndUpdatesRank()
        {
            GameObject go = new GameObject("Player_Test");
            ComboTracker tracker = go.AddComponent<ComboTracker>();

            Assert.AreEqual(0, tracker.ComboCount);
            Assert.AreEqual(ComboRank.None, tracker.CurrentRank);

            tracker.AddHit();
            Assert.AreEqual(1, tracker.ComboCount);
            Assert.AreEqual(ComboRank.D, tracker.CurrentRank);

            for (int i = 0; i < 9; i++) tracker.AddHit();
            Assert.AreEqual(10, tracker.ComboCount);
            Assert.AreEqual(ComboRank.B, tracker.CurrentRank);

            tracker.ResetCombo();
            Assert.AreEqual(0, tracker.ComboCount);
            Assert.AreEqual(ComboRank.None, tracker.CurrentRank);

            Object.DestroyImmediate(go);
        }

        [Test]
        public void AdrenalineController_AddsAndClampsAtMax()
        {
            GameObject go = new GameObject("Player_Test");
            AdrenalineController adr = go.AddComponent<AdrenalineController>();

            Assert.AreEqual(0f, adr.CurrentAdrenaline);
            Assert.IsFalse(adr.IsReady);

            adr.AddAdrenaline(50f);
            Assert.AreEqual(50f, adr.CurrentAdrenaline);
            Assert.IsFalse(adr.IsReady);

            adr.AddAdrenaline(80f);
            Assert.AreEqual(100f, adr.CurrentAdrenaline);
            Assert.IsTrue(adr.IsReady);
            Assert.AreEqual(1.0f, adr.AdrenalineRatio);

            Object.DestroyImmediate(go);
        }

        [Test]
        public void CameraFollow_ShakeParametersAcceptValidValues()
        {
            GameObject camObj = new GameObject("Camera_Test");
            CameraFollow cam = camObj.AddComponent<CameraFollow>();

            cam.Shake(0.2f, 0.3f);
            Assert.DoesNotThrow(() => cam.Shake(0.1f, 0.1f));

            Object.DestroyImmediate(camObj);
        }
    }
}
