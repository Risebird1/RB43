using Xunit;

namespace Risebird.CommonHelper.Tests.CommonHelperLogic
{
    /// <summary>
    /// Inputs are real entries from the SendGrid bounce list (Oct 2026): resume text that was saved and mailed as the
    /// candidate's address.
    /// </summary>
    public class CandidateEmailNormalizerTests
    {
        [Theory]
        // Separators and phone numbers in front of the address, raw or URL-encoded.
        [InlineData("|amansambhariya9@gmail.com", "amansambhariya9@gmail.com")]
        [InlineData("%7c%20kingejaz143@gmail.com", "kingejaz143@gmail.com")]
        [InlineData("7775812827|%20riteshobhoyar@gmail.com", "riteshobhoyar@gmail.com")]
        [InlineData("karnataka%7c8095971568%7cprajnamohanty2010@gmail.com", "prajnamohanty2010@gmail.com")]
        [InlineData("+91-9860173074%20%7c%20kumarsalunkhe12@gmail.com", "kumarsalunkhe12@gmail.com")]
        [InlineData("email%20id%20-%20joshi.aditya303@gmail.com", "joshi.aditya303@gmail.com")]
        [InlineData("e-mail-%2520%2520akashshelke01@gmail.com", "akashshelke01@gmail.com")]
        [InlineData("id%3anagurbabu2000@gmail.com", "nagurbabu2000@gmail.com")]
        [InlineData("email_id%20%e2%80%93%20smlangoti@gmail.com", "smlangoti@gmail.com")]
        [InlineData("%23%20shegokar2020@gmail.com", "shegokar2020@gmail.com")]
        [InlineData("=vivz1989@gmail.com", "vivz1989@gmail.com")]
        [InlineData("`akshaya.p1226@gmail.com", "akshaya.p1226@gmail.com")]
        [InlineData("/madana.kanigalla@gmail.com", "madana.kanigalla@gmail.com")]
        [InlineData("•jeetjangirs@gmail.com", "jeetjangirs@gmail.com")]
        [InlineData("mailto:Someone.Name@Gmail.com", "someone.name@gmail.com")]
        // Invisible characters: NBSP, zero-width space, word joiner.
        [InlineData("%c2%a0swapnil.mulchandani@gmail.com", "swapnil.mulchandani@gmail.com")]
        [InlineData("​sathishreddy2609@gmail.com​", "sathishreddy2609@gmail.com")]
        [InlineData("⁠sathishreddy2609@gmail.com", "sathishreddy2609@gmail.com")]
        [InlineData("  sneha.sharma111023@gmail.com ", "sneha.sharma111023@gmail.com")]
        // PDF ligatures.
        [InlineData("anushathotaoﬃcial@gmail.com", "anushathotaofficial@gmail.com")]
        [InlineData("vaibhavchaﬂe09@gmail.com", "vaibhavchafle09@gmail.com")]
        [InlineData("â•ﬁ%20%20nilambari596@gmail.com", "nilambari596@gmail.com")]
        // Display-name form.
        [InlineData("antara deb <antara.deb@xponentiums.com>", "antara.deb@xponentiums.com")]
        [InlineData("Antara Deb &lt;antara.deb@xponentiums.com&gt;", "antara.deb@xponentiums.com")]
        // The next word of the resume glued to the domain.
        [InlineData("kapoor.raunak91@gmail.comprofessional", "kapoor.raunak91@gmail.com")]
        [InlineData("ykumar945@gmail.comlinkedin", "ykumar945@gmail.com")]
        [InlineData("saurabhpantse@gmail.commob", "saurabhpantse@gmail.com")]
        [InlineData("ankitkorade123@gmail.com%20%7c", "ankitkorade123@gmail.com")]
        [InlineData("someone@yahoo.co.inphone", "someone@yahoo.co.in")]
        [InlineData("someone@acme.comcontact", "someone@acme.com")]
        [InlineData("someone@acme.co.inprofessional", "someone@acme.co.in")]
        // A mobile number (and an "email" label) glued in front.
        [InlineData("+91-9711126040garima03.1990@gmail.com", "garima03.1990@gmail.com")]
        [InlineData("+91-90333-83308bavanibharadava@gmail.com", "bavanibharadava@gmail.com")]
        [InlineData("918349667240sahus8349@gmail.com", "sahus8349@gmail.com")]
        [InlineData("+918310076701.sahooamruta71@gmail.com", "sahooamruta71@gmail.com")]
        [InlineData("+919575968373emailsharma.shivam1607@gmail.com", "sharma.shivam1607@gmail.com")]
        [InlineData("9876543210rahul@gmail.com", "9876543210rahul@gmail.com")]
        [InlineData("9876543210@gmail.com", "9876543210@gmail.com")]
        // Junk at the very start of the value.
        [InlineData("'chandra18c@gmail.com", "chandra18c@gmail.com")]
        [InlineData("-tauseefahmadcs@gmail.com", "tauseefahmadcs@gmail.com")]
        [InlineData("--------rout.satya550@gmail.com", "rout.satya550@gmail.com")]
        // gmail without its ".com".
        [InlineData("pravinrathod592@gmail", "pravinrathod592@gmail.com")]
        [InlineData("harinod95@gmailcom", "harinod95@gmail.com")]
        [InlineData("mayekar.smita@hotmailcom", "mayekar.smita@hotmail.com")]
        [InlineData("anilkumar.k.uid@outlookcom", "anilkumar.k.uid@outlook.com")]
        // Already clean addresses stay as they are (lower-cased).
        [InlineData("Rameshssc109@Gmail.com", "rameshssc109@gmail.com")]
        [InlineData("first.last+tag@sub.company.co.in", "first.last+tag@sub.company.co.in")]
        [InlineData("someone@company.info", "someone@company.info")]
        [InlineData("someone@company.community", "someone@company.community")]
        [InlineData("someone@gmail.com.", "someone@gmail.com")]
        public void Cleans_the_address_out_of_resume_text(string raw, string expected)
        {
            Assert.Equal(expected, CandidateEmailNormalizer.Normalize(raw));
        }

        [Theory]
        // Masked or damaged: any "fix" would be a guess at someone else's mailbox.
        [InlineData("ru****@gmail.com")]
        [InlineData("talentcmlzzwjpcmquaw8=@naukri.com")]
        [InlineData("bobbykboseo?ce@gmail.com")]
        [InlineData("v?rakhisharma094@gmail.com")]
        [InlineData("amitrajupōg@gmail.com")]
        [InlineData("sravanthikammarié30@gmail.com")]
        [InlineData("y%27vivekmangukiya11@gmail.com")]
        [InlineData("pa!thrakandasamy27@gmail.com")]
        [InlineData("75.76%prakash.athani@gmail.com")]
        [InlineData("ashesh3247@hotmail")]
        [InlineData("귗귘귙귚귛귝규venkatesh.adapa97@gmail.com")]
        // Not an address at all, or two of them.
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("not an email")]
        [InlineData("someone@localhost")]
        [InlineData("a@x.com / b@y.com")]
        public void Refuses_what_cannot_be_trusted(string raw)
        {
            Assert.Null(CandidateEmailNormalizer.Normalize(raw));
        }

        [Fact]
        public void ExtractFirst_takes_the_first_address_of_a_whole_resume()
        {
            const string resume = "RAVI KUMAR Mob:9876543210|ravi.kumar@gmail.comLinkedIn linkedin.com/in/ravi References: hr@acme.com";
            Assert.Equal("ravi.kumar@gmail.com", CandidateEmailNormalizer.ExtractFirst(resume));
        }

        [Fact]
        public void IsValid_only_for_the_stored_form()
        {
            Assert.True(CandidateEmailNormalizer.IsValid("ravi.kumar@gmail.com"));
            Assert.False(CandidateEmailNormalizer.IsValid("Ravi.Kumar@gmail.com"));
            Assert.False(CandidateEmailNormalizer.IsValid("|ravi.kumar@gmail.com"));
        }
    }
}
