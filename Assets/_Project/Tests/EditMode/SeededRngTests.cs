using NUnit.Framework;
using SITG.Core.Random;

namespace SITG.Tests
{
    public class SeededRngTests
    {
        [Test]
        public void SameSeed_GivesSameNumbers()
        {
            var a = new SeededRng(1234);
            var b = new SeededRng(1234);
            for (int i = 0; i < 100; i++) Assert.AreEqual(a.NextUInt(), b.NextUInt());
        }

        [Test]
        public void DifferentSeeds_GiveDifferentNumbers()
        {
            var a = new SeededRng(1);
            var b = new SeededRng(2);
            int same = 0;
            for (int i = 0; i < 100; i++) if (a.NextUInt() == b.NextUInt()) same++;
            Assert.Less(same, 3);
        }

        [Test]
        public void NextInt_StaysInRange_AndHitsEveryValue()
        {
            var rng = new SeededRng(99);
            var seen = new bool[13];
            for (int i = 0; i < 5000; i++)
            {
                int v = rng.NextInt(1, 14);
                Assert.That(v, Is.InRange(1, 13));
                seen[v - 1] = true;
            }
            foreach (var s in seen) Assert.IsTrue(s);
        }

        [Test]
        public void FromState_ContinuesTheExactSameSequence()
        {
            var original = new SeededRng(7);
            original.NextUInt();
            var copy = SeededRng.FromState(original.State);
            for (int i = 0; i < 50; i++) Assert.AreEqual(original.NextUInt(), copy.NextUInt());
        }
    }
}
