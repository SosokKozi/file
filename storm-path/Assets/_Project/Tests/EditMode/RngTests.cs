using NUnit.Framework;
using StormPath.Sim.Core;

namespace StormPath.Tests
{
    public class RngTests
    {
        [Test]
        public void SameSeed_SameSequence()
        {
            var a = new Rng(42);
            var b = new Rng(42);
            for (int i = 0; i < 100; i++) Assert.AreEqual(a.NextU64(), b.NextU64());
        }

        [Test]
        public void DifferentSeeds_DifferentSequences()
        {
            Assert.AreNotEqual(new Rng(1).NextU64(), new Rng(2).NextU64());
        }

        [Test]
        public void ZeroSeed_Works()
        {
            var r = new Rng(0);
            Assert.AreNotEqual(r.NextU64(), r.NextU64());
        }

        [Test]
        public void NextInt_StaysInRange_AndCoversAllValues()
        {
            var r = new Rng(7);
            var seen = new bool[5];
            for (int i = 0; i < 1000; i++)
            {
                int v = r.NextInt(5);
                Assert.That(v, Is.InRange(0, 4));
                seen[v] = true;
            }
            Assert.That(seen, Is.All.True);
        }

        [Test]
        public void Range_IsInclusive()
        {
            var r = new Rng(9);
            bool lo = false, hi = false;
            for (int i = 0; i < 500; i++)
            {
                int v = r.Range(-1, 1);
                Assert.That(v, Is.InRange(-1, 1));
                lo |= v == -1;
                hi |= v == 1;
            }
            Assert.IsTrue(lo && hi);
        }

        [Test]
        public void Hash_Combine_IsStableAndSensitive()
        {
            Assert.AreEqual(Hash.Combine(5, 3, 2), Hash.Combine(5, 3, 2));
            Assert.AreNotEqual(Hash.Combine(5, 3, 2), Hash.Combine(5, 2, 3));
            Assert.AreNotEqual(Hash.Combine(5, 3, 2), Hash.Combine(6, 3, 2));
        }
    }
}
