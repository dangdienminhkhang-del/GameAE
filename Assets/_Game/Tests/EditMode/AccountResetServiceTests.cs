using System;
using System.Reflection;
using System.Threading.Tasks;
using Game.Core.Account;
using Game.Core.World;
using Game.Services.Account;
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

            public bool IsSignedIn => true;
            public string PlayerId => "fake_player";
            public AccountType AccountType { get; set; } = AccountType.Guest;
            public LinkProvider LinkedProvider => default;
            public bool IsLinked { get; set; }

            public Task InitializeAsync() => Task.CompletedTask;
            public Task<bool> SignInAnonymouslyAsync() => Task.FromResult(SignInResult);

            public Task SignOutAsync()
            {
                SignOutCalls++;
                return Task.CompletedTask;
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
