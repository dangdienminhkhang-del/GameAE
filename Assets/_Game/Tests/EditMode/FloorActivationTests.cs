using System.Collections.Generic;
using System.Text.RegularExpressions;
using Game.Core.Floor;
using Game.PvE.Boss;
using Game.PvE.Enemy;
using Game.PvE.Floor;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Game.Tests.EditMode
{
    /// <summary>
    /// P9-1 — khung chuyển tầng chung: chọn binder, bật/tắt nội dung, đặt người chơi,
    /// listener của EnemySpawner và BossController.
    /// Dùng danh sách binder riêng (inject) nên không đụng binder của scene đang mở.
    /// </summary>
    public class FloorActivationTests
    {
        private readonly List<GameObject> _objects = new List<GameObject>();
        private readonly List<ScriptableObject> _assets = new List<ScriptableObject>();
        private readonly List<FloorSceneBinder> _binders = new List<FloorSceneBinder>();

        private sealed class FloorFixture
        {
            public FloorSceneBinder Binder;
            public GameObject Content;
            public Transform Entry;
            public BossController Boss;
        }

        // Listener thật nằm trong nội dung tầng là BossController: khi tầng được bật, boss về vị trí gốc.
        // Fixture đặt boss ra xa vị trí gốc, nên "boss về gốc" = "đã được thông báo OnFloorActivated".
        private static readonly Vector3 BossHome = new Vector3(1f, 2f, 0f);
        private static readonly Vector3 BossAway = new Vector3(99f, 99f, 0f);

        private static bool IsAtHome(FloorFixture f) => f.Boss.transform.position == BossHome;
        private static bool IsAway(FloorFixture f) => f.Boss.transform.position == BossAway;

        [TearDown]
        public void TearDown()
        {
            foreach (var binder in _binders) FloorSceneBinder.Unregister(binder);
            _binders.Clear();

            foreach (var go in _objects)
                if (go != null) Object.DestroyImmediate(go);
            _objects.Clear();

            foreach (var asset in _assets)
                if (asset != null) Object.DestroyImmediate(asset);
            _assets.Clear();
        }

        // ── Helpers ─────────────────────────────────────────────────────────

        private GameObject Track(GameObject go)
        {
            _objects.Add(go);
            return go;
        }

        private FloorDefinition MakeDef(string id, int number)
        {
            var def = ScriptableObject.CreateInstance<FloorDefinition>();
            def.id = id;
            def.floorNumber = number;
            _assets.Add(def);
            return def;
        }

        private FloorFixture MakeFloor(string id, bool contentActive, Vector3 entryPosition)
        {
            var host = Track(new GameObject("Host_" + id));
            var content = new GameObject("Content_" + id);
            content.transform.SetParent(host.transform);

            var entry = new GameObject("Entry_" + id).transform;
            entry.SetParent(host.transform);
            entry.position = entryPosition;

            var bossObject = new GameObject("Boss_" + id);
            bossObject.transform.SetParent(content.transform);
            bossObject.transform.position = BossHome;
            var boss = bossObject.AddComponent<BossController>();
            boss.ResetEncounter(); // chốt vị trí gốc (Awake có thể chưa chạy trong EditMode)
            bossObject.transform.position = BossAway;

            var binder = host.AddComponent<FloorSceneBinder>();
            binder.Configure(id, content, entry, null);
            content.SetActive(contentActive);

            _binders.Add(binder);
            return new FloorFixture { Binder = binder, Content = content, Entry = entry, Boss = boss };
        }

        private Transform MakePlayer(Vector3 position)
        {
            var player = Track(new GameObject("Player"));
            player.transform.position = position;
            player.AddComponent<Rigidbody2D>();
            return player.transform;
        }

        // ── Chọn binder ─────────────────────────────────────────────────────

        [Test]
        public void FindBinder_ReturnsBinderMatchingId()
        {
            var a = MakeFloor("test_a", true, Vector3.zero);
            var b = MakeFloor("test_b", false, Vector3.zero);

            Assert.AreSame(a.Binder, FloorActivation.FindBinder(_binders, "test_a"));
            Assert.AreSame(b.Binder, FloorActivation.FindBinder(_binders, "test_b"));
        }

        [Test]
        public void FindBinder_FindsBinderEvenWhenContentIsInactive()
        {
            var b = MakeFloor("test_b", false, Vector3.zero);

            Assert.IsFalse(b.Binder.IsActive);
            Assert.AreSame(b.Binder, FloorActivation.FindBinder(_binders, "test_b"));
        }

        [Test]
        public void FindBinder_UnknownOrEmptyId_ReturnsNull()
        {
            MakeFloor("test_a", true, Vector3.zero);

            Assert.IsNull(FloorActivation.FindBinder(_binders, "does_not_exist"));
            Assert.IsNull(FloorActivation.FindBinder(_binders, ""));
            Assert.IsNull(FloorActivation.FindBinder(_binders, null));
            Assert.IsNull(FloorActivation.FindBinder(null, "test_a"));
        }

        [Test]
        public void BinderRegistry_RegisterAndUnregister()
        {
            var a = MakeFloor("test_a", true, Vector3.zero);

            FloorSceneBinder.Register(a.Binder);
            Assert.Contains(a.Binder, new List<FloorSceneBinder>(FloorSceneBinder.All));

            FloorSceneBinder.Unregister(a.Binder);
            Assert.IsFalse(new List<FloorSceneBinder>(FloorSceneBinder.All).Contains(a.Binder));
        }

        // ── Kích hoạt tầng ──────────────────────────────────────────────────

        [Test]
        public void ActivateFloor_UnknownId_DoesNotThrow_AndChangesNothing()
        {
            var a = MakeFloor("test_a", true, Vector3.zero);
            var player = MakePlayer(new Vector3(5f, 5f, 0f));
            var unknown = MakeDef("test_unknown", 9);

            LogAssert.Expect(LogType.Warning, new Regex("FloorActivation"));

            bool result = true;
            Assert.DoesNotThrow(() => result = FloorActivation.ActivateFloor(unknown, _binders, true, () => player, null));

            Assert.IsFalse(result);
            Assert.IsTrue(a.Content.activeSelf, "Tầng đang bật không được bị tắt khi id không khớp.");
            Assert.IsTrue(IsAway(a), "Không được thông báo hay reset tầng đang chạy.");
            Assert.AreEqual(new Vector3(5f, 5f, 0f), player.position, "Người chơi không được bị dịch chuyển.");
        }

        [Test]
        public void ActivateFloor_ActivatingB_DeactivatesA()
        {
            var a = MakeFloor("test_a", true, Vector3.zero);
            var b = MakeFloor("test_b", false, Vector3.zero);
            var player = MakePlayer(Vector3.zero);

            bool result = FloorActivation.ActivateFloor(MakeDef("test_b", 2), _binders, true, () => player, null);

            Assert.IsTrue(result);
            Assert.IsFalse(a.Content.activeSelf);
            Assert.IsTrue(b.Content.activeSelf);
            Assert.IsTrue(IsAtHome(b), "Tầng đích phải được thông báo OnFloorActivated.");
        }

        [Test]
        public void ActivateFloor_DeactivatesAllOtherActiveBinders_RegardlessOfPreviousFloor()
        {
            var a = MakeFloor("test_a", true, Vector3.zero);
            var b = MakeFloor("test_b", true, Vector3.zero);
            var c = MakeFloor("test_c", false, Vector3.zero);
            var player = MakePlayer(Vector3.zero);

            FloorActivation.ActivateFloor(MakeDef("test_c", 3), _binders, true, () => player, null);

            Assert.IsFalse(a.Content.activeSelf);
            Assert.IsFalse(b.Content.activeSelf);
            Assert.IsTrue(c.Content.activeSelf);
        }

        [Test]
        public void BuildPlan_OnlyDeactivatesBindersThatAreActive()
        {
            var a = MakeFloor("test_a", true, Vector3.zero);
            var b = MakeFloor("test_b", false, Vector3.zero);
            var c = MakeFloor("test_c", true, Vector3.zero);

            var plan = FloorActivation.BuildPlan(_binders, "test_c", true);

            Assert.AreSame(c.Binder, plan.Target);
            CollectionAssert.AreEquivalent(new[] { a.Binder }, plan.ToDeactivate);
            Assert.IsTrue(plan.ShouldActivate);
            Assert.IsTrue(plan.ShouldPlacePlayer);
        }

        [Test]
        public void BuildPlan_UnknownId_HasNoTargetAndNoActions()
        {
            MakeFloor("test_a", true, Vector3.zero);

            var plan = FloorActivation.BuildPlan(_binders, "nope", true);

            Assert.IsNull(plan.Target);
            Assert.IsEmpty(plan.ToDeactivate);
            Assert.IsFalse(plan.ShouldActivate);
            Assert.IsFalse(plan.ShouldPlacePlayer);
        }

        [Test]
        public void BuildPlan_AlreadyStandingThere_DoesNotResetOrPlace()
        {
            var a = MakeFloor("test_a", true, Vector3.zero);

            var plan = FloorActivation.BuildPlan(_binders, "test_a", false);

            Assert.AreSame(a.Binder, plan.Target);
            Assert.IsFalse(plan.ShouldActivate, "Nội dung đang chạy không được reset khi chọn lại đúng tầng đang đứng.");
            Assert.IsFalse(plan.ShouldPlacePlayer);
        }

        [Test]
        public void BuildPlan_AlreadyStandingThere_ButContentOff_ReactivatesWithoutPlacing()
        {
            MakeFloor("test_a", false, Vector3.zero);

            var plan = FloorActivation.BuildPlan(_binders, "test_a", false);

            Assert.IsTrue(plan.ShouldActivate);
            Assert.IsFalse(plan.ShouldPlacePlayer);
        }

        // ── Binder Activate / Deactivate ────────────────────────────────────

        [Test]
        public void Activate_EnablesContentRoot_AndNotifiesListenerOnInactiveChild()
        {
            var b = MakeFloor("test_b", false, Vector3.zero);
            var def = MakeDef("test_b", 2);

            Assert.IsFalse(b.Content.activeSelf);
            b.Binder.Activate(def);

            Assert.IsTrue(b.Content.activeSelf);
            Assert.IsTrue(IsAtHome(b), "Listener nằm trong object con đang tắt vẫn phải được thông báo.");
        }

        [Test]
        public void Deactivate_NotifiesListener_ThenDisablesContentRoot()
        {
            var a = MakeFloor("test_a", true, Vector3.zero);

            a.Binder.Deactivate();

            Assert.IsFalse(a.Content.activeSelf);
            Assert.IsTrue(IsAway(a), "Tắt tầng không được reset nội dung.");
        }

        [Test]
        public void Activate_NotifiesOnlyListenersInsideOwnContent()
        {
            var a = MakeFloor("test_a", true, Vector3.zero);
            var b = MakeFloor("test_b", false, Vector3.zero);

            b.Binder.Activate(MakeDef("test_b", 2));

            Assert.IsTrue(IsAtHome(b));
            Assert.IsTrue(IsAway(a), "Listener của tầng khác không được thông báo.");
        }

        // ── Thông báo listener (listener thường, không cần scene) ───────────────────────────────

        [Test]
        public void NotifyActivated_PassesDefinitionToEveryListener()
        {
            var one = new FloorActivationTestListener();
            var two = new FloorActivationTestListener();
            var def = MakeDef("test_a", 1);

            FloorSceneBinder.NotifyActivated(new IFloorActivationListener[] { one, two }, def);

            Assert.AreEqual(1, one.ActivatedCount);
            Assert.AreEqual(1, two.ActivatedCount);
            Assert.AreSame(def, one.LastDefinition);
            Assert.AreEqual(0, one.DeactivatedCount);
        }

        [Test]
        public void NotifyDeactivated_NotifiesEveryListener()
        {
            var one = new FloorActivationTestListener();
            var two = new FloorActivationTestListener();

            FloorSceneBinder.NotifyDeactivated(new IFloorActivationListener[] { one, two });

            Assert.AreEqual(1, one.DeactivatedCount);
            Assert.AreEqual(1, two.DeactivatedCount);
            Assert.AreEqual(0, one.ActivatedCount);
        }

        [Test]
        public void Notify_ListenerThrows_DoesNotBlockOtherListeners()
        {
            var bad = new FloorActivationTestListener { ThrowOnNotify = new System.InvalidOperationException("boom") };
            var good = new FloorActivationTestListener();

            LogAssert.Expect(LogType.Exception, new Regex("boom"));
            Assert.DoesNotThrow(() => FloorSceneBinder.NotifyActivated(new IFloorActivationListener[] { bad, good }, MakeDef("test_a", 1)));

            Assert.AreEqual(1, good.ActivatedCount, "Listener sau listener lỗi vẫn phải được thông báo.");
        }

        [Test]
        public void Notify_NullListenersOrNullEntries_AreIgnored()
        {
            var good = new FloorActivationTestListener();

            Assert.DoesNotThrow(() => FloorSceneBinder.NotifyActivated(null, MakeDef("test_a", 1)));
            Assert.DoesNotThrow(() => FloorSceneBinder.NotifyDeactivated(new IFloorActivationListener[] { null, good }));
            Assert.AreEqual(1, good.DeactivatedCount);
        }

        // ── Đặt người chơi ──────────────────────────────────────────────────

        [Test]
        public void PlacePlayerAt_MovesPlayerToEntry_KeepsZ_AndZeroesVelocity()
        {
            var entry = Track(new GameObject("Entry")).transform;
            entry.position = new Vector3(12f, -7f, 0f);
            var player = MakePlayer(new Vector3(1f, 1f, -2f));
            var body = player.GetComponent<Rigidbody2D>();
            body.linearVelocity = new Vector2(3f, 4f);

            bool placed = FloorActivation.PlacePlayerAt(player, entry);

            Assert.IsTrue(placed);
            Assert.AreEqual(new Vector3(12f, -7f, -2f), player.position);
            Assert.AreEqual(Vector2.zero, body.linearVelocity);
        }

        [Test]
        public void PlacePlayerAt_MissingPlayerOrEntry_ReturnsFalse()
        {
            var entry = Track(new GameObject("Entry")).transform;
            var player = MakePlayer(Vector3.zero);

            Assert.IsFalse(FloorActivation.PlacePlayerAt(null, entry));
            Assert.IsFalse(FloorActivation.PlacePlayerAt(player, null));
        }

        [Test]
        public void ActivateFloor_PlacesPlayerAtEntryPoint_AndRunsAfterPlaceStep()
        {
            MakeFloor("test_a", true, new Vector3(0f, 0f, 0f));
            MakeFloor("test_b", false, new Vector3(20f, 8f, 0f));
            var player = MakePlayer(new Vector3(3f, 3f, 0f));
            bool afterPlaceCalled = false;

            FloorActivation.ActivateFloor(MakeDef("test_b", 2), _binders, true, () => player, () => afterPlaceCalled = true);

            Assert.AreEqual(new Vector3(20f, 8f, 0f), player.position);
            Assert.IsTrue(afterPlaceCalled);
        }

        [Test]
        public void ActivateFloor_PlacePlayerFalse_LeavesPlayerAndRunningContentUntouched()
        {
            var a = MakeFloor("test_a", true, new Vector3(100f, 100f, 0f));
            var player = MakePlayer(new Vector3(3f, 3f, 0f));
            bool afterPlaceCalled = false;

            bool result = FloorActivation.ActivateFloor(MakeDef("test_a", 1), _binders, false, () => player, () => afterPlaceCalled = true);

            Assert.IsTrue(result);
            Assert.AreEqual(new Vector3(3f, 3f, 0f), player.position);
            Assert.IsFalse(afterPlaceCalled);
            Assert.IsTrue(IsAway(a), "Chọn lại tầng đang đứng không được reset nội dung.");
        }

        // ── "Đang đứng đúng tầng" nhận ra từ trạng thái binder, không cần cờ placePlayer (P9-1b) ──

        [Test]
        public void BuildPlan_TargetActiveAndOnlyActiveBinder_IsNoOp_EvenWhenPlacePlayerTrue()
        {
            var a = MakeFloor("test_a", true, new Vector3(100f, 100f, 0f));
            MakeFloor("test_b", false, Vector3.zero); // binder tắt không tính là "binder khác đang bật"

            var plan = FloorActivation.BuildPlan(_binders, "test_a", true);

            Assert.AreSame(a.Binder, plan.Target);
            Assert.IsEmpty(plan.ToDeactivate);
            Assert.IsFalse(plan.ShouldActivate, "Đích đang bật và là duy nhất: không bật lại/reset dù placePlayer = true.");
            Assert.IsFalse(plan.ShouldPlacePlayer, "Đích đang bật và là duy nhất: không dịch chuyển người chơi dù placePlayer = true.");
        }

        [Test]
        public void BuildPlan_TargetActiveButAnotherBinderStillActive_StillActivatesAndPlaces()
        {
            var a = MakeFloor("test_a", true, Vector3.zero);
            var b = MakeFloor("test_b", true, new Vector3(20f, 8f, 0f)); // scene mới nạp: nội dung lưu mặc định bật

            var plan = FloorActivation.BuildPlan(_binders, "test_b", true);

            Assert.AreSame(b.Binder, plan.Target);
            CollectionAssert.AreEquivalent(new[] { a.Binder }, plan.ToDeactivate);
            Assert.IsTrue(plan.ShouldActivate, "Còn tầng khác đang bật = chuyển tầng thật: vẫn kích hoạt.");
            Assert.IsTrue(plan.ShouldPlacePlayer, "Còn tầng khác đang bật = chuyển tầng thật: vẫn đặt người chơi.");
        }

        [Test]
        public void BuildPlan_TargetInactive_ActivatesAndPlaces_WhetherOrNotOthersAreActive()
        {
            var a = MakeFloor("test_a", true, Vector3.zero);
            var b = MakeFloor("test_b", false, new Vector3(20f, 8f, 0f));

            var withOther = FloorActivation.BuildPlan(_binders, "test_b", true);
            Assert.AreSame(b.Binder, withOther.Target);
            CollectionAssert.AreEquivalent(new[] { a.Binder }, withOther.ToDeactivate);
            Assert.IsTrue(withOther.ShouldActivate);
            Assert.IsTrue(withOther.ShouldPlacePlayer);

            a.Content.SetActive(false); // không còn binder nào bật, đích vẫn tắt
            var alone = FloorActivation.BuildPlan(_binders, "test_b", true);
            Assert.IsEmpty(alone.ToDeactivate);
            Assert.IsTrue(alone.ShouldActivate, "Đích đang tắt phải được bật.");
            Assert.IsTrue(alone.ShouldPlacePlayer);
        }

        [Test]
        public void ActivateFloor_FirstSelectWhileStandingInTheOnlyActiveFloor_DoesNotTeleportOrReset()
        {
            // Tầng khởi đầu nằm sẵn trong scene host, loader chưa nạp tầng nào (CurrentFloorId còn null)
            // nên chọn tầng đó lần đầu bị coi là "vào tầng mới" (placePlayer = true).
            var a = MakeFloor("test_a", true, new Vector3(100f, 100f, 0f));
            var player = MakePlayer(new Vector3(3f, 3f, 0f));
            bool afterPlaceCalled = false;

            bool result = FloorActivation.ActivateFloor(MakeDef("test_a", 1), _binders, true, () => player, () => afterPlaceCalled = true);

            Assert.IsTrue(result, "Vẫn là thành công (loader trả Loaded), chỉ là không làm gì.");
            Assert.AreEqual(new Vector3(3f, 3f, 0f), player.position, "Người chơi không được bị dịch chuyển.");
            Assert.IsFalse(afterPlaceCalled, "Không snap camera khi không dịch chuyển.");
            Assert.IsTrue(a.Content.activeSelf);
            Assert.IsTrue(IsAway(a), "Boss/quái đang chạy không được reset.");
        }

        [Test]
        public void ActivateFloor_FreshlyLoadedFloorWhileAnotherIsActive_IsARealTravel()
        {
            var a = MakeFloor("test_a", true, Vector3.zero);
            var b = MakeFloor("test_b", true, new Vector3(20f, 8f, 0f)); // scene mới nạp, nội dung mặc định bật
            var player = MakePlayer(new Vector3(3f, 3f, 0f));
            bool afterPlaceCalled = false;

            bool result = FloorActivation.ActivateFloor(MakeDef("test_b", 2), _binders, true, () => player, () => afterPlaceCalled = true);

            Assert.IsTrue(result);
            Assert.IsFalse(a.Content.activeSelf, "Tầng cũ phải bị tắt.");
            Assert.IsTrue(b.Content.activeSelf);
            Assert.IsTrue(IsAtHome(b), "Tầng đích phải được thông báo OnFloorActivated.");
            Assert.AreEqual(new Vector3(20f, 8f, 0f), player.position, "Người chơi phải được đặt tại điểm vào.");
            Assert.IsTrue(afterPlaceCalled);
        }

        // ── EnemySpawner (listener) ─────────────────────────────────────────

        private EnemyController MakeEnemy(string name, bool active, Vector3 position)
        {
            var go = Track(new GameObject(name));
            go.transform.position = position;
            var enemy = go.AddComponent<EnemyController>();
            go.SetActive(active);
            return enemy;
        }

        [Test]
        public void EnemySpawner_ImplementsFloorActivationListener()
        {
            var spawner = Track(new GameObject("Spawner")).AddComponent<EnemySpawner>();

            Assert.IsInstanceOf<IFloorActivationListener>(spawner);
            Assert.DoesNotThrow(() => ((IFloorActivationListener)spawner).OnFloorActivated(MakeDef("test_a", 1)));
            Assert.DoesNotThrow(() => ((IFloorActivationListener)spawner).OnFloorDeactivated());
        }

        [Test]
        public void ReviveSlots_ReactivatesDeadEnemies_AndRepositionsAll()
        {
            var dead = MakeEnemy("Dead", false, new Vector3(50f, 50f, 0f));
            var alive = MakeEnemy("Alive", true, new Vector3(60f, 60f, 0f));
            var slots = new List<EnemyController> { dead, null, alive };

            int handled = EnemySpawner.ReviveSlots(slots, _ => new Vector2(1f, 2f));

            Assert.AreEqual(2, handled);
            Assert.IsTrue(dead.gameObject.activeSelf, "Quái đã chết (đang tắt) phải được bật lại.");
            Assert.IsTrue(alive.gameObject.activeSelf);
            Assert.AreEqual(new Vector3(1f, 2f, 0f), dead.transform.position);
            Assert.AreEqual(new Vector3(1f, 2f, 0f), alive.transform.position);
        }

        [Test]
        public void ReviveSlots_NullOrEmpty_ReturnsZero()
        {
            Assert.AreEqual(0, EnemySpawner.ReviveSlots(null, null));
            Assert.AreEqual(0, EnemySpawner.ReviveSlots(new List<EnemyController>(), _ => Vector2.zero));
        }

        // ── BossController (listener) ───────────────────────────────────────

        [Test]
        public void BossController_ImplementsFloorActivationListener()
        {
            var boss = Track(new GameObject("Boss")).AddComponent<BossController>();

            Assert.IsInstanceOf<IFloorActivationListener>(boss);
        }

        [Test]
        public void BossController_OnFloorActivated_ResetsHealthAndPosition()
        {
            // Đặt vị trí TRƯỚC khi AddComponent: dù Awake có chạy hay không, vị trí gốc vẫn là điểm này.
            var go = Track(new GameObject("Boss"));
            go.transform.position = new Vector3(4f, 6f, 0f);
            var boss = go.AddComponent<BossController>();

            boss.OnFloorActivated(MakeDef("test_a", 1));
            Assert.IsFalse(boss.IsDead, "Sau khi vào tầng, boss phải còn sống với máu đầy.");
            Assert.AreEqual(boss.MaxHealth, boss.CurrentHealth);

            go.transform.position = new Vector3(-30f, 25f, 0f);
            boss.OnFloorActivated(MakeDef("test_a", 1));

            Assert.AreEqual(new Vector3(4f, 6f, 0f), go.transform.position, "Boss phải về vị trí gốc.");
            Assert.AreEqual(boss.MaxHealth, boss.CurrentHealth);
        }

        [Test]
        public void BossController_OnFloorActivated_ReactivatesDisabledBoss()
        {
            var go = Track(new GameObject("Boss"));
            var boss = go.AddComponent<BossController>();
            go.SetActive(false);

            boss.OnFloorActivated(MakeDef("test_a", 1));

            Assert.IsTrue(go.activeSelf);
            Assert.IsFalse(boss.IsDead);
        }

        [Test]
        public void Binder_Activate_ResetsBossInsideContent()
        {
            var a = MakeFloor("test_a", false, Vector3.zero);
            var bossObject = new GameObject("Boss");
            bossObject.transform.SetParent(a.Content.transform);
            bossObject.transform.position = new Vector3(2f, 2f, 0f);
            var boss = bossObject.AddComponent<BossController>();

            a.Binder.Activate(MakeDef("test_a", 1));

            Assert.IsFalse(boss.IsDead);
            Assert.AreEqual(boss.MaxHealth, boss.CurrentHealth);
        }
    }
}
