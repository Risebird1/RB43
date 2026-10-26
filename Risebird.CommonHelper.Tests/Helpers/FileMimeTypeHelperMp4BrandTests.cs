using System.Text;
using Risebird.CommonHelper;

namespace Risebird.CommonHelper.Tests.Helpers
{
    /// <summary>
    /// iPhone answers were rejected with "Error saving video Response" because the MIME sniffer
    /// reported Safari's fragmented MP4 as text/plain.
    /// </summary>
    public class FileMimeTypeHelperMp4BrandTests
    {
        private static byte[] Ftyp(string brand)
        {
            var bytes = new byte[64];
            bytes[3] = 0x20;
            Encoding.ASCII.GetBytes("ftyp" + brand).CopyTo(bytes, 4);
            return bytes;
        }

        [Theory]
        [InlineData("iso5")]
        [InlineData("iso6")]
        [InlineData("mp41")]
        [InlineData("avc1")]
        [InlineData("M4V ")]
        [InlineData("isom")]
        [InlineData("mp42")]
        public void Mp4_brands_from_mobile_recorders_are_video_mp4(string brand)
        {
            Assert.Equal("video/mp4", new FileMimeTypeHelper().GetFileMimeTypeFromByteArray(Ftyp(brand)));
        }

        [Fact]
        public void QuickTime_is_still_quicktime()
        {
            Assert.Equal("video/quicktime", new FileMimeTypeHelper().GetFileMimeTypeFromByteArray(Ftyp("qt  ")));
        }

        [Theory]
        [InlineData("heic")]
        [InlineData("3gp4")]
        public void Other_iso_media_brands_are_not_forced_to_mp4(string brand)
        {
            Assert.Null(FileMimeTypeHelper.Mp4BrandMimeType(Ftyp(brand)));
        }
    }
}
