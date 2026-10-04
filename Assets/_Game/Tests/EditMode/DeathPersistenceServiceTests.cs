using System.Threading.Tasks;
using Game.Core.Character;
using Game.Core.World;
using Game.Services.Character;
using Game.Services.World;
using NUnit.Framework;

namespace Game.Tests.EditMode
{
    /// <summary>
    /// Permadeath (docs/09 §1, docs/03 §12): chết chỉ ghi nhận một lần, và không xác minh được thì FAIL-CLOSED.
    /// </summary>
    public class DeathPersistenceServiceTests
    {
        /// <summary>Giả lập nguồn chính (cloud) bị lỗi mạng.</summary>
        private class UnavailableSaveService : PlayerSaveService
        {
            public override Task<T> LoadAuthoritativeAsync<T>(string key)
            {
                return Task.FromException<T>(new SaveUnavailableException(key, null));
            }
        }

        [Test]
        public void LoadDeathInfo_WhenSourceUnavailable_Throws_NotFallbackToAlive()
        {
            var service = new DeathPersistenceService(new UnavailableSaveService());

            Assert.Throws<SaveUnavailableException>(() => service.LoadDeathInfoAsync().GetAwaiter().GetResult());
            Assert.IsFalse(service.IsPlayerDead, "Chưa biết ≠ còn sống: caller phải tự chặn, service không được tự khẳng định");
        }

        [Test]
        public void LoadDeathInfo_ReturnsSavedDeadState()
        {
            var save = new PlayerSaveService();
            save.SaveAsync(SaveKeys.Death, new DeathInfo { isDead = true, deathCause = "Enemy" }).GetAwaiter().GetResult();
            var service = new DeathPersistenceService(save);

            DeathInfo info = service.LoadDeathInfoAsync().GetAwaiter().GetResult();

            Assert.IsTrue(info.isDead);
            Assert.IsTrue(service.IsPlayerDead);
        }

        [Test]
        public void OnPlayerDied_PersistsDeath()
        {
            var save = new PlayerSaveService();
            var service = new DeathPersistenceService(save);

            service.OnPlayerDiedAsync(DeathReason.Enemy, "enemy.slime", 5, 1, 120).GetAwaiter().GetResult();

            DeathInfo stored = save.LoadAuthoritativeAsync<DeathInfo>(SaveKeys.Death).GetAwaiter().GetResult();
            Assert.IsTrue(stored.isDead);
            Assert.AreEqual("Enemy", stored.deathCause);
            Assert.AreEqual("enemy.slime", stored.deathSourceId);
            Assert.AreEqual(5, stored.finalLevel);
        }

        [Test]
        public void OnPlayerDied_CalledTwice_KeepsFirstDeath()
        {
            var save = new PlayerSaveService();
            var service = new DeathPersistenceService(save);

            service.OnPlayerDiedAsync(DeathReason.Enemy, "enemy.slime", 5, 1, 120).GetAwaiter().GetResult();
            service.OnPlayerDiedAsync(DeathReason.Trap, "trap.spike", 9, 2, 999).GetAwaiter().GetResult();

            Assert.AreEqual("Enemy", service.CurrentDeathInfo.deathCause);
            Assert.AreEqual("enemy.slime", service.CurrentDeathInfo.deathSourceId);
            Assert.AreEqual(5, service.CurrentDeathInfo.finalLevel);

            DeathInfo stored = save.LoadAuthoritativeAsync<DeathInfo>(SaveKeys.Death).GetAwaiter().GetResult();
            Assert.AreEqual("enemy.slime", stored.deathSourceId, "Lần gọi thứ hai không được ghi đè");
        }
    }
}
