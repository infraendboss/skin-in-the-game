using System;
using NUnit.Framework;
using SITG.Core.HouseRules;
using SITG.Core.Run;
using SITG.Core.Stakes;

namespace SITG.Tests
{
    public class RunControllerTests
    {
        [Test]
        public void NewRun_HasFiveNights_StartingWithAnOpener()
        {
            var run = TestData.NewRun();
            Assert.AreEqual(5, run.State.NightCount);
            Assert.AreEqual(DefaultHouseRules.CalmNightId, run.State.HouseRuleIdPerNight[0]);
            for (int n = 1; n < 5; n++)
                Assert.AreNotEqual(run.State.HouseRuleIdPerNight[n - 1], run.State.HouseRuleIdPerNight[n]);
        }

        [Test]
        public void SameSeed_GivesTheSameRun()
        {
            var a = TestData.NewRun(777);
            var b = TestData.NewRun(777);
            CollectionAssert.AreEqual(a.State.HouseRuleIdPerNight, b.State.HouseRuleIdPerNight);
            CollectionAssert.AreEqual(a.State.QuotaPerNight, b.State.QuotaPerNight);
        }

        [Test]
        public void MeetingEveryQuota_WinsTheRun()
        {
            var run = TestData.NewRun();
            for (int n = 0; n < run.State.NightCount; n++)
            {
                run.BeginNight();
                run.Economy.DepositTeam(run.State.CurrentQuota);
                var result = run.EndNight();
                Assert.IsTrue(result.QuotaMet);
            }
            Assert.AreEqual(RunResult.Won, run.State.Result);
            Assert.Throws<InvalidOperationException>(() => run.BeginNight());
        }

        [Test]
        public void MissingQuota_TakesMoneyAndOnePartFromEveryone()
        {
            var run = TestData.NewRun();
            run.BeginNight();
            run.Economy.EmptyTeamBank();
            run.Economy.DepositTeam(10);

            var result = run.EndNight();

            Assert.IsFalse(result.QuotaMet);
            Assert.AreEqual(10, result.MoneyCollected);
            Assert.AreEqual(0, run.Economy.TeamBank);
            Assert.AreEqual(2, result.PartsTaken.Count);
            foreach (var p in run.State.Players) Assert.AreEqual(3, p.Parts.Count());
            Assert.AreEqual(RunResult.InProgress, result.Result);
            Assert.AreEqual(1, run.State.NightIndex);
        }

        [Test]
        public void EveryoneAGhost_LosesTheRun()
        {
            var run = TestData.NewRun();
            run.BeginNight();
            foreach (var p in run.State.Players) p.Parts = BodyParts.Legs;
            run.Economy.EmptyTeamBank();
            var result = run.EndNight();
            Assert.AreEqual(RunResult.Lost, result.Result);
        }

        [Test]
        public void FailingTheLastNight_LosesTheRun()
        {
            var run = TestData.NewRun();
            for (int n = 0; n < run.State.NightCount - 1; n++)
            {
                run.BeginNight();
                run.Economy.DepositTeam(run.State.CurrentQuota);
                run.EndNight();
            }
            run.BeginNight();
            run.Economy.EmptyTeamBank();
            Assert.AreEqual(RunResult.Lost, run.EndNight().Result);
        }

        [Test]
        public void Ghosts_GetPocketMoneyEachNight()
        {
            var run = TestData.NewRun();
            var bram = run.State.Get(TestData.Bram);
            bram.Parts = BodyParts.None;
            long before = bram.PocketMoney;
            run.BeginNight();
            Assert.AreEqual(before + run.Balance.GhostNightlyAllowance, bram.PocketMoney);
        }

        [Test]
        public void RejoiningPlayer_GetsTheirStateBack()
        {
            var run = TestData.NewRun();
            run.BeginNight();
            var anna = run.State.Get(TestData.Anna);
            anna.Parts = BodyParts.Hands;
            anna.PocketMoney = 123;
            run.MarkDisconnected(TestData.Anna);
            Assert.IsFalse(anna.IsConnected);

            var back = run.AddPlayer(TestData.Anna, "Anna");
            Assert.AreSame(anna, back);
            Assert.IsTrue(back.IsConnected);
            Assert.AreEqual(BodyParts.Hands, back.Parts);
            Assert.AreEqual(123, back.PocketMoney);
            Assert.AreEqual(2, run.State.Players.Count);
        }

        [Test]
        public void NewPlayer_CanJoinMidRun()
        {
            var run = TestData.NewRun();
            run.BeginNight();
            run.AddPlayer(TestData.Cas, "Cas");
            Assert.AreEqual(3, run.State.Players.Count);
            Assert.AreEqual(run.Balance.StartingPocketMoney, run.State.Get(TestData.Cas).PocketMoney);
        }

        [Test]
        public void ResumedRun_PlaysOutExactlyTheSame()
        {
            var original = TestData.NewRun(5);
            original.BeginNight();
            original.Economy.EmptyTeamBank();
            original.EndNight(); // night 1 failed, now between nights: save point

            var saved = Clone(original.State);
            var resumed = RunController.Resume(saved, original.Balance, DefaultHouseRules.All);

            original.BeginNight();
            resumed.BeginNight();
            original.Economy.EmptyTeamBank();
            resumed.Economy.EmptyTeamBank();
            var a = original.EndNight();
            var b = resumed.EndNight();

            Assert.AreEqual(a.PartsTaken.Count, b.PartsTaken.Count);
            for (int i = 0; i < a.PartsTaken.Count; i++)
            {
                Assert.AreEqual(a.PartsTaken[i].Player, b.PartsTaken[i].Player);
                Assert.AreEqual(a.PartsTaken[i].Part, b.PartsTaken[i].Part);
            }
        }

        [Test]
        public void CannotResume_FromTheMiddleOfANight()
        {
            var run = TestData.NewRun();
            run.BeginNight();
            Assert.Throws<InvalidOperationException>(() =>
                RunController.Resume(run.State, run.Balance, DefaultHouseRules.All));
        }

        private static RunState Clone(RunState s)
        {
            var copy = new RunState
            {
                Seed = s.Seed,
                NightStartRngState = s.NightStartRngState,
                NightIndex = s.NightIndex,
                NightInProgress = s.NightInProgress,
                TeamBank = s.TeamBank,
                QuotaPerNight = (long[])s.QuotaPerNight.Clone(),
                HouseRuleIdPerNight = (string[])s.HouseRuleIdPerNight.Clone(),
                Result = s.Result
            };
            foreach (var p in s.Players)
                copy.Players.Add(new PlayerState
                {
                    Key = p.Key, DisplayName = p.DisplayName, PocketMoney = p.PocketMoney,
                    Parts = p.Parts, PledgedParts = p.PledgedParts
                });
            return copy;
        }
    }
}
