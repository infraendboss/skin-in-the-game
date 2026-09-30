using System;
using NUnit.Framework;
using SITG.Core.Balance;
using SITG.Core.Minigames;
using SITG.Core.Minigames.HigherLower;
using SITG.Core.Stakes;

namespace SITG.Tests
{
    public class HigherLowerTests
    {
        private static HigherLowerGame NewGame(params int[] cards) =>
            new HigherLowerGame("table_01", new FixedDeck(cards), new GameBalance());

        [Test]
        public void CorrectGuess_GrowsThePotByTheOdds()
        {
            var game = NewGame(7, 10);
            game.TryStart(TestData.Anna, Stake.OfMoney(100), 100);

            // Card 7: 6 of 12 other cards are higher, fair odds 2x, house edge 0.95 -> 1.9x
            Assert.AreEqual(1.9, game.MultiplierFor(Guess.Higher), 1e-9);
            Assert.AreEqual(GuessResult.Correct, game.MakeGuess(TestData.Anna, Guess.Higher));
            Assert.AreEqual(190, game.Pot);
            Assert.AreEqual(10, game.CurrentCard);
        }

        [Test]
        public void WrongGuess_LosesTheRound()
        {
            var game = NewGame(7, 3);
            RoundOutcome outcome = null;
            game.RoundResolved += o => outcome = o;
            game.TryStart(TestData.Anna, Stake.OfMoney(100), 100);

            Assert.AreEqual(GuessResult.Wrong, game.MakeGuess(TestData.Anna, Guess.Higher));
            Assert.IsNotNull(outcome);
            Assert.IsFalse(outcome.Won);
            Assert.AreEqual(0, outcome.Payout);
            Assert.AreEqual(MinigamePhase.Waiting, game.Phase);
        }

        [Test]
        public void EqualCard_IsRedrawn()
        {
            var game = NewGame(7, 7, 7, 12);
            game.TryStart(TestData.Anna, Stake.OfMoney(100), 100);
            Assert.AreEqual(GuessResult.Correct, game.MakeGuess(TestData.Anna, Guess.Higher));
            Assert.AreEqual(12, game.CurrentCard);
        }

        [Test]
        public void ImpossibleGuess_IsNotAllowed()
        {
            var game = NewGame(13);
            game.TryStart(TestData.Anna, Stake.OfMoney(100), 100);
            Assert.IsFalse(game.CanGuess(Guess.Higher), "Nothing is higher than a king");
            Assert.IsTrue(game.CanGuess(Guess.Lower));
            Assert.Throws<InvalidOperationException>(() => game.MakeGuess(TestData.Anna, Guess.Higher));
        }

        [Test]
        public void CashOut_NeedsOneCorrectGuess_ThenPaysThePot()
        {
            var game = NewGame(2, 9);
            RoundOutcome outcome = null;
            game.RoundResolved += o => outcome = o;
            game.TryStart(TestData.Anna, Stake.OfMoney(100), 100);

            Assert.IsFalse(game.CanCashOut);
            game.MakeGuess(TestData.Anna, Guess.Higher);
            Assert.IsTrue(game.CanCashOut);
            long pot = game.Pot;
            game.CashOut(TestData.Anna);

            Assert.IsTrue(outcome.Won);
            Assert.AreEqual(pot, outcome.Payout);
        }

        [Test]
        public void OnlyTheCurrentPlayer_CanGuess()
        {
            var game = NewGame(7, 10);
            game.TryStart(TestData.Anna, Stake.OfMoney(100), 100);
            Assert.Throws<InvalidOperationException>(() => game.MakeGuess(TestData.Bram, Guess.Higher));
        }

        [Test]
        public void TableIsBusy_WhileARoundIsGoing()
        {
            var game = NewGame(7, 10);
            Assert.IsTrue(game.TryStart(TestData.Anna, Stake.OfMoney(100), 100));
            Assert.IsFalse(game.TryStart(TestData.Bram, Stake.OfMoney(100), 100));
        }

        [Test]
        public void RoundKeys_AreUniquePerRound()
        {
            var game = NewGame(7, 3, 7, 3);
            game.TryStart(TestData.Anna, Stake.OfMoney(10), 10);
            string first = game.CurrentRoundKey;
            game.MakeGuess(TestData.Anna, Guess.Higher);
            game.TryStart(TestData.Anna, Stake.OfMoney(10), 10);
            Assert.AreNotEqual(first, game.CurrentRoundKey);
        }
    }
}
