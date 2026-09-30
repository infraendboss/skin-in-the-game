using NUnit.Framework;
using SITG.Core.Random;
using SITG.Core.Stakes;

namespace SITG.Tests
{
    public class EconomyAndStakeTests
    {
        [Test]
        public void CannotWithdrawMoreThanTheTeamBank()
        {
            var run = TestData.NewRun();
            long bank = run.Economy.TeamBank;
            Assert.IsFalse(run.Economy.TryWithdrawTeam(bank + 1));
            Assert.AreEqual(bank, run.Economy.TeamBank);
            Assert.IsTrue(run.Economy.TryWithdrawTeam(bank));
            Assert.AreEqual(0, run.Economy.TeamBank);
        }

        [Test]
        public void PledgedPart_StaysUntilLost()
        {
            var run = TestData.NewRun();
            run.BeginNight();
            run.Stakes.Pledge(TestData.Anna, BodyParts.Voice);

            var anna = run.State.Get(TestData.Anna);
            Assert.IsTrue(anna.Parts.Has(BodyParts.Voice), "Still has voice while the round is going");
            Assert.IsFalse(run.Stakes.CanPledge(TestData.Anna, BodyParts.Voice), "Cannot pledge the same part twice");

            run.Stakes.ForfeitPledge(TestData.Anna, BodyParts.Voice);
            Assert.IsFalse(anna.Parts.Has(BodyParts.Voice));
        }

        [Test]
        public void LosingEveryPart_MakesYouAGhost()
        {
            var run = TestData.NewRun();
            run.BeginNight();
            bool ghostEvent = false;
            run.Stakes.BecameGhost += k => ghostEvent = k == TestData.Bram;

            foreach (var part in BodyPartsExtensions.Individual)
            {
                run.Stakes.Pledge(TestData.Bram, part);
                run.Stakes.ForfeitPledge(TestData.Bram, part);
            }

            Assert.IsTrue(run.State.Get(TestData.Bram).IsGhost);
            Assert.IsTrue(ghostEvent);
        }

        [Test]
        public void BodyPartValue_GrowsEachNight()
        {
            var balance = new SITG.Core.Balance.GameBalance();
            Assert.AreEqual(200, balance.BodyPartValue(BodyParts.Eyes, 0));
            Assert.AreEqual(300, balance.BodyPartValue(BodyParts.Eyes, 1));
            Assert.AreEqual(600, balance.BodyPartValue(BodyParts.Eyes, 4));
        }

        [Test]
        public void RestoreRandomLostPart_GivesBackAPartYouLost()
        {
            var run = TestData.NewRun();
            run.BeginNight();
            run.Stakes.Pledge(TestData.Anna, BodyParts.Legs);
            run.Stakes.ForfeitPledge(TestData.Anna, BodyParts.Legs);

            var restored = run.Stakes.RestoreRandomLostPart(TestData.Anna, new SeededRng(1));
            Assert.AreEqual(BodyParts.Legs, restored);
            Assert.AreEqual(BodyParts.None, run.Stakes.RestoreRandomLostPart(TestData.Anna, new SeededRng(1)));
        }
    }
}
