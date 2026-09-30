using NUnit.Framework;
using SITG.Core.Minigames.HigherLower;
using SITG.Core.SideBets;
using SITG.Core.Stakes;

namespace SITG.Tests
{
    public class SideBetTests
    {
        private double _clock;

        private (SITG.Core.Run.RunController run, HigherLowerGame game) Setup(params int[] cards)
        {
            _clock = 0;
            var run = TestData.NewRun(42, TestData.Anna, TestData.Bram, TestData.Cas);
            run.BeginNight();
            var game = new HigherLowerGame("t1", new FixedDeck(cards), run.Balance);
            run.Settler.Attach(game);
            run.SideBets.Attach(game, () => _clock);
            return (run, game);
        }

        [Test]
        public void BettingAgainstAFriend_PaysOutWhenTheyLose()
        {
            var (run, game) = Setup(7, 3);
            game.TryStart(TestData.Anna, Stake.OfMoney(50), 50);
            string key = game.CurrentRoundKey;

            Assert.AreEqual(PlaceBetResult.Ok, run.SideBets.Place(key, TestData.Bram, SideBetPick.PlayerLoses, 20, _clock));
            Assert.AreEqual(30, run.State.Get(TestData.Bram).PocketMoney);

            game.MakeGuess(TestData.Anna, Guess.Higher);
            Assert.AreEqual(70, run.State.Get(TestData.Bram).PocketMoney, "50 - 20 + 40");
        }

        [Test]
        public void LosingSideBet_IsGone()
        {
            var (run, game) = Setup(7, 10);
            game.TryStart(TestData.Anna, Stake.OfMoney(50), 50);
            run.SideBets.Place(game.CurrentRoundKey, TestData.Cas, SideBetPick.PlayerLoses, 20, _clock);
            game.MakeGuess(TestData.Anna, Guess.Higher);
            game.CashOut(TestData.Anna);
            Assert.AreEqual(30, run.State.Get(TestData.Cas).PocketMoney);
        }

        [Test]
        public void CannotBetOnYourself()
        {
            var (run, game) = Setup(7, 10);
            game.TryStart(TestData.Anna, Stake.OfMoney(50), 50);
            Assert.AreEqual(PlaceBetResult.CannotBetOnYourself,
                run.SideBets.Place(game.CurrentRoundKey, TestData.Anna, SideBetPick.PlayerWins, 10, _clock));
        }

        [Test]
        public void BettingCloses_AfterTheWindow()
        {
            var (run, game) = Setup(7, 10);
            game.TryStart(TestData.Anna, Stake.OfMoney(50), 50);
            _clock = run.Balance.SideBetWindowSeconds + 0.1;
            Assert.AreEqual(PlaceBetResult.BettingClosed,
                run.SideBets.Place(game.CurrentRoundKey, TestData.Bram, SideBetPick.PlayerWins, 10, _clock));
        }

        [Test]
        public void CannotBetMoreThanYourPocket_OrTwice()
        {
            var (run, game) = Setup(7, 10);
            game.TryStart(TestData.Anna, Stake.OfMoney(50), 50);
            string key = game.CurrentRoundKey;
            Assert.AreEqual(PlaceBetResult.NotEnoughMoney, run.SideBets.Place(key, TestData.Bram, SideBetPick.PlayerWins, 999, _clock));
            Assert.AreEqual(PlaceBetResult.Ok, run.SideBets.Place(key, TestData.Bram, SideBetPick.PlayerWins, 10, _clock));
            Assert.AreEqual(PlaceBetResult.AlreadyBet, run.SideBets.Place(key, TestData.Bram, SideBetPick.PlayerWins, 10, _clock));
        }

        [Test]
        public void TracksWhoBetMostAgainstTheTeam()
        {
            var (run, game) = Setup(7, 3, 7, 3);
            game.TryStart(TestData.Anna, Stake.OfMoney(10), 10);
            run.SideBets.Place(game.CurrentRoundKey, TestData.Bram, SideBetPick.PlayerLoses, 10, _clock);
            run.SideBets.Place(game.CurrentRoundKey, TestData.Cas, SideBetPick.PlayerLoses, 30, _clock);
            game.MakeGuess(TestData.Anna, Guess.Higher);

            Assert.AreEqual(TestData.Cas, run.SideBets.TopBetterAgainstTeam());
        }
    }
}
