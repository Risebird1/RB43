using System;
using System.Threading.Tasks;

namespace Risebird.CommonHelper.Interface
{
    public interface IPdfReaderHelper
    {
        Task<string> ReadPdfFromUrlAsync(string pdfUrl);
    }
}
