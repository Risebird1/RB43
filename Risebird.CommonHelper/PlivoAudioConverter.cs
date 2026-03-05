using System;

namespace Risebird.CommonHelper
{
    /// <summary>
    /// Converts audio between the formats used by Plivo (μ-law 8kHz) and
    /// OpenAI Realtime API (PCM 16-bit signed, 24kHz little-endian).
    ///
    /// Pipeline (inbound — caller audio → OpenAI):
    ///   Plivo mulaw 8kHz  →  decode → PCM16 8kHz  →  upsample 3× → PCM16 24kHz
    ///
    /// Pipeline (outbound — OpenAI audio → Plivo caller):
    ///   PCM16 24kHz  →  downsample ÷3 → PCM16 8kHz  →  encode → mulaw 8kHz
    /// </summary>
    public static class PlivoAudioConverter
    {
        // μ-law decode look-up table (256 entries → 16-bit PCM)
        private static readonly short[] MulawDecodeTable = BuildMulawDecodeTable();

        private static short[] BuildMulawDecodeTable()
        {
            var table = new short[256];
            for (int i = 0; i < 256; i++)
            {
                int ulaw = ~i & 0xFF;
                int sign = ulaw & 0x80;
                int exp = (ulaw >> 4) & 0x07;
                int mant = ulaw & 0x0F;
                int sample = (((mant << 3) + 0x84) << exp) - 0x84;
                table[i] = (short)(sign != 0 ? -sample : sample);
            }
            return table;
        }

        /// <summary>
        /// Encodes a 16-bit signed PCM sample to a single μ-law byte.
        /// </summary>
        private static byte PcmToMulaw(short pcm)
        {
            const int BIAS = 0x84;
            const int CLIP = 32635;

            int sign = (pcm >> 8) & 0x80;
            if (sign != 0) pcm = (short)-pcm;
            if (pcm > CLIP) pcm = CLIP;

            pcm = (short)(pcm + BIAS);
            int exp = 7;
            for (int expMask = 0x4000; (pcm & expMask) == 0 && exp > 0; exp--, expMask >>= 1) { }
            int mantissa = (pcm >> (exp + 3)) & 0x0F;
            int ulawByte = ~(sign | (exp << 4) | mantissa) & 0xFF;
            return (byte)ulawByte;
        }

        /// <summary>
        /// Converts Plivo mulaw 8kHz bytes to OpenAI PCM16 24kHz bytes.
        /// </summary>
        public static byte[] MulawToPcm24k(byte[] mulawBytes)
        {
            // Step 1: decode mulaw → PCM16 (8kHz)
            var pcm8k = new short[mulawBytes.Length];
            for (int i = 0; i < mulawBytes.Length; i++)
                pcm8k[i] = MulawDecodeTable[mulawBytes[i]];

            // Step 2: upsample 8kHz → 24kHz by linear interpolation (factor 3)
            int outputSamples = pcm8k.Length * 3;
            var pcm24k = new byte[outputSamples * 2]; // 2 bytes per 16-bit sample

            int outIdx = 0;
            for (int i = 0; i < pcm8k.Length; i++)
            {
                short s0 = pcm8k[i];
                short s1 = (i + 1 < pcm8k.Length) ? pcm8k[i + 1] : s0;

                // Three interpolated samples per input sample
                for (int frac = 0; frac < 3; frac++)
                {
                    short interp = (short)(s0 + (s1 - s0) * frac / 3);
                    pcm24k[outIdx++] = (byte)(interp & 0xFF);
                    pcm24k[outIdx++] = (byte)((interp >> 8) & 0xFF);
                }
            }

            return pcm24k;
        }

        /// <summary>
        /// Converts OpenAI PCM16 24kHz bytes to Plivo mulaw 8kHz bytes.
        /// </summary>
        public static byte[] Pcm24kToMulaw(byte[] pcm24kBytes)
        {
            if (pcm24kBytes == null || pcm24kBytes.Length < 2)
                return Array.Empty<byte>();

            int totalSamples = pcm24kBytes.Length / 2;

            // Step 1: downsample 24kHz → 8kHz by averaging every 3 samples (factor 3)
            int outputSamples = totalSamples / 3;
            if (outputSamples == 0) return Array.Empty<byte>();

            var mulaw = new byte[outputSamples];
            for (int i = 0; i < outputSamples; i++)
            {
                long sum = 0;
                int count = 0;
                for (int j = 0; j < 3; j++)
                {
                    int srcIdx = (i * 3 + j) * 2;
                    if (srcIdx + 1 < pcm24kBytes.Length)
                    {
                        short sample = (short)(pcm24kBytes[srcIdx] | (pcm24kBytes[srcIdx + 1] << 8));
                        sum += sample;
                        count++;
                    }
                }
                short avg = count > 0 ? (short)(sum / count) : (short)0;

                // Step 2: encode PCM16 → mulaw
                mulaw[i] = PcmToMulaw(avg);
            }

            return mulaw;
        }
    }
}
