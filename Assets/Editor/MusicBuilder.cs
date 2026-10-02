using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace PrehistoricTribe.EditorTools
{
    /// <summary>
    /// Sinh nhạc nền đơn giản bằng code (M7/P2): ngày (trống da, đàn gảy, sáo) và đêm (nền trầm, tiếng gảy thưa, dế kêu),
    /// thang ngũ cung La thứ, lặp liền mạch. Ghi ra <see cref="GeneratedFolder"/>.
    /// Thay bằng nhạc riêng: đặt file <c>Day</c> / <c>Night</c> (.wav/.mp3/.ogg) vào <see cref="Folder"/> rồi dựng lại scene —
    /// file của người dùng luôn được ưu tiên (<see cref="Resolve"/>), file sinh ra không bao giờ đè lên nó.
    /// </summary>
    public static class MusicBuilder
    {
        public const string Folder = "Assets/Audio/Music";
        public const string GeneratedFolder = Folder + "/Generated";
        private const int Rate = 22050;
        private static readonly string[] Extensions = { ".wav", ".mp3", ".ogg" };

        [MenuItem("Tools/Prehistoric/Build Music")]
        public static void BuildAll()
        {
            Directory.CreateDirectory(GeneratedFolder);
            WriteWav($"{GeneratedFolder}/Day.wav", Day());
            WriteWav($"{GeneratedFolder}/Night.wav", Night());
            AssetDatabase.Refresh();
        }

        /// <summary>Nhạc của người dùng (Folder/name.*) nếu có, không thì bản sinh bằng code.</summary>
        public static AudioClip Resolve(string name)
        {
            foreach (var ext in Extensions)
            {
                var clip = AssetDatabase.LoadAssetAtPath<AudioClip>($"{Folder}/{name}{ext}");
                if (clip != null) return clip;
            }
            return AssetDatabase.LoadAssetAtPath<AudioClip>($"{GeneratedFolder}/{name}.wav");
        }

        // ─── Bản nhạc ────────────────────────────────────────────────────────
        // Ngũ cung La thứ: La Đô Rê Mi Sol (bán cung tính từ La).
        private static readonly int[] Pentatonic = { 0, 3, 5, 7, 10 };

        /// <summary>Tần số bậc <paramref name="degree"/> của ngũ cung (bậc 0 = nốt La có tần số <paramref name="baseA"/>, âm được).</summary>
        private static float Note(int degree, float baseA)
        {
            int octave = Mathf.FloorToInt(degree / 5f);
            int semis = Pentatonic[degree - octave * 5] + 12 * octave;
            return baseA * Mathf.Pow(2f, semis / 12f);
        }

        private static float[] Day()
        {
            const float bpm = 92f;
            const int bars = 16;
            int beat = Mathf.RoundToInt(Rate * 60f / bpm);
            var buf = new float[beat * 4 * bars];
            var rng = new System.Random(7);
            // Hợp âm theo ô nhịp: La La Sol Sol Rê Rê Mi Mi (lặp 2 lần).
            int[] roots = { 0, 0, -1, -1, 2, 2, 3, 3 };

            for (int bar = 0; bar < bars; bar++)
            {
                int start = bar * 4 * beat;
                int root = roots[bar % roots.Length];

                // Đàn gảy: nốt trầm đầu ô + rải hợp âm móc đơn.
                Pluck(buf, start, Note(root, 110f), 1.8f, 0.32f, rng);
                int[] arp = { 0, 3, 5, 3 };
                for (int e = 0; e < 8; e++)
                    Pluck(buf, start + e * beat / 2, Note(root + arp[e % 4], 220f), 0.6f, e % 2 == 0 ? 0.16f : 0.11f, rng);

                // Trống da: phách 1, 3, và móc cuối ô; lắc ống tre đều móc đơn.
                Drum(buf, start, 0.6f, rng);
                Drum(buf, start + 2 * beat, 0.42f, rng);
                Drum(buf, start + 7 * beat / 2, 0.22f, rng);
                for (int e = 0; e < 8; e++)
                    Shaker(buf, start + e * beat / 2, e % 2 == 1 ? 0.09f : 0.04f, rng);
            }

            // Sáo: ô 5–16 theo khuôn A B A (4 ô mỗi câu), 4 ô đầu chỉ có trống + đàn.
            var phraseA = Phrase(rng, roots, 0, endOnRoot: false);
            var phraseB = Phrase(rng, roots, 4, endOnRoot: true);
            PlayPhrase(buf, phraseA, 4, beat);
            PlayPhrase(buf, phraseB, 8, beat);
            PlayPhrase(buf, phraseA, 12, beat);

            return Master(buf);
        }

        private static float[] Night()
        {
            const float bpm = 60f;
            const int bars = 8;
            int beat = Mathf.RoundToInt(Rate * 60f / bpm);
            var buf = new float[beat * 4 * bars];
            var rng = new System.Random(19);
            int[] roots = { 0, 0, 1, 1, -1, -1, 3, 3 }; // La La Đô Đô Sol Sol Mi Mi

            for (int bar = 0; bar < bars; bar++)
            {
                int start = bar * 4 * beat;
                int root = roots[bar];
                // Nền trầm: nốt gốc + quãng năm + quãng tám, vào/ra chậm, chồng lên ô sau cho liền.
                Pad(buf, start, 5.5f, new[] { Note(root, 110f), Note(root + 3, 110f), Note(root + 5, 110f) }, 0.11f);
                // Vài tiếng gảy thưa.
                int notes = 1 + rng.Next(2);
                for (int k = 0; k < notes; k++)
                {
                    int at = start + rng.Next(8) * beat / 2;
                    Pluck(buf, at, Note(root + 5 + rng.Next(5), 220f), 2f, 0.14f, rng);
                }
                if (bar % 2 == 0) Drum(buf, start, 0.18f, rng); // nhịp tim nhẹ
            }

            // Dế kêu: cụm 3 tiếng ríc, rải ngẫu nhiên.
            for (int t = rng.Next(Rate); t < buf.Length; t += Rate + rng.Next(2 * Rate))
                for (int c = 0; c < 3; c++)
                    Cricket(buf, t + c * Rate / 14, 0.025f);

            return Master(buf);
        }

        // ─── Giai điệu sáo ───────────────────────────────────────────────────
        private struct MelodyNote { public float beat; public float length; public int degree; }

        private static readonly float[][] Rhythms =
        {
            new[] { 1f, 1f, 2f }, new[] { 2f, 1f, 1f }, new[] { 1.5f, 0.5f, 2f }, new[] { 1f, 1f, 1f, 1f },
            new[] { 0.5f, 0.5f, 1f, 2f }, new[] { 3f, 1f }, new[] { 1f, 0.5f, 0.5f, 2f }
        };

        /// <summary>Câu nhạc 4 ô: nhịp chọn ngẫu nhiên, cao độ đi từng bước nhỏ, nốt cuối mỗi ô rơi vào nốt hợp âm.</summary>
        private static System.Collections.Generic.List<MelodyNote> Phrase(System.Random rng, int[] roots, int firstBar, bool endOnRoot)
        {
            var notes = new System.Collections.Generic.List<MelodyNote>();
            int degree = 5; // La giữa
            for (int bar = 0; bar < 4; bar++)
            {
                float[] rhythm = bar == 3 ? new[] { 1f, 1f, 2f } : Rhythms[rng.Next(Rhythms.Length)];
                int root = roots[(firstBar + bar) % roots.Length];
                float at = bar * 4f;
                for (int i = 0; i < rhythm.Length; i++)
                {
                    bool last = i == rhythm.Length - 1;
                    if (last)
                    {
                        // Nốt ngân cuối ô: nốt gốc hoặc quãng năm của hợp âm, gần cao độ hiện tại nhất.
                        int target = endOnRoot && bar == 3 ? root + 5 : (rng.Next(2) == 0 ? root + 5 : root + 3);
                        degree = target;
                    }
                    else
                    {
                        degree += rng.Next(-2, 3);
                    }
                    degree = Mathf.Clamp(degree, 2, 10);
                    notes.Add(new MelodyNote { beat = at, length = rhythm[i], degree = degree });
                    at += rhythm[i];
                }
            }
            return notes;
        }

        private static void PlayPhrase(float[] buf, System.Collections.Generic.List<MelodyNote> phrase, int startBar, int beat)
        {
            foreach (var note in phrase)
            {
                int at = (startBar * 4) * beat + Mathf.RoundToInt(note.beat * beat);
                Flute(buf, at, Note(note.degree, 220f), note.length * beat / (float)Rate * 0.92f, 0.2f);
            }
        }

        // ─── Nhạc cụ ─────────────────────────────────────────────────────────
        private static void Add(float[] buf, int index, float value)
        {
            int n = buf.Length;
            buf[((index % n) + n) % n] += value; // quấn vòng → đuôi nốt cuối nối vào đầu bài, lặp liền mạch
        }

        /// <summary>Dây gảy (Karplus–Strong): ồn trắng chạy qua vòng trễ có lọc → tiếng dây tắt dần.</summary>
        private static void Pluck(float[] buf, int start, float freq, float seconds, float amp, System.Random rng)
        {
            int period = Mathf.Max(2, Mathf.RoundToInt(Rate / freq));
            var line = new float[period];
            for (int i = 0; i < period; i++) line[i] = (float)(rng.NextDouble() * 2.0 - 1.0);
            int length = (int)(seconds * Rate);
            int fade = Mathf.Min(600, length);
            int idx = 0;
            for (int i = 0; i < length; i++)
            {
                float v = line[idx];
                int next = (idx + 1) % period;
                line[idx] = 0.497f * (line[idx] + line[next]);
                idx = next;
                float env = i < length - fade ? 1f : (length - i) / (float)fade;
                Add(buf, start + i, v * amp * env);
            }
        }

        /// <summary>Sáo trúc: sóng sin + họa âm nhẹ, hơi thổi, rung (vibrato) khi ngân.</summary>
        private static void Flute(float[] buf, int start, float freq, float seconds, float amp)
        {
            int length = (int)(seconds * Rate);
            var noise = new System.Random(start);
            double phase = 0;
            for (int i = 0; i < length; i++)
            {
                float t = i / (float)Rate;
                float attack = Mathf.Min(1f, t / 0.05f);
                float release = Mathf.Clamp01((seconds - t) / 0.1f);
                float vibrato = t > 0.25f ? 1f + 0.006f * Mathf.Sin(2f * Mathf.PI * 5f * t) : 1f;
                phase += 2.0 * Mathf.PI * freq * vibrato / Rate;
                float tone = Mathf.Sin((float)phase) + 0.22f * Mathf.Sin(2f * (float)phase) + 0.07f * Mathf.Sin(3f * (float)phase);
                float breath = ((float)noise.NextDouble() - 0.5f) * 0.05f;
                Add(buf, start + i, (tone + breath) * amp * attack * release);
            }
        }

        /// <summary>Trống da: sóng sin trầm tụt cao độ nhanh + tiếng "bộp" ngắn.</summary>
        private static void Drum(float[] buf, int start, float amp, System.Random rng)
        {
            int length = (int)(0.45f * Rate);
            double phase = 0;
            for (int i = 0; i < length; i++)
            {
                float t = i / (float)Rate;
                float freq = 55f + 75f * Mathf.Exp(-t * 28f);
                phase += 2.0 * Mathf.PI * freq / Rate;
                float body = Mathf.Sin((float)phase) * Mathf.Exp(-t * 7f);
                float click = i < 80 ? ((float)rng.NextDouble() - 0.5f) * (1f - i / 80f) * 0.6f : 0f;
                Add(buf, start + i, (body + click) * amp);
            }
        }

        /// <summary>Ống lắc tre: tiếng xào xạc ngắn (ồn lọc cao).</summary>
        private static void Shaker(float[] buf, int start, float amp, System.Random rng)
        {
            int length = (int)(0.07f * Rate);
            float previous = 0f;
            for (int i = 0; i < length; i++)
            {
                float x = (float)(rng.NextDouble() * 2.0 - 1.0);
                float high = x - previous; // lọc thông cao đơn giản
                previous = x;
                float t = i / (float)Rate;
                Add(buf, start + i, high * amp * Mathf.Min(1f, t / 0.005f) * Mathf.Exp(-t * 55f));
            }
        }

        /// <summary>Nền (pad): vài sóng sin hơi lệch tần, vào/ra chậm.</summary>
        private static void Pad(float[] buf, int start, float seconds, float[] freqs, float amp)
        {
            int length = (int)(seconds * Rate);
            for (int i = 0; i < length; i++)
            {
                float t = i / (float)Rate;
                float env = Mathf.Min(1f, t / 1.5f) * Mathf.Clamp01((seconds - t) / 1.8f);
                float v = 0f;
                foreach (float f in freqs)
                    v += Mathf.Sin(2f * Mathf.PI * f * t) + 0.5f * Mathf.Sin(2f * Mathf.PI * f * 1.004f * t);
                Add(buf, start + i, v * amp * env / freqs.Length);
            }
        }

        private static void Cricket(float[] buf, int start, float amp)
        {
            int length = (int)(0.035f * Rate);
            for (int i = 0; i < length; i++)
            {
                float t = i / (float)Rate;
                float env = Mathf.Sin(Mathf.PI * i / length);
                Add(buf, start + i, Mathf.Sin(2f * Mathf.PI * 4300f * t) * env * amp);
            }
        }

        /// <summary>Nén mềm (tanh) cho khỏi vỡ tiếng rồi chuẩn hóa đỉnh về 0,8.</summary>
        private static float[] Master(float[] buf)
        {
            float peak = 0f;
            for (int i = 0; i < buf.Length; i++)
            {
                buf[i] = (float)System.Math.Tanh(buf[i] * 1.2f);
                peak = Mathf.Max(peak, Mathf.Abs(buf[i]));
            }
            if (peak > 0f)
                for (int i = 0; i < buf.Length; i++) buf[i] *= 0.8f / peak;
            return buf;
        }

        private static void WriteWav(string path, float[] samples)
        {
            using (var stream = new FileStream(path, FileMode.Create))
            using (var w = new BinaryWriter(stream))
            {
                int dataBytes = samples.Length * 2;
                w.Write(Encoding.ASCII.GetBytes("RIFF"));
                w.Write(36 + dataBytes);
                w.Write(Encoding.ASCII.GetBytes("WAVE"));
                w.Write(Encoding.ASCII.GetBytes("fmt "));
                w.Write(16);
                w.Write((short)1);   // PCM
                w.Write((short)1);   // mono
                w.Write(Rate);
                w.Write(Rate * 2);   // byte/giây
                w.Write((short)2);   // byte/mẫu
                w.Write((short)16);  // bit
                w.Write(Encoding.ASCII.GetBytes("data"));
                w.Write(dataBytes);
                foreach (float s in samples)
                    w.Write((short)Mathf.Clamp(Mathf.RoundToInt(s * 32767f), -32768, 32767));
            }
        }
    }
}
