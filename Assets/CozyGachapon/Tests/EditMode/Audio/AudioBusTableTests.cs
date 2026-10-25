using System;
using CozyGachapon.Audio;
using CozyGachapon.Contracts;
using NUnit.Framework;

namespace CozyGachapon.Tests.Audio
{
    public class AudioBusTableTests
    {
        [TestCase(AudioBus.Music, AudioBus.Master)]
        [TestCase(AudioBus.Ambience, AudioBus.Master)]
        [TestCase(AudioBus.Sfx, AudioBus.Master)]
        [TestCase(AudioBus.Gameplay, AudioBus.Sfx)]
        [TestCase(AudioBus.Ui, AudioBus.Sfx)]
        [TestCase(AudioBus.Plinko, AudioBus.Gameplay)]
        [TestCase(AudioBus.Gacha, AudioBus.Gameplay)]
        public void ParentMatchesBusTree(AudioBus bus, AudioBus expectedParent)
        {
            Assert.IsTrue(AudioBusTable.TryGetParent(bus, out AudioBus parent));
            Assert.AreEqual(expectedParent, parent);
        }

        [Test]
        public void MasterHasNoParent()
        {
            Assert.IsFalse(AudioBusTable.TryGetParent(AudioBus.Master, out _));
        }

        [Test]
        public void PlinkoAscendsThroughGameplayAndSfxToMaster()
        {
            AudioBus[] expected = { AudioBus.Gameplay, AudioBus.Sfx, AudioBus.Master };
            AudioBus current = AudioBus.Plinko;

            for (int i = 0; i < expected.Length; i++)
            {
                Assert.IsTrue(AudioBusTable.TryGetParent(current, out current));
                Assert.AreEqual(expected[i], current);
            }

            Assert.IsFalse(AudioBusTable.TryGetParent(current, out _));
        }

        [Test]
        public void EveryBusReachesMasterWithoutCycle()
        {
            Array buses = Enum.GetValues(typeof(AudioBus));

            foreach (AudioBus bus in buses)
            {
                AudioBus current = bus;
                int steps = 0;
                while (AudioBusTable.TryGetParent(current, out AudioBus parent))
                {
                    current = parent;
                    steps++;
                    Assert.LessOrEqual(steps, buses.Length);
                }

                Assert.AreEqual(AudioBus.Master, current);
            }
        }
    }
}
