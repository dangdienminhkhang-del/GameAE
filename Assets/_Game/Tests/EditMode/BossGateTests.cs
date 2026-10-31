using Game.PvE.Environment;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Game.Tests.EditMode
{
    /// <summary>Logic cổng hầm boss (BossGate): giữ trước cổng đủ lâu thì mở; thay đổi va chạm đóng/mở.</summary>
    public class BossGateTests
    {
        private GameObject _root;
        private BossGate _gate;
        private BoxCollider2D _closed;
        private BoxCollider2D _open;

        [SetUp]
        public void Setup()
        {
            _root = new GameObject("BossGateUnderTest");
            _closed = _root.AddComponent<BoxCollider2D>();
            var openGo = new GameObject("Open");
            openGo.transform.SetParent(_root.transform, false);
            _open = openGo.AddComponent<BoxCollider2D>();
            _gate = _root.AddComponent<BossGate>();

            var so = new SerializedObject(_gate);
            var closed = so.FindProperty("closedColliders"); closed.arraySize = 1; closed.GetArrayElementAtIndex(0).objectReferenceValue = _closed;
            var open = so.FindProperty("openColliders"); open.arraySize = 1; open.GetArrayElementAtIndex(0).objectReferenceValue = _open;
            so.FindProperty("holdSeconds").floatValue = 1.2f;
            so.FindProperty("openSeconds").floatValue = 0.8f;
            so.ApplyModifiedPropertiesWithoutUndo();
            _gate.ResetClosed();   // Unity không tự gọi OnEnable ngoài chế độ chạy
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_root);
        }

        [Test]
        public void StartsClosed_BlockingTheWholeCorridor()
        {
            Assert.AreEqual(BossGate.GateState.Closed, _gate.State);
            Assert.IsTrue(_closed.enabled, "Cổng đóng phải bật collider chặn kín.");
            Assert.IsFalse(_open.enabled, "Cổng đóng không được bật collider của trạng thái mở.");
        }

        [Test]
        public void NobodyInRange_NeverOpens()
        {
            for (int i = 0; i < 600; i++) _gate.Tick(0.1f, false);
            Assert.AreEqual(BossGate.GateState.Closed, _gate.State);
            Assert.AreEqual(0f, _gate.Progress, 0.0001f);
        }

        [Test]
        public void PlayerHoldsLongEnough_GateOpens_AndOnlyPillarsRemainSolid()
        {
            for (int i = 0; i < 11; i++) _gate.Tick(0.1f, true);          // 1.1s: chưa đủ 1.2s
            Assert.AreEqual(BossGate.GateState.Closed, _gate.State, "Chưa đủ thời gian giữ thì cổng chưa được mở.");
            _gate.Tick(0.2f, true);                                        // đủ
            Assert.AreEqual(BossGate.GateState.Opening, _gate.State);
            for (int i = 0; i < 9; i++) _gate.Tick(0.1f, true);            // 0.9s > 0.8s
            Assert.IsTrue(_gate.IsOpen);
            Assert.IsFalse(_closed.enabled, "Cổng mở phải tắt collider chặn kín.");
            Assert.IsTrue(_open.enabled, "Cổng mở vẫn phải giữ trụ đá và thanh ngang.");
        }

        [Test]
        public void LeavingMidway_ProgressDecaysBackToZero_WithoutOpening()
        {
            for (int i = 0; i < 6; i++) _gate.Tick(0.1f, true);           // 0.6s ~ 50%
            Assert.Greater(_gate.Progress, 0.4f);
            float before = _gate.Progress;
            _gate.Tick(0.1f, false);
            Assert.Less(_gate.Progress, before, "Rời đi thì tiến độ phải tụt.");
            for (int i = 0; i < 50; i++) _gate.Tick(0.1f, false);
            Assert.AreEqual(0f, _gate.Progress, 0.0001f);
            Assert.AreEqual(BossGate.GateState.Closed, _gate.State);
        }

        [Test]
        public void ResetClosed_ClosesAnOpenGateAgain()
        {
            for (int i = 0; i < 30; i++) _gate.Tick(0.1f, true);
            Assert.IsTrue(_gate.IsOpen);
            _gate.ResetClosed();
            Assert.AreEqual(BossGate.GateState.Closed, _gate.State);
            Assert.IsTrue(_closed.enabled);
            Assert.IsFalse(_open.enabled);
            Assert.AreEqual(0f, _gate.Progress, 0.0001f);
        }
    }
}
