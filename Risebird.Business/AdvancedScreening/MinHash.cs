using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Risebird.Business.AdvancedScreening
{
    /// <summary>
    /// MinHash signatures for near-duplicate text: the estimated Jaccard similarity of two texts'
    /// word 3-shingles is the share of signature positions where they agree. Banding the
    /// signature (32 bands of 4) gives index keys - two texts at ~80% similarity share a band
    /// with high probability, texts at ~30% rarely do - so the duplicate search is an index
    /// lookup rather than a comparison against every resume.
    ///
    /// Deterministic across processes and versions: the hash family is fixed here rather than
    /// seeded from string.GetHashCode, which .NET randomises per process.
    /// </summary>
    public static class MinHash
    {
        public const int SignatureSize = 128;
        public const int BandCount = 32;
        public const int RowsPerBand = SignatureSize / BandCount;
        private const int ShingleSize = 3;

        private static readonly (ulong A, ulong B)[] HashFamily = BuildFamily();

        private static (ulong, ulong)[] BuildFamily()
        {
            // SplitMix64 from a fixed seed: reproducible coefficients.
            var family = new (ulong, ulong)[SignatureSize];
            ulong state = 0x9E3779B97F4A7C15UL;
            ulong Next()
            {
                state += 0x9E3779B97F4A7C15UL;
                var z = state;
                z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
                z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
                return z ^ (z >> 31);
            }
            for (var i = 0; i < SignatureSize; i++)
            {
                family[i] = (Next() | 1UL, Next());
            }
            return family;
        }

        private static ulong Fnv1a64(string s)
        {
            var hash = 14695981039346656037UL;
            foreach (var b in Encoding.UTF8.GetBytes(s))
            {
                hash ^= b;
                hash *= 1099511628211UL;
            }
            return hash;
        }

        /// <summary>The signature of a text, or null when it has too few words to fingerprint.</summary>
        public static uint[] Signature(string text)
        {
            var shingles = AuthenticityFacts.Shingles(text, ShingleSize);
            if (shingles.Count < 5)
            {
                return null;
            }
            var hashes = shingles.Select(Fnv1a64).ToList();
            var sig = new uint[SignatureSize];
            for (var i = 0; i < SignatureSize; i++)
            {
                var (a, b) = HashFamily[i];
                var min = ulong.MaxValue;
                foreach (var h in hashes)
                {
                    var v = a * h + b;
                    if (v < min) min = v;
                }
                sig[i] = (uint)(min >> 32);
            }
            return sig;
        }

        public static decimal Similarity(uint[] a, uint[] b)
        {
            if (a == null || b == null || a.Length != b.Length || a.Length == 0)
            {
                return 0m;
            }
            var same = 0;
            for (var i = 0; i < a.Length; i++)
            {
                if (a[i] == b[i]) same++;
            }
            return Math.Round((decimal)same / a.Length, 2);
        }

        /// <summary>One 64-bit key per band.</summary>
        public static List<(byte BandNo, long BandHash)> Bands(uint[] signature)
        {
            var bands = new List<(byte, long)>(BandCount);
            for (var band = 0; band < BandCount; band++)
            {
                var hash = 14695981039346656037UL ^ (ulong)band;
                for (var r = 0; r < RowsPerBand; r++)
                {
                    hash ^= signature[band * RowsPerBand + r];
                    hash *= 1099511628211UL;
                }
                bands.Add(((byte)band, unchecked((long)hash)));
            }
            return bands;
        }

        public static byte[] ToBytes(uint[] signature)
        {
            var bytes = new byte[signature.Length * 4];
            Buffer.BlockCopy(signature, 0, bytes, 0, bytes.Length);
            return bytes;
        }

        public static uint[] FromBytes(byte[] bytes)
        {
            if (bytes == null || bytes.Length % 4 != 0)
            {
                return null;
            }
            var sig = new uint[bytes.Length / 4];
            Buffer.BlockCopy(bytes, 0, sig, 0, bytes.Length);
            return sig;
        }
    }
}
