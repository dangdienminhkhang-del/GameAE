using Game.Core.Combat;
using Game.Core.PvP;
using NUnit.Framework;

namespace Game.Tests.EditMode
{
    [TestFixture]
    public class DuelRulesTests
    {
        [Test]
        public void FirstStrike_WhenP1HitsFirst_P1Wins()
        {
            bool ended = DuelRules.CheckDuelEnd(
                DuelMode.FirstStrike,
                p1Hp: 100, p1MaxHp: 100,
                p2Hp: 90, p2MaxHp: 100,
                p1HitsDealt: 1, p2HitsDealt: 0,
                out int winner);

            Assert.IsTrue(ended);
            Assert.AreEqual(1, winner);
        }

        [Test]
        public void FirstStrike_WhenNoHits_DuelContinues()
        {
            bool ended = DuelRules.CheckDuelEnd(
                DuelMode.FirstStrike,
                p1Hp: 100, p1MaxHp: 100,
                p2Hp: 100, p2MaxHp: 100,
                p1HitsDealt: 0, p2HitsDealt: 0,
                out int winner);

            Assert.IsFalse(ended);
            Assert.AreEqual(-1, winner);
        }

        [Test]
        public void HalfLoss_WhenP2Below50Percent_P1Wins()
        {
            bool ended = DuelRules.CheckDuelEnd(
                DuelMode.HalfLoss,
                p1Hp: 80, p1MaxHp: 100,
                p2Hp: 49, p2MaxHp: 100,
                p1HitsDealt: 3, p2HitsDealt: 1,
                out int winner);

            Assert.IsTrue(ended);
            Assert.AreEqual(1, winner);
        }

        [Test]
        public void HalfLoss_WhenBothAbove50Percent_DuelContinues()
        {
            bool ended = DuelRules.CheckDuelEnd(
                DuelMode.HalfLoss,
                p1Hp: 60, p1MaxHp: 100,
                p2Hp: 55, p2MaxHp: 100,
                p1HitsDealt: 2, p2HitsDealt: 2,
                out int winner);

            Assert.IsFalse(ended);
            Assert.AreEqual(-1, winner);
        }

        [Test]
        public void TotalLoss_WhenP1ZeroHp_P2Wins()
        {
            bool ended = DuelRules.CheckDuelEnd(
                DuelMode.TotalLoss,
                p1Hp: 0, p1MaxHp: 100,
                p2Hp: 30, p2MaxHp: 100,
                p1HitsDealt: 2, p2HitsDealt: 5,
                out int winner);

            Assert.IsTrue(ended);
            Assert.AreEqual(2, winner);
        }

        [Test]
        public void PlayerCursorRules_EvaluateCorrectColorAndThreatLevel()
        {
            Assert.AreEqual(CursorThreatLevel.PlayerGreen, PlayerCursorRules.ToThreatLevel(PlayerCrimeStatus.Innocent));
            Assert.AreEqual(CursorThreatLevel.PlayerOrange, PlayerCursorRules.ToThreatLevel(PlayerCrimeStatus.Criminal));
            Assert.AreEqual(CursorThreatLevel.PlayerRed, PlayerCursorRules.ToThreatLevel(PlayerCrimeStatus.Murderer));

            var greenColor = PlayerCursorRules.GetCursorColor(PlayerCrimeStatus.Innocent);
            Assert.AreEqual(EnemyCursorRules.GetCursorColor(CursorThreatLevel.PlayerGreen), greenColor);
        }

        [Test]
        public void PlayerCursorRules_AttackInnocent_BecomesCriminal()
        {
            var status = PlayerCursorRules.OnAttackPlayer(PlayerCrimeStatus.Innocent, PlayerCrimeStatus.Innocent);
            Assert.AreEqual(PlayerCrimeStatus.Criminal, status);
        }

        [Test]
        public void PlayerCursorRules_AttackCriminal_StaysInnocent()
        {
            var status = PlayerCursorRules.OnAttackPlayer(PlayerCrimeStatus.Innocent, PlayerCrimeStatus.Criminal);
            Assert.AreEqual(PlayerCrimeStatus.Innocent, status);
        }

        [Test]
        public void PlayerCursorRules_KillInnocent_BecomesMurderer()
        {
            var status = PlayerCursorRules.OnKillPlayer(PlayerCrimeStatus.Innocent, PlayerCrimeStatus.Innocent);
            Assert.AreEqual(PlayerCrimeStatus.Murderer, status);
        }
    }
}
