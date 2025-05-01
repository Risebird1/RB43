using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Risebird.Business.Interface;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Enum;
using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Risebird.CommonHelper
{
    public class PdfReaderHelper : IPdfReaderHelper
    {
        private readonly IApplicationErrorLogger _applicationErrorLogger;
        //private readonly IConfiguration _configuration;
        
        public PdfReaderHelper(IApplicationErrorLogger applicationErrorLogger) //, IConfiguration configuration
        {
            //_configuration = configuration;
            _applicationErrorLogger = applicationErrorLogger;
        }

        public async Task<string> ReadPdfFromUrlAsync(string pdfUrl)
        {

            try
            {
                using (HttpClient client = new HttpClient())
                {
                    byte[] pdfBytes = await client.GetByteArrayAsync(pdfUrl);

                    using (MemoryStream stream = new MemoryStream(pdfBytes))
                    {
                        using (var pdfDocument = UglyToad.PdfPig.PdfDocument.Open(stream))
                        {
                            string text = "";
                            foreach (var page in pdfDocument.GetPages())
                            {
                                text += page.Text + "\n";
                            }
                            return text;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }
    }
}
