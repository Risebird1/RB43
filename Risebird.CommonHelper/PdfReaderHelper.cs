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
        
        private readonly IHttpClientFactory _httpClientFactory;

        public PdfReaderHelper(IApplicationErrorLogger applicationErrorLogger, IHttpClientFactory httpClientFactory) //, IConfiguration configuration
        {
            _httpClientFactory = httpClientFactory;
            //_configuration = configuration;
            _applicationErrorLogger = applicationErrorLogger;
        }

        public async Task<string> ReadPdfFromUrlAsync(string pdfUrl)
        {

            try
            {
                using (HttpClient client = _httpClientFactory.CreateClient())
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

        public async Task<System.Collections.Generic.IReadOnlyList<string>> FindHiddenTextAsync(string pdfUrl)
        {
            var runs = new System.Collections.Generic.List<string>();
            try
            {
                using (HttpClient client = _httpClientFactory.CreateClient())
                {
                    byte[] pdfBytes = await client.GetByteArrayAsync(pdfUrl);
                    using (var pdfDocument = UglyToad.PdfPig.PdfDocument.Open(pdfBytes))
                    {
                        foreach (var page in pdfDocument.GetPages())
                        {
                            var current = new StringBuilder();
                            UglyToad.PdfPig.Content.Letter previous = null;
                            foreach (var letter in page.Letters)
                            {
                                if (IsHidden(letter))
                                {
                                    // PdfPig letters carry no spaces; a horizontal gap or a new line is a word break.
                                    if (previous != null && current.Length > 0
                                        && (letter.StartBaseLine.X - previous.EndBaseLine.X > Math.Max(1.0, letter.Width * 0.25)
                                            || Math.Abs(letter.StartBaseLine.Y - previous.StartBaseLine.Y) > 1.0))
                                    {
                                        current.Append(' ');
                                    }
                                    current.Append(letter.Value);
                                    previous = letter;
                                }
                                else if (current.Length > 0)
                                {
                                    AddRun(runs, current);
                                }
                            }
                            AddRun(runs, current);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return runs;
        }

        private static bool IsHidden(UglyToad.PdfPig.Content.Letter letter)
        {
            if (string.IsNullOrWhiteSpace(letter.Value))
            {
                return false;
            }
            if (letter.RenderingMode == UglyToad.PdfPig.Core.TextRenderingMode.Neither)
            {
                return true;
            }
            if (letter.PointSize > 0 && letter.PointSize < 3)
            {
                return true;
            }
            try
            {
                var (r, g, b) = (letter.FillColor ?? letter.Color).ToRGBValues();
                return r > 0.95 && g > 0.95 && b > 0.95;
            }
            catch
            {
                return false;
            }
        }

        private static void AddRun(System.Collections.Generic.List<string> runs, StringBuilder current)
        {
            // A single stray glyph (a bullet drawn white, a thin rule) is not hidden text.
            var text = current.ToString().Trim();
            if (text.Length >= 12)
            {
                runs.Add(text.Length > 300 ? text.Substring(0, 300) : text);
            }
            current.Clear();
        }
    }
}
