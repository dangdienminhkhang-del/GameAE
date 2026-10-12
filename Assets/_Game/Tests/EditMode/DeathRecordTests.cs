using System;
using System.IO;
using Game.Core.Character;
using Game.Core.World;
using Game.Services.World;
using NUnit.Framework;

namespace Game.Tests.EditMode
{
    /// <summary>docs/09 §1.3: DeathRecord → DeathInfo ánh xạ đúng; SaveAuthoritativeAsync ghi local (recovery) trước.</summary>
    public class DeathRecordTests
    {
        [Test]
        public void ToDeathInfo_MapsEveryField()
        {
            var record = new DeathRecord(DeathReason.Boss, "boss.floor_01", 12, 3, 4567, 1700000123L);

            DeathInfo info = record.ToDeathInfo();

            Assert.IsTrue(info.isDead, "DeathRecord luôn là một cái chết");
            Assert.AreEqual("Boss", info.deathCause);
            Assert.AreEqual("boss.floor_01", info.deathSourceId);
            Assert.AreEqual(12, info.finalLevel);
            Assert.AreEqual(3, info.finalHighestFloor);
            Assert.AreEqual(4567, info.totalPlayTimeSeconds);
            Assert.AreEqual(1700000123L, info.deathTimeUnix);
        }

        [Test]
        public void Properties_ExposeConstructorValues()
        {
            var record = new DeathRecord(DeathReason.Trap, "trap.spike", 7, 2, 90, 1700000000L);

            Assert.AreEqual(DeathReason.Trap, record.Reason);
            Assert.AreEqual("trap.spike", record.SourceId);
            Assert.AreEqual(7, record.FinalLevel);
            Assert.AreEqual(2, record.FinalHighestFloor);
            Assert.AreEqual(90, record.TotalPlayTimeSeconds);
            Assert.AreEqual(1700000000L, record.DeathTimeUnix);
        }

        [Test]
        public void ToDeathInfo_ReturnsNewObjectEachCall_SoRecordStaysImmutable()
        {
            var record = new DeathRecord(DeathReason.Enemy, "enemy.slime", 5, 1, 120, 1700000000L);

            DeathInfo first = record.ToDeathInfo();
            first.finalLevel = 999;
            DeathInfo second = record.ToDeathInfo();

            Assert.AreEqual(5, second.finalLevel);
            Assert.AreNotSame(first, second);
        }

        [Test]
        public void CloudPlayerSaveService_SaveAuthoritative_WritesLocalRecoveryRecord()
        {
            string dir = Path.Combine(Path.GetTempPath(), "game-death-test-" + Guid.NewGuid().ToString("N"));
            try
            {
                var service = new CloudPlayerSaveService(dir);
                var info = new DeathInfo { isDead = true, deathCause = "Enemy", deathSourceId = "enemy.slime", finalLevel = 5 };

                try
                {
                    service.SaveAuthoritativeAsync(SaveKeys.Death, info).GetAwaiter().GetResult();
                }
                catch (SaveUnavailableException)
                {
                    // Có UGS Cloud Save mà cloud chưa khởi tạo (EditMode): không xác nhận được là ĐÚNG hành vi —
                    // điều cần kiểm ở đây là bản recovery local vẫn được ghi trước.
                }

                string path = Path.Combine(dir, SaveKeys.Death + ".json");
                Assert.IsTrue(File.Exists(path), "Phải ghi local (recovery) trước khi ghi nguồn chính");
                StringAssert.Contains("\"isDead\": true", File.ReadAllText(path));
            }
            finally
            {
                if (Directory.Exists(dir)) Directory.Delete(dir, true);
            }
        }

        [Test]
        public void CloudPlayerSaveService_SaveAuthoritative_EmptyKey_Throws()
        {
            var service = new CloudPlayerSaveService(Path.Combine(Path.GetTempPath(), "game-death-test-unused"));

            Assert.Throws<ArgumentException>(() =>
                service.SaveAuthoritativeAsync(string.Empty, new DeathInfo()).GetAwaiter().GetResult());
        }
    }
}
