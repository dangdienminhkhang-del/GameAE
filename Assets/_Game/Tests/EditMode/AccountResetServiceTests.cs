using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Game.Core.Account;
using Game.Core.Character;
using Game.Core.World;
using Game.Services.Account;
using Game.Services.Character;
using Game.Services.World;
using NUnit.Framework;

namespace Game.Tests.EditMode
{
    /// <summary>
    /// Reset tài khoản Guest (docs/09 §2.4): nhân vật mới KHÔNG được thừa hưởng dữ liệu của nhân vật đã chết.
    /// </summary>
    public class AccountResetServiceTests
    {
        private class FakeAccount : IAccountService
        {
            public bool SignInResult = true;
            public int SignOutCalls;

            /// <summary>PlayerId hiện tại (null = chưa biết). Mặc định giữ "fake_player" như trước D3.</summary>
            public string CurrentPlayerId = "fake_player";

            /// <summary>Nếu khác null: đăng nhập ẩn danh THÀNH CÔNG sẽ chuyển sang PlayerId mới này (như UGS cấp id mới).</summary>
            public string NextPlayerId;

            public bool IsSignedIn => true;
            public string PlayerId => CurrentPlayerId;
            public AccountType AccountType { get; set; } = AccountType.Guest;
            public LinkProvider LinkedProvider => default;
            public bool IsLinked { get; set; }

            public Task InitializeAsync() => Task.CompletedTask;
            public Task<bool> SignInAnonymouslyAsync()
            {
                if (SignInResult && NextPlayerId != null) CurrentPlayerId = NextPlayerId;
                return Task.FromResult(SignInResult);
            }

            public Task SignOutAsync()
            {
                SignOutCalls++;
                return Task.CompletedTask;
            }
        }

        private string _tempDir;

        [SetUp]
        public void SetUp()
        {
            _tempDir = Path.Combine(Path.GetTempPath(), "GameAE_AccountReset_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_tempDir);
        }

        [TearDown]
        public void TearDown()
        {
            try
            {
                if (Directory.Exists(_tempDir)) Directory.Delete(_tempDir, true);
            }
            catch (IOException)
            {
                // Thư mục tạm bị khóa tạm thời — không làm hỏng test.
            }
        }

        private static PlayerSaveService SeedAllKeys()
        {
            var save = new PlayerSaveService();
            foreach (string key in SaveKeys.All)
            {
                save.SaveAsync(key, new ProfileData { level = 9 }).GetAwaiter().GetResult();
            }
            return save;
        }

        private static bool HasData(PlayerSaveService save, string key)
        {
            return save.LoadAsync<ProfileData>(key).GetAwaiter().GetResult() != null;
        }

        [Test]
        public void Reset_Guest_WipesEveryPlayerDataKey()
        {
            PlayerSaveService save = SeedAllKeys();
            var account = new FakeAccount();
            var service = new AccountResetService(account, save);

            bool ok = service.ResetGuestAccountAsync().GetAwaiter().GetResult();

            Assert.IsTrue(ok);
            Assert.AreEqual(1, account.SignOutCalls);
            foreach (string key in SaveKeys.All)
            {
                Assert.IsFalse(HasData(save, key), $"Key '{key}' phải bị xóa khi reset");
            }
        }

        [Test]
        public void Reset_LinkedAccount_IsRefused_AndKeepsData()
        {
            PlayerSaveService save = SeedAllKeys();
            var account = new FakeAccount { IsLinked = true };
            var service = new AccountResetService(account, save);

            bool ok = service.ResetGuestAccountAsync().GetAwaiter().GetResult();

            Assert.IsFalse(ok);
            Assert.AreEqual(0, account.SignOutCalls);
            foreach (string key in SaveKeys.All) Assert.IsTrue(HasData(save, key));
        }

        [Test]
        public void Reset_OfficialAccount_IsRefused_AndKeepsData()
        {
            PlayerSaveService save = SeedAllKeys();
            var account = new FakeAccount { AccountType = AccountType.Official };
            var service = new AccountResetService(account, save);

            Assert.IsFalse(service.CanResetCurrentAccount());
            Assert.IsFalse(service.ResetGuestAccountAsync().GetAwaiter().GetResult());
            foreach (string key in SaveKeys.All) Assert.IsTrue(HasData(save, key));
        }

        [Test]
        public void Reset_WhenNewSignInFails_DoesNotWipe()
        {
            PlayerSaveService save = SeedAllKeys();
            var account = new FakeAccount { SignInResult = false };
            var service = new AccountResetService(account, save);

            bool ok = service.ResetGuestAccountAsync().GetAwaiter().GetResult();

            Assert.IsFalse(ok);
            foreach (string key in SaveKeys.All) Assert.IsTrue(HasData(save, key), "Đăng nhập mới thất bại thì chưa được xóa gì");
        }

        // ── D2 + D3: recovery record death.pending.<playerId>.json ───────────

        private string PendingPathFor(string playerId) =>
            Path.Combine(_tempDir, DeathPersistenceService.PendingRecoveryNameFor(playerId) + ".json");

        private string LegacyPendingPath => Path.Combine(_tempDir, "death.pending.json");

        /// <summary>Ghi file pending RIÊNG của một tài khoản (đúng tên mà luồng D3 dùng).</summary>
        private string WritePendingFileFor(CloudPlayerSaveService files, string playerId)
        {
            var pending = new PendingDeathRecord
            {
                playerId = playerId,
                deathInfo = new DeathInfo { isDead = true, deathSourceId = "enemy.slime" }
            };
            Assert.IsTrue(files.TryWriteRecovery(DeathPersistenceService.PendingRecoveryNameFor(playerId), pending));

            string path = PendingPathFor(playerId);
            Assert.IsTrue(File.Exists(path), $"Tiền đề: file pending của '{playerId}' phải tồn tại trước khi reset");
            return path;
        }

        /// <summary>Ghi file chung CŨ (trước D3): death.pending.json = {playerId, DeathInfo}.</summary>
        private void WriteLegacyPendingFile(CloudPlayerSaveService files, string playerId)
        {
            var pending = new PendingDeathRecord
            {
                playerId = playerId,
                deathInfo = new DeathInfo { isDead = true, deathSourceId = "legacy.slime" }
            };
            Assert.IsTrue(files.TryWriteRecovery(DeathPersistenceService.PendingRecoveryName, pending));
            Assert.IsTrue(File.Exists(LegacyPendingPath), "Tiền đề: file chung cũ phải tồn tại");
        }

        [Test]
        public void Reset_Guest_DeletesPendingDeathFile_OfTheOldAccount()
        {
            var files = new CloudPlayerSaveService(_tempDir);
            string oldPath = WritePendingFileFor(files, "fake_player");
            var service = new AccountResetService(new FakeAccount { NextPlayerId = "new_player" }, files);

            bool ok = service.ResetGuestAccountAsync().GetAwaiter().GetResult();

            Assert.IsTrue(ok);
            Assert.IsFalse(File.Exists(oldPath), "Reset phải xóa death.pending.<playerId cũ>.json");
        }

        [Test]
        public void Reset_Guest_UsesTheOldPlayerId_NotTheNewOneAfterSignIn()
        {
            // Sau đăng nhập mới PlayerId đổi → nếu xóa theo id mới sẽ xóa nhầm file của tài khoản mới và để sót file cũ.
            var files = new CloudPlayerSaveService(_tempDir);
            string oldPath = WritePendingFileFor(files, "old_player");
            string newPath = WritePendingFileFor(files, "new_player");
            var account = new FakeAccount { CurrentPlayerId = "old_player", NextPlayerId = "new_player" };
            var service = new AccountResetService(account, files);

            bool ok = service.ResetGuestAccountAsync().GetAwaiter().GetResult();

            Assert.IsTrue(ok);
            Assert.AreEqual("new_player", account.PlayerId, "Tiền đề: đã chuyển sang tài khoản mới");
            Assert.IsFalse(File.Exists(oldPath), "File của tài khoản CŨ phải bị xóa");
            Assert.IsTrue(File.Exists(newPath), "File của tài khoản mới (id vừa cấp) không được đụng tới");
        }

        [Test]
        public void Reset_Guest_KeepsPendingFilesOfOtherAccounts()
        {
            var files = new CloudPlayerSaveService(_tempDir);
            string oldPath = WritePendingFileFor(files, "account_A");
            string otherPath = WritePendingFileFor(files, "account_B");
            string weirdPath = WritePendingFileFor(files, "weird/id:C");
            var service = new AccountResetService(new FakeAccount { CurrentPlayerId = "account_A", NextPlayerId = "account_Z" }, files);

            bool ok = service.ResetGuestAccountAsync().GetAwaiter().GetResult();

            Assert.IsTrue(ok);
            Assert.IsFalse(File.Exists(oldPath), "Reset A xóa file của A");
            Assert.IsTrue(File.Exists(otherPath), "Reset A không được xóa file của B");
            Assert.IsTrue(File.Exists(weirdPath), "Reset A không được xóa file của tài khoản khác (kể cả id có ký tự lạ)");
        }

        [Test]
        public void Reset_Guest_DeletesLegacyFile_WhenItBelongsToTheOldAccount()
        {
            var files = new CloudPlayerSaveService(_tempDir);
            WriteLegacyPendingFile(files, "account_A");
            var service = new AccountResetService(new FakeAccount { CurrentPlayerId = "account_A", NextPlayerId = "account_Z" }, files);

            bool ok = service.ResetGuestAccountAsync().GetAwaiter().GetResult();

            Assert.IsTrue(ok);
            Assert.IsFalse(File.Exists(LegacyPendingPath), "File chung cũ của đúng tài khoản cũ cũng phải bị dọn");
        }

        [Test]
        public void Reset_Guest_KeepsLegacyFile_WhenItBelongsToAnotherAccount()
        {
            var files = new CloudPlayerSaveService(_tempDir);
            WriteLegacyPendingFile(files, "account_B");
            var service = new AccountResetService(new FakeAccount { CurrentPlayerId = "account_A", NextPlayerId = "account_Z" }, files);

            bool ok = service.ResetGuestAccountAsync().GetAwaiter().GetResult();

            Assert.IsTrue(ok);
            Assert.IsTrue(File.Exists(LegacyPendingPath), "File chung cũ của tài khoản khác phải giữ nguyên");
        }

        [Test]
        public void Reset_Guest_WhenOldPlayerIdUnknown_DeletesNoPendingFile()
        {
            var files = new CloudPlayerSaveService(_tempDir);
            string otherPath = WritePendingFileFor(files, "account_B");
            WriteLegacyPendingFile(files, "account_B");
            var service = new AccountResetService(new FakeAccount { CurrentPlayerId = null, NextPlayerId = "account_Z" }, files);

            bool ok = service.ResetGuestAccountAsync().GetAwaiter().GetResult();

            Assert.IsTrue(ok, "Vẫn reset được dù không biết id cũ");
            Assert.IsTrue(File.Exists(otherPath), "Không biết id cũ thì không được xóa file của ai");
            Assert.IsTrue(File.Exists(LegacyPendingPath));
        }

        [Test]
        public void Reset_WhenNewSignInFails_KeepsPendingDeathFile()
        {
            var files = new CloudPlayerSaveService(_tempDir);
            string pendingPath = WritePendingFileFor(files, "fake_player");
            var service = new AccountResetService(new FakeAccount { SignInResult = false, NextPlayerId = "new_player" }, files);

            bool ok = service.ResetGuestAccountAsync().GetAwaiter().GetResult();

            Assert.IsFalse(ok);
            Assert.IsTrue(File.Exists(pendingPath), "Đăng nhập mới thất bại thì tài khoản cũ vẫn bị khóa → giữ file");
        }

        [Test]
        public void SaveKeys_All_ContainsEveryDeclaredKey()
        {
            // Chốt chặn: thêm const key mới vào SaveKeys mà quên thêm vào All → reset sẽ để sót dữ liệu cũ.
            foreach (FieldInfo field in typeof(SaveKeys).GetFields(BindingFlags.Public | BindingFlags.Static))
            {
                if (field.IsLiteral && field.FieldType == typeof(string))
                {
                    string key = (string)field.GetRawConstantValue();
                    CollectionAssert.Contains(SaveKeys.All, key, $"SaveKeys.All thiếu '{field.Name}'");
                }
            }
        }
    }
}
