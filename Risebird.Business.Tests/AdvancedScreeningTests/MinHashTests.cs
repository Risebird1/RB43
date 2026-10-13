using System.Linq;
using Risebird.Business.AdvancedScreening;

namespace Risebird.Business.Tests.AdvancedScreeningTests
{
    /// <summary>
    /// The duplicate search depends on MinHash being stable across processes (it is stored) and on
    /// near-duplicates sharing an LSH band while unrelated text does not.
    /// </summary>
    public class MinHashTests
    {
        private const string Project =
            "Designed and built a Kafka based settlement pipeline processing twelve thousand transactions per second " +
            "for the payouts platform, reduced p99 latency from 900 milliseconds to 320 milliseconds by moving " +
            "reconciliation to idempotent consumers and partition aware batching, mentored three engineers on the team.";

        [Fact]
        public void The_same_text_always_gives_the_same_signature()
        {
            Assert.Equal(MinHash.Signature(Project), MinHash.Signature(Project));
            Assert.Equal(MinHash.SignatureSize, MinHash.Signature(Project).Length);
        }

        [Fact]
        public void A_lightly_edited_copy_is_highly_similar_and_shares_a_band()
        {
            var edited = Project.Replace("mentored three engineers on the team.", "mentored two engineers.");
            var a = MinHash.Signature(Project);
            var b = MinHash.Signature(edited);

            Assert.True(MinHash.Similarity(a, b) >= 0.7m);
            Assert.NotEmpty(MinHash.Bands(a).Intersect(MinHash.Bands(b)));
        }

        [Fact]
        public void Unrelated_text_is_dissimilar()
        {
            var other = "Managed vendor contracts and quarterly budgets for the facilities department, negotiated " +
                        "office leases across three cities and ran the annual employee engagement survey programme.";

            Assert.True(MinHash.Similarity(MinHash.Signature(Project), MinHash.Signature(other)) < 0.2m);
        }

        [Fact]
        public void Signatures_round_trip_through_bytes()
        {
            var sig = MinHash.Signature(Project);
            var bytes = MinHash.ToBytes(sig);

            Assert.Equal(512, bytes.Length);
            Assert.Equal(sig, MinHash.FromBytes(bytes));
        }

        [Fact]
        public void Too_little_text_is_not_fingerprinted()
        {
            Assert.Null(MinHash.Signature("Java developer"));
        }
    }
}
