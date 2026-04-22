using UnityEngine;

namespace IdleGame.Managers
{
    public static class PlaceholderAudio
    {
        private const int SR = 44100;

        // A minor pentatonic 게임 BGM: 멜로디 + 베이스 + 드럼
        public static AudioClip MakeBgm()
        {
            const float BPM  = 132f;
            const float T16  = 60f / BPM / 4f; // 16분음표 길이(초)

            // A minor pentatonic: A3 C4 D4 E4 G4 A4 C5 E5
            float[] P = { 220.0f, 261.6f, 293.7f, 329.6f, 392.0f, 440.0f, 523.3f, 659.3f };

            // 32스텝 멜로디 (인덱스, -1=쉬기)
            int[] M = {
                4, -1,  5,  4,   2, -1,  4,  2,   1, -1,  2,  4,   5,  4,  2, -1,
                4, -1,  6,  5,   4, -1,  5,  4,   2, -1,  4,  5,   6, -1, -1, -1
            };

            // 베이스 (8스텝마다 = 2박): A2 C3 G2 E2
            float[] Bass = { 110.0f, 130.8f, 98.0f, 82.4f };

            int loopN = (int)(SR * T16 * 32);
            float[] buf = new float[loopN];

            // 멜로디 (square-ish wave: 기음 + 홀수 배음)
            for (int s = 0; s < 32; s++)
            {
                if (M[s] < 0) continue;
                float freq  = P[M[s]];
                int   start = (int)(s * T16 * SR);
                int   len   = (int)(T16 * 0.78f * SR);
                for (int i = 0; i < len && start + i < loopN; i++)
                {
                    float t   = (float)i / SR;
                    float env = Mathf.Clamp01(t / 0.004f) * Mathf.Pow(1f - (float)i / len, 0.45f);
                    float w   = Mathf.Sin(2 * Mathf.PI * freq * t)
                              + 0.30f * Mathf.Sin(2 * Mathf.PI * freq * 3 * t)
                              + 0.15f * Mathf.Sin(2 * Mathf.PI * freq * 5 * t);
                    buf[start + i] += env * 0.20f * w;
                }
            }

            // 베이스
            for (int b = 0; b < 4; b++)
            {
                float freq  = Bass[b];
                int   start = (int)(b * 8 * T16 * SR);
                int   len   = (int)(7.2f * T16 * SR);
                for (int i = 0; i < len && start + i < loopN; i++)
                {
                    float t   = (float)i / SR;
                    float env = Mathf.Clamp01(t / 0.006f) * Mathf.Pow(1f - (float)i / len, 0.28f);
                    buf[start + i] += env * 0.32f * Mathf.Sin(2 * Mathf.PI * freq * t);
                }
            }

            // 킥드럼 (1박, 3박)
            for (int k = 0; k < 2; k++)
            {
                int   start = (int)(k * 16 * T16 * SR);
                float dur   = 0.13f;
                int   len   = (int)(dur * SR);
                for (int i = 0; i < len && start + i < loopN; i++)
                {
                    float t   = (float)i / SR;
                    float env = Mathf.Pow(1f - t / dur, 1.4f);
                    float frq = 160f * Mathf.Exp(-t * 38f) + 48f;
                    buf[start + i] += env * 0.55f * Mathf.Sin(2 * Mathf.PI * frq * t);
                }
            }

            // 스네어 (2박, 4박)
            for (int s = 0; s < 2; s++)
            {
                int   start = (int)((s * 16 + 8) * T16 * SR);
                float dur   = 0.09f;
                int   len   = (int)(dur * SR);
                for (int i = 0; i < len && start + i < loopN; i++)
                {
                    float t   = (float)i / SR;
                    float env = Mathf.Pow(1f - t / dur, 1.8f) * 0.32f;
                    buf[start + i] += env * (0.35f * Mathf.Sin(2 * Mathf.PI * 210f * t)
                                           + 0.65f * (Random.value * 2f - 1f));
                }
            }

            // 하이햇 (8분음표마다)
            for (int h = 0; h < 8; h++)
            {
                int   start = (int)(h * 4 * T16 * SR);
                float dur   = 0.028f;
                int   len   = (int)(dur * SR);
                for (int i = 0; i < len && start + i < loopN; i++)
                {
                    float t   = (float)i / SR;
                    float env = Mathf.Pow(1f - t / dur, 3.5f) * 0.10f;
                    buf[start + i] += env * (Random.value * 2f - 1f);
                }
            }

            // 클리핑 방지 노멀라이즈
            float peak = 0f;
            for (int i = 0; i < loopN; i++) peak = Mathf.Max(peak, Mathf.Abs(buf[i]));
            if (peak > 0f) for (int i = 0; i < loopN; i++) buf[i] /= peak * 1.15f;

            // 루프 페이드 (클릭 방지)
            int fade = SR / 80;
            for (int i = 0; i < fade; i++)
            {
                buf[i]            *= (float)i / fade;
                buf[loopN - 1 - i] *= (float)i / fade;
            }

            var clip = AudioClip.Create("BGM_Game", loopN, 1, SR, false);
            clip.SetData(buf, 0);
            return clip;
        }

        // 몬스터 타격음
        public static AudioClip MakeHit()
        {
            const float duration = 0.18f;
            int n = (int)(SR * duration);
            float[] d = new float[n];
            for (int i = 0; i < n; i++)
            {
                float t    = (float)i / SR;
                float env  = Mathf.Pow(1f - t / duration, 2.5f);
                float freq = 260f * Mathf.Exp(-t * 25f) + 55f;
                d[i] = env * (0.78f * Mathf.Sin(2 * Mathf.PI * freq * t)
                             + 0.22f * (Random.value * 2f - 1f));
            }
            var clip = AudioClip.Create("Hit_Temp", n, 1, SR, false);
            clip.SetData(d, 0);
            return clip;
        }

        // 골드 획득음
        public static AudioClip MakeGoldPing()
        {
            const float duration = 0.14f;
            int n = (int)(SR * duration);
            float[] d = new float[n];
            for (int i = 0; i < n; i++)
            {
                float t    = (float)i / SR;
                float env  = Mathf.Pow(1f - t / duration, 1.8f);
                float freq = 880f + 440f * (t / duration);
                d[i] = 0.45f * env * Mathf.Sin(2 * Mathf.PI * freq * t);
            }
            var clip = AudioClip.Create("Gold_Temp", n, 1, SR, false);
            clip.SetData(d, 0);
            return clip;
        }
    }
}
