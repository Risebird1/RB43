using System;
using System.Threading.Tasks;

namespace Risebird.CommonHelper.Interface
{
    public interface IPdfReaderHelper
    {
        Task<string> ReadPdfFromUrlAsync(string pdfUrl);

        /// <summary>
        /// Text a reader of the PDF would not see: white or near-white fill, a font under 3pt, or the
        /// invisible text-rendering mode. Used to spot instructions hidden for an AI screener. Returns
        /// the hidden runs (empty when none, or when the file cannot be read).
        /// </summary>
        Task<System.Collections.Generic.IReadOnlyList<string>> FindHiddenTextAsync(string pdfUrl);
    }
}
