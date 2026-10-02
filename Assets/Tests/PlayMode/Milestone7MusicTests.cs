using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace PrehistoricTribe.Tests
{
    /// <summary>Milestone 7 – P2: nhạc nền ngày/đêm (sinh bằng code, thay được bằng file riêng).</summary>
    public class Milestone7MusicTests : PlayModeTestBase
    {
        private static MusicManager Music => MusicManager.Instance;

        [UnityTest]
        public IEnumerator Scene_HasLoopingDayAndNightMusic()
        {
            yield return null;
            Assert.IsNotNull(Music);
            Assert.IsNotNull(Music.DayMusic, "Day track assigned");
            Assert.IsNotNull(Music.NightMusic, "Night track assigned");
            Assert.AreNotSame(Music.DayMusic, Music.NightMusic);
            Assert.Greater(Music.DayMusic.length, 20f, "A real piece, not a blip");
            Assert.Greater(Music.NightMusic.length, 20f);
            Assert.AreSame(Music.DayMusic, Music.CurrentClip, "Daytime plays the day track");
        }

        [UnityTest]
        public IEnumerator Nightfall_SwitchesToNightMusic_AndDawnBack()
        {
            yield return null;
            DayNightCycle.Instance.SetTime(0.85f);
            yield return null;
            Assert.AreSame(Music.NightMusic, Music.CurrentClip);
            DayNightCycle.Instance.SetTime(0.35f);
            yield return null;
            Assert.AreSame(Music.DayMusic, Music.CurrentClip);
        }
    }
}
