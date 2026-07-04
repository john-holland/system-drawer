using Locomotion.Narrative.Music;
using NUnit.Framework;

namespace Locomotion.Narrative.Tests
{
    public sealed class MusicTheoryTests
    {
        [Test]
        public void TonalDistance_C_to_G_LessThan_C_to_Gb()
        {
            float cG = MusicTheory.TonalDistance(0, 7);
            float cGb = MusicTheory.TonalDistance(0, 6);
            Assert.Less(cG, cGb);
        }

        [Test]
        public void IsFifthStep_G_to_D()
        {
            Assert.IsTrue(MusicTheory.IsFifthStep(7, 2));
        }

        [Test]
        public void ModulationBank_FifthChain_IncreasesSavings()
        {
            var bank = new ModulationSavingsBank { currentTonic = 7 };
            bank.OnSectionAdvance(2, 4);
            bank.OnSectionAdvance(9, 4);
            Assert.Greater(bank.savings, 0f);
            Assert.Greater(bank.fifthStepsInRow, 0);
        }

        [Test]
        public void ModulationBank_Oscillation_Penalized()
        {
            var bank = new ModulationSavingsBank { currentTonic = 0 };
            bank.OnSectionAdvance(7, 4);
            bank.OnSectionAdvance(0, 4);
            float penalty = bank.OscillationPenalty(7);
            Assert.Greater(penalty, 0f);
        }

        [Test]
        public void RhythmQuadWalk_Deterministic()
        {
            var tree = new RhythmMeterQuadTree(5, seed: 42);
            RhythmMeterTemplate a = tree.Walk("Q2.1.7", 12345, 0.5f, 0.5f);
            RhythmMeterTemplate b = tree.Walk("Q2.1.7", 12345, 0.5f, 0.5f);
            Assert.AreEqual(a.quadPathId, b.quadPathId);
            Assert.AreEqual(a.beatsPerBar, b.beatsPerBar);
        }
    }
}
