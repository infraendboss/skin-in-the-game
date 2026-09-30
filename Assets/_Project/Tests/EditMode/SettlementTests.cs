using NUnit.Framework;
using SITG.Core.HouseRules;
using SITG.Core.Minigames;
using SITG.Core.Minigames.HigherLower;
using SITG.Core.Stakes;

namespace SITG.Tests
{
    public class SettlementTests
    {
        [Test]
        public void MoneyStake_IsTakenFromTheTeamBank_AndWinIsPaidBack()
        {
            var run = TestData.NewRun();
            run.BeginNight();
            var game = new HigherLowerGame("t1", new FixedDeck(7, 10), run.Balance);
            run.Settler.Attach(game);
            long bank = run.Economy.TeamBank;

            Assert.IsTrue(run.Settler.TryTakeStake(TestData.Anna, Stake.OfMoney(100), out long value));
            Assert.AreEqual(bank - 100, run.Economy.TeamBank);

            game.TryStart(TestData.Anna, Stake.OfMoney(100), value);
            game.MakeGuess(TestData.Anna, Guess.Higher);
            game.CashOut(TestData.Anna);

            long expected = bank - 100 + run.Rules.ApplyPayout(190);
            Assert.AreEqual(expected, run.Economy.TeamBank);
        }

        [Test]
        public void HouseRule_DoublesThePayout()
        {
            var rules = new HouseRuleSet(new HouseRuleDefinition { Id = "x", PayoutMultiplier = 2.0 });
            Assert.AreEqual(380, rules.ApplyPayout(190));
        }

        [Test]
        public void CannotStakeMoreThanTheTeamHas()
        {
            var run = TestData.NewRun();
            run.BeginNight();
            Assert.IsFalse(run.Settler.TryTakeStake(TestData.Anna, Stake.OfMoney(run.Economy.TeamBank + 1), out _));
        }

        [Test]
        public void CannotStakeOutsideANight()
        {
            var run = TestData.NewRun();
            Assert.IsFalse(run.Settler.TryTakeStake(TestData.Anna, Stake.OfMoney(10), out _));
        }

        [Test]
        public void BodyStake_LostRound_TakesThePart()
        {
            var run = TestData.NewRun();
            run.BeginNight();
            var game = new HigherLowerGame("t1", new FixedDeck(7, 3), run.Balance);
            run.Settler.Attach(game);

            var stake = Stake.OfBodyPart(BodyParts.Eyes);
            Assert.IsTrue(run.Settler.TryTakeStake(TestData.Anna, stake, out long value));
            Assert.AreEqual(200, value);
            game.TryStart(TestData.Anna, stake, value);
            game.MakeGuess(TestData.Anna, Guess.Higher);

            Assert.IsFalse(run.State.Get(TestData.Anna).Parts.Has(BodyParts.Eyes));
        }

        [Test]
        public void BodyStake_WonRound_KeepsThePart_AndRestoresALostOne()
        {
            var run = TestData.NewRun();
            run.BeginNight();
            run.Stakes.Pledge(TestData.Anna, BodyParts.Legs);
            run.Stakes.ForfeitPledge(TestData.Anna, BodyParts.Legs);

            var game = new HigherLowerGame("t1", new FixedDeck(7, 10), run.Balance);
            Settlement settlement = null;
            run.Settler.Settled += s => settlement = s;
            run.Settler.Attach(game);

            var stake = Stake.OfBodyPart(BodyParts.Voice);
            run.Settler.TryTakeStake(TestData.Anna, stake, out long value);
            game.TryStart(TestData.Anna, stake, value);
            game.MakeGuess(TestData.Anna, Guess.Higher);
            game.CashOut(TestData.Anna);

            var anna = run.State.Get(TestData.Anna);
            Assert.IsTrue(anna.Parts.Has(BodyParts.Voice));
            Assert.IsTrue(anna.Parts.Has(BodyParts.Legs), "Legs came back as a bonus");
            Assert.AreEqual(BodyParts.Legs, settlement.PartRestored);
            Assert.Greater(settlement.PaidToTeam, 0);
        }

        [Test]
        public void GhostsCannotPlayAtTables()
        {
            var run = TestData.NewRun();
            run.BeginNight();
            run.State.Get(TestData.Bram).Parts = BodyParts.None;
            Assert.IsFalse(run.Settler.TryTakeStake(TestData.Bram, Stake.OfMoney(10), out _));
        }
    }
}
