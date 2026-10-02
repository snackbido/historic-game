using System.IO;
using UnityEditor;
using UnityEngine;

namespace PrehistoricTribe.EditorTools
{
    /// <summary>
    /// Sinh tiếng động bằng code (M7/P2b): mỗi <see cref="SfxKind"/> một file WAV trong <see cref="GeneratedFolder"/>.
    /// Thay bằng tiếng riêng: đặt file cùng tên (vd <c>Chop.wav</c>, <c>Howl.mp3</c>) vào <see cref="Folder"/> rồi dựng lại
    /// scene — file của người dùng luôn được ưu tiên (<see cref="Resolve"/>).
    /// </summary>
    public static class SfxBuilder
    {
        public const string Folder = "Assets/Audio/SFX";
        public const string GeneratedFolder = Folder + "/Generated";
        private const int Rate = MusicBuilder.Rate;
        private static readonly string[] Extensions = { ".wav", ".mp3", ".ogg" };

        [MenuItem("Tools/Prehistoric/Build Sound Effects")]
        public static void BuildAll()
        {
            Directory.CreateDirectory(GeneratedFolder);
            foreach (SfxKind kind in System.Enum.GetValues(typeof(SfxKind)))
                MusicBuilder.WriteWav($"{GeneratedFolder}/{kind}.wav", Make(kind));
            AssetDatabase.Refresh();
        }

        /// <summary>Tiếng của người dùng (Folder/Kind.*) nếu có, không thì bản sinh bằng code.</summary>
        public static AudioClip Resolve(SfxKind kind)
        {
            foreach (var ext in Extensions)
            {
                var clip = AssetDatabase.LoadAssetAtPath<AudioClip>($"{Folder}/{kind}{ext}");
                if (clip != null) return clip;
            }
            return AssetDatabase.LoadAssetAtPath<AudioClip>($"{GeneratedFolder}/{kind}.wav");
        }

        private static float[] Make(SfxKind kind)
        {
            var rng = new System.Random((int)kind * 31 + 5);
            switch (kind)
            {
                case SfxKind.Chop: return OneShot(Chop(rng), 0.9f);
                case SfxKind.Rustle: return OneShot(Rustle(rng, 0.6f), 0.5f);
                case SfxKind.Splash: return OneShot(Splash(rng), 0.7f);
                case SfxKind.Dig: return OneShot(Dig(rng), 0.8f);
                case SfxKind.Build: return OneShot(Build(rng), 0.85f);
                case SfxKind.Crash: return OneShot(Crash(rng), 0.9f);
                case SfxKind.Harvest: return OneShot(Harvest(rng), 0.7f);
                case SfxKind.Chime: return OneShot(Chime(), 0.6f);
                case SfxKind.Hit: return OneShot(Hit(rng), 0.9f);
                case SfxKind.Hiss: return OneShot(Hiss(rng), 0.5f);
                case SfxKind.Thunder: return OneShot(Thunder(rng), 0.95f);
                case SfxKind.Howl: return OneShot(Howl(rng), 0.7f);
                case SfxKind.Rain: return Normalize(Rain(rng), 0.5f);
                case SfxKind.Fire: return Normalize(FireLoop(rng), 0.55f);
                default: return new float[Rate / 10];
            }
        }

        // ─── Tiếng động ──────────────────────────────────────────────────────
        /// <summary>Rìu bổ vào gỗ: tiếng "cốc" trầm + vụn gỗ.</summary>
        private static float[] Chop(System.Random rng)
        {
            var buf = Buf(0.25f);
            float lp = 0f;
            for (int i = 0; i < buf.Length; i++)
            {
                float t = T(i);
                lp += 0.3f * (Noise(rng) - lp);
                buf[i] = Sin(190f, t) * Mathf.Exp(-t * 30f) + 0.5f * Sin(430f, t) * Mathf.Exp(-t * 45f) + 0.8f * lp * Mathf.Exp(-t * 60f);
            }
            return buf;
        }

        /// <summary>Lá xào xạc: ồn cao, biên độ lổn nhổn theo từng hạt 20ms.</summary>
        private static float[] Rustle(System.Random rng, float seconds)
        {
            var buf = Buf(seconds);
            float lp = 0f, grain = 1f;
            int grainSamples = Rate / 50;
            for (int i = 0; i < buf.Length; i++)
            {
                float t = T(i);
                if (i % grainSamples == 0) grain = 0.3f + (float)rng.NextDouble();
                float x = Noise(rng);
                lp += 0.25f * (x - lp);
                buf[i] = (x - lp) * grain * Mathf.Sin(Mathf.PI * t / seconds);
            }
            return buf;
        }

        /// <summary>Nước bắn: ồn trầm tắt dần + vài bọt "bóp" vút lên.</summary>
        private static float[] Splash(System.Random rng)
        {
            var buf = Buf(0.5f);
            float lp = 0f;
            for (int i = 0; i < buf.Length; i++)
            {
                float t = T(i);
                lp += 0.25f * (Noise(rng) - lp);
                buf[i] = lp * Mathf.Exp(-t * 9f) * 1.4f;
            }
            foreach (float at in new[] { 0.05f, 0.14f, 0.26f })
                Chirp(buf, at, 0.06f, 500f + (float)rng.NextDouble() * 200f, 1100f, 0.35f);
            return buf;
        }

        /// <summary>Cuốc đất: "bịch" trầm + đất lạo xạo.</summary>
        private static float[] Dig(System.Random rng)
        {
            var buf = Buf(0.3f);
            float lp = 0f;
            double phase = 0;
            for (int i = 0; i < buf.Length; i++)
            {
                float t = T(i);
                phase += 2.0 * Mathf.PI * (85f + 55f * Mathf.Exp(-t * 30f)) / Rate;
                lp += 0.15f * (Noise(rng) - lp);
                buf[i] = Mathf.Sin((float)phase) * Mathf.Exp(-t * 18f) + 1.2f * lp * Mathf.Exp(-t * 20f);
            }
            return buf;
        }

        /// <summary>Dựng nhà: ba nhát búa gỗ.</summary>
        private static float[] Build(System.Random rng)
        {
            var buf = Buf(0.7f);
            foreach (float at in new[] { 0f, 0.22f, 0.44f })
            {
                int start = (int)(at * Rate);
                for (int i = 0; i < (int)(0.2f * Rate) && start + i < buf.Length; i++)
                {
                    float t = T(i);
                    float click = i < 110 ? Noise(rng) * (1f - i / 110f) * 0.5f : 0f;
                    buf[start + i] += Sin(320f, t) * Mathf.Exp(-t * 40f) + 0.4f * Sin(760f, t) * Mathf.Exp(-t * 55f) + click;
                }
            }
            return buf;
        }

        /// <summary>Sập đổ: ầm ầm trầm + vài tiếng bịch + gỗ gãy lách tách.</summary>
        private static float[] Crash(System.Random rng)
        {
            var buf = Buf(1.2f);
            float lp = 0f;
            for (int i = 0; i < buf.Length; i++)
            {
                float t = T(i);
                lp += 0.05f * (Noise(rng) - lp);
                buf[i] = lp * 4f * Mathf.Min(1f, t / 0.02f) * Mathf.Exp(-t * 3.5f);
            }
            foreach (float at in new[] { 0f, 0.15f, 0.4f })
            {
                int start = (int)(at * Rate);
                for (int i = 0; i < (int)(0.3f * Rate) && start + i < buf.Length; i++)
                {
                    float t = T(i);
                    buf[start + i] += 0.8f * Sin(70f, t) * Mathf.Exp(-t * 12f);
                }
            }
            for (int k = 0; k < 25; k++) Click(buf, (float)rng.NextDouble() * 0.8f, 0.004f, 0.25f * (float)rng.NextDouble(), rng);
            return buf;
        }

        /// <summary>Gặt: lá xào xạc ngắn + hai nốt "tưng" vui tai.</summary>
        private static float[] Harvest(System.Random rng)
        {
            var buf = Buf(0.45f);
            var rustle = Rustle(rng, 0.25f);
            for (int i = 0; i < rustle.Length; i++) buf[i] += rustle[i] * 0.6f;
            Pling(buf, 0.05f, 660f, 0.5f);
            Pling(buf, 0.16f, 880f, 0.5f);
            return buf;
        }

        /// <summary>Lấp lánh: ba tiếng chuông đi lên.</summary>
        private static float[] Chime()
        {
            var buf = Buf(1.2f);
            float[] notes = { 880f, 1046.5f, 1318.5f };
            for (int n = 0; n < notes.Length; n++)
            {
                int start = (int)(n * 0.12f * Rate);
                for (int i = 0; start + i < buf.Length; i++)
                {
                    float t = T(i);
                    // Chuông: họa âm không nguyên (×2,76) cho tiếng kim loại.
                    buf[start + i] += (Sin(notes[n], t) + 0.4f * Sin(notes[n] * 2.76f, t) * Mathf.Exp(-t * 6f)) * Mathf.Exp(-t * 4f);
                }
            }
            return buf;
        }

        /// <summary>Đánh trúng: "bụp" ngắn, chắc.</summary>
        private static float[] Hit(System.Random rng)
        {
            var buf = Buf(0.18f);
            float lp = 0f;
            double phase = 0;
            for (int i = 0; i < buf.Length; i++)
            {
                float t = T(i);
                phase += 2.0 * Mathf.PI * (90f + 60f * Mathf.Exp(-t * 40f)) / Rate;
                lp += 0.4f * (Noise(rng) - lp);
                buf[i] = Mathf.Sin((float)phase) * Mathf.Exp(-t * 35f) + 0.7f * lp * Mathf.Exp(-t * 50f);
            }
            return buf;
        }

        /// <summary>Hơi nước xèo xèo.</summary>
        private static float[] Hiss(System.Random rng)
        {
            var buf = Buf(0.9f);
            float lp = 0f;
            for (int i = 0; i < buf.Length; i++)
            {
                float t = T(i);
                float x = Noise(rng);
                lp += 0.2f * (x - lp);
                buf[i] = (x - lp) * Mathf.Min(1f, t / 0.03f) * Mathf.Exp(-t * 3f);
            }
            return buf;
        }

        /// <summary>Sấm: tiếng nổ đanh rồi ầm ì kéo dài, to nhỏ thất thường.</summary>
        private static float[] Thunder(System.Random rng)
        {
            var buf = Buf(3f);
            float rumble = 0f, rumble2 = 0f, swell = 0f, swellTarget = 1f;
            for (int i = 0; i < buf.Length; i++)
            {
                float t = T(i);
                float x = Noise(rng);
                rumble += 0.03f * (x - rumble);
                rumble2 += 0.03f * (rumble - rumble2);
                if (i % (Rate / 8) == 0) swellTarget = 0.4f + (float)rng.NextDouble();
                swell += 0.0005f * (swellTarget - swell);
                float env = t < 0.08f ? t / 0.08f : Mathf.Exp(-(t - 0.08f) * 1.1f);
                float crack = x * Mathf.Exp(-t * 22f) * 0.35f;
                buf[i] = rumble2 * 18f * env * swell + crack;
            }
            return buf;
        }

        /// <summary>Sói hú: tiếng vút lên, ngân, rồi trầm xuống; con thứ hai hú theo cao hơn.</summary>
        private static float[] Howl(System.Random rng)
        {
            var buf = Buf(2.9f);
            HowlVoice(buf, 0f, 2.4f, 1f, 1f, rng);
            HowlVoice(buf, 0.5f, 2.3f, 1.13f, 0.5f, rng);
            return buf;
        }

        private static void HowlVoice(float[] buf, float startSeconds, float seconds, float pitch, float amp, System.Random rng)
        {
            int start = (int)(startSeconds * Rate);
            int length = (int)(seconds * Rate);
            double phase = 0;
            for (int i = 0; i < length && start + i < buf.Length; i++)
            {
                float t = T(i);
                float u = t / seconds;
                float freq = u < 0.18f ? Mathf.Lerp(380f, 620f, Smooth(u / 0.18f))
                    : u < 0.65f ? Mathf.Lerp(620f, 660f, (u - 0.18f) / 0.47f)
                    : Mathf.Lerp(660f, 420f, Smooth((u - 0.65f) / 0.35f));
                freq *= pitch * (1f + 0.012f * Mathf.Sin(2f * Mathf.PI * 5.5f * t));
                phase += 2.0 * Mathf.PI * freq / Rate;
                float p = (float)phase;
                float tone = Mathf.Sin(p) + 0.35f * Mathf.Sin(2f * p) + 0.12f * Mathf.Sin(3f * p);
                float env = Mathf.Min(1f, t / 0.25f) * Mathf.Clamp01((seconds - t) / 0.6f);
                buf[start + i] += (tone + Noise(rng) * 0.04f) * env * amp;
            }
        }

        /// <summary>Mưa (lặp 4s): rào rào đều + giọt lách tách rải ngẫu nhiên, quấn vòng cho liền.</summary>
        private static float[] Rain(System.Random rng)
        {
            var buf = Buf(4f);
            float lp = 0f;
            for (int i = 0; i < buf.Length; i++)
            {
                lp += 0.5f * (Noise(rng) - lp);
                buf[i] = lp * 0.35f;
            }
            for (int k = 0; k < 320; k++)
            {
                int start = rng.Next(buf.Length);
                float freq = 1500f + (float)rng.NextDouble() * 2000f;
                float amp = 0.1f + 0.3f * (float)rng.NextDouble();
                for (int i = 0; i < (int)(0.012f * Rate); i++)
                {
                    float t = T(i);
                    buf[(start + i) % buf.Length] += Sin(freq, t) * Mathf.Exp(-t * 400f) * amp;
                }
            }
            return buf;
        }

        /// <summary>Lửa cháy (lặp 3s): tiếng phù phù trầm + lách tách, nổ tí tách.</summary>
        private static float[] FireLoop(System.Random rng)
        {
            var buf = Buf(3f);
            float lp = 0f, lp2 = 0f;
            for (int i = 0; i < buf.Length; i++)
            {
                lp += 0.04f * (Noise(rng) - lp);
                lp2 += 0.04f * (lp - lp2);
                buf[i] = lp2 * 6f;
            }
            for (int k = 0; k < 140; k++)
            {
                int start = rng.Next(buf.Length);
                int length = (int)((0.002f + 0.005f * (float)rng.NextDouble()) * Rate);
                float amp = 0.15f + 0.6f * (float)rng.NextDouble() * (float)rng.NextDouble();
                float previous = 0f;
                for (int i = 0; i < length; i++)
                {
                    float x = Noise(rng);
                    buf[(start + i) % buf.Length] += (x - previous) * amp * (1f - i / (float)length);
                    previous = x;
                }
            }
            return buf;
        }

        // ─── Tiện ích ────────────────────────────────────────────────────────
        private static float[] Buf(float seconds) => new float[Mathf.CeilToInt(seconds * Rate)];
        private static float T(int sample) => sample / (float)Rate;
        private static float Noise(System.Random rng) => (float)(rng.NextDouble() * 2.0 - 1.0);
        private static float Sin(float freq, float t) => Mathf.Sin(2f * Mathf.PI * freq * t);
        private static float Smooth(float u) => u * u * (3f - 2f * u);

        private static void Chirp(float[] buf, float at, float seconds, float from, float to, float amp)
        {
            int start = (int)(at * Rate);
            double phase = 0;
            for (int i = 0; i < (int)(seconds * Rate) && start + i < buf.Length; i++)
            {
                float u = i / (seconds * Rate);
                phase += 2.0 * Mathf.PI * Mathf.Lerp(from, to, u) / Rate;
                buf[start + i] += Mathf.Sin((float)phase) * Mathf.Sin(Mathf.PI * u) * amp;
            }
        }

        private static void Pling(float[] buf, float at, float freq, float amp)
        {
            int start = (int)(at * Rate);
            for (int i = 0; start + i < buf.Length; i++)
            {
                float t = T(i);
                buf[start + i] += (Sin(freq, t) + 0.3f * Sin(freq * 2f, t)) * Mathf.Exp(-t * 14f) * amp;
            }
        }

        private static void Click(float[] buf, float at, float seconds, float amp, System.Random rng)
        {
            int start = (int)(at * Rate);
            int length = (int)(seconds * Rate);
            for (int i = 0; i < length && start + i < buf.Length; i++)
                buf[start + i] += Noise(rng) * amp * (1f - i / (float)length);
        }

        /// <summary>Tiếng một lần: vào/ra cực ngắn cho khỏi "tách" ở mép, rồi chuẩn hóa.</summary>
        private static float[] OneShot(float[] buf, float peak)
        {
            int fadeIn = Mathf.Min(buf.Length, Rate / 500);
            int fadeOut = Mathf.Min(buf.Length, Rate / 100);
            for (int i = 0; i < fadeIn; i++) buf[i] *= i / (float)fadeIn;
            for (int i = 0; i < fadeOut; i++) buf[buf.Length - 1 - i] *= i / (float)fadeOut;
            return Normalize(buf, peak);
        }

        private static float[] Normalize(float[] buf, float peak)
        {
            float max = 0f;
            foreach (float s in buf) max = Mathf.Max(max, Mathf.Abs(s));
            if (max > 0f)
                for (int i = 0; i < buf.Length; i++) buf[i] *= peak / max;
            return buf;
        }
    }
}
