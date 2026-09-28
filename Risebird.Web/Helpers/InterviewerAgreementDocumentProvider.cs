using iTextSharp.text;
using iTextSharp.text.pdf;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Options;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Data;
using Risebird.Entities.ViewModel;
using System;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace Risebird.Web.Helpers
{
    /// <summary>
    /// Loads the agreement PDF and its "&lt;name&gt;.layout.json" from the local AgreementDocuments
    /// folder or from Azure Blob (ZohoSign:DocumentSource), then clears the placeholder areas and
    /// draws the layout's static text so the Zoho Sign fields have room.
    /// </summary>
    public class InterviewerAgreementDocumentProvider : IInterviewerAgreementDocumentProvider
    {
        private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(10);
        private static readonly object CacheLock = new object();
        private static (string Key, DateTime LoadedAt, AgreementDocument Document) _cache;

        private readonly ZohoSignOptions _options;
        private readonly IWebHostEnvironment _environment;
        private readonly IAzureStorageHelper _azureStorageHelper;

        public InterviewerAgreementDocumentProvider(IOptions<ZohoSignOptions> options, IWebHostEnvironment environment, IAzureStorageHelper azureStorageHelper)
        {
            _options = options.Value;
            _environment = environment;
            _azureStorageHelper = azureStorageHelper;
        }

        public async Task<AgreementDocument> GetDocumentAsync()
        {
            var key = $"{_options.DocumentSource}|{_options.DocumentBlobContainer}|{_options.LocalDocumentFolder}|{_options.DocumentName}|{_options.ResolvedLayoutName}";
            lock (CacheLock)
            {
                if (_cache.Key == key && DateTime.UtcNow - _cache.LoadedAt < CacheDuration)
                    return _cache.Document;
            }

            var pdf = await ReadFileAsync(_options.DocumentName, allowLocalFallback: false);
            var layoutJson = await ReadFileAsync(_options.ResolvedLayoutName, allowLocalFallback: true);
            var layout = JsonSerializer.Deserialize<AgreementDocumentLayout>(layoutJson,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true, ReadCommentHandling = JsonCommentHandling.Skip, AllowTrailingCommas = true })
                ?? throw new InvalidOperationException($"Agreement layout for {_options.DocumentName} is empty.");

            var document = Prepare(pdf, layout);
            document.FileName = _options.DocumentName;
            document.SourceSha256 = Convert.ToHexString(SHA256.HashData(pdf));
            lock (CacheLock)
            {
                _cache = (key, DateTime.UtcNow, document);
            }
            return document;
        }

        public byte[] StampPortalSignature(AgreementDocument document, PortalSignatureStamp stamp)
        {
            var reader = new PdfReader(document.Pdf);
            try
            {
                using var output = new MemoryStream();
                var stamper = new PdfStamper(reader, output);
                var textFont = BaseFont.CreateFont(BaseFont.TIMES_ROMAN, BaseFont.CP1252, BaseFont.NOT_EMBEDDED);
                var auditFont = BaseFont.CreateFont(BaseFont.HELVETICA, BaseFont.CP1252, BaseFont.NOT_EMBEDDED);
                var auditBold = BaseFont.CreateFont(BaseFont.HELVETICA_BOLD, BaseFont.CP1252, BaseFont.NOT_EMBEDDED);
                var signedDate = stamp.SignedOn.ToString("dd MMM yyyy", CultureInfo.InvariantCulture);

                // Company (countersignature) fields are not part of portal signing.
                foreach (var field in document.Layout.Fields.Where(f => string.Equals(f.Signer, "Interviewer", StringComparison.OrdinalIgnoreCase)))
                {
                    var pageHeight = document.PageSizes[field.Page - 1].Height;
                    var canvas = stamper.GetOverContent(field.Page);
                    var bottom = pageHeight - field.Y - field.Height;

                    if (string.Equals(field.Type, "Signature", StringComparison.OrdinalIgnoreCase))
                    {
                        var image = Image.GetInstance(stamp.SignatureImage);
                        image.ScaleToFit(field.Width, field.Height);
                        image.SetAbsolutePosition(field.X, bottom);
                        canvas.AddImage(image);
                        continue;
                    }

                    var value = field.Type?.ToLowerInvariant() switch
                    {
                        "date" => signedDate,
                        "name" => stamp.FullName,
                        "textfield" => LabelValue(field.Label, stamp),
                        _ => null
                    };
                    if (!string.IsNullOrEmpty(value))
                        DrawFittedText(canvas, textFont, value, field.X + 1, bottom, field.Width - 2, field.Height, field.FontSize);
                }

                DrawAudit(stamper, reader, document, stamp, auditFont, auditBold);

                stamper.Close();
                return output.ToArray();
            }
            finally
            {
                reader.Close();
            }
        }

        // Textfields are matched to the interviewer's input by their layout label.
        private string LabelValue(string label, PortalSignatureStamp stamp)
        {
            if (string.Equals(label, _options.NameFieldLabel, StringComparison.OrdinalIgnoreCase)) return stamp.FullName;
            if (string.Equals(label, _options.FatherNameFieldLabel, StringComparison.OrdinalIgnoreCase)) return stamp.FatherName;
            if (string.Equals(label, _options.AddressFieldLabel, StringComparison.OrdinalIgnoreCase)) return stamp.Address;
            return null;
        }

        // Fits the value in the field box: one line, shrinking the font down to 7.5pt; if it still does not
        // fit, two lines (split at the comma / space nearest the middle) shrinking down to 5.5pt.
        private static void DrawFittedText(PdfContentByte canvas, BaseFont font, string value, float x, float bottom, float maxWidth, float boxHeight, float fontSize)
        {
            var size = fontSize;
            while (size > 7.5f && font.GetWidthPoint(value, size) > maxWidth)
                size -= 0.5f;
            if (font.GetWidthPoint(value, size) <= maxWidth || boxHeight < 13)
            {
                while (size > 6 && font.GetWidthPoint(value, size) > maxWidth)
                    size -= 0.5f;
                ShowLine(canvas, font, value, x, bottom + 3.5f, size);
                return;
            }

            var (first, second) = SplitNearMiddle(value);
            size = Math.Min(fontSize, (boxHeight - 2.5f) / 2.1f);
            while (size > 5.5f && Math.Max(font.GetWidthPoint(first, size), font.GetWidthPoint(second, size)) > maxWidth)
                size -= 0.25f;
            ShowLine(canvas, font, first, x, bottom + 2 + size * 1.1f, size);
            ShowLine(canvas, font, second, x, bottom + 2, size);
        }

        private static (string First, string Second) SplitNearMiddle(string value)
        {
            var middle = value.Length / 2;
            int Best(char separator)
            {
                int best = -1;
                for (var i = value.IndexOf(separator); i >= 0; i = value.IndexOf(separator, i + 1))
                    if (best < 0 || Math.Abs(i - middle) < Math.Abs(best - middle)) best = i;
                return best;
            }
            var cut = Best(',');
            if (cut < 0 || Math.Abs(cut - middle) > value.Length / 4) cut = Best(' ');
            if (cut <= 0) cut = middle;
            return (value.Substring(0, cut + 1).Trim(), value.Substring(cut + 1).Trim());
        }

        private static void ShowLine(PdfContentByte canvas, BaseFont font, string value, float x, float baseline, float size)
        {
            canvas.BeginText();
            canvas.SetColorFill(BaseColor.BLACK);
            canvas.SetFontAndSize(font, size);
            canvas.SetTextMatrix(x, baseline);
            canvas.ShowText(value);
            canvas.EndText();
        }

        // Electronic-signature evidence: a block on the audit page and a reference line on every page.
        private void DrawAudit(PdfStamper stamper, PdfReader reader, AgreementDocument document, PortalSignatureStamp stamp, BaseFont font, BaseFont bold)
        {
            // Fingerprint of the source agreement (stable; matches the file in blob / the folder).
            var fingerprint = (document.SourceSha256 ?? Convert.ToHexString(SHA256.HashData(document.Pdf))).Substring(0, 16);
            var signedAt = stamp.SignedOn.ToString("dd MMM yyyy, hh:mm:ss tt", CultureInfo.InvariantCulture);
            var lines = new[]
            {
                $"Signer: {stamp.FullName}  |  Email: {stamp.SignerEmail}  |  Interviewer Profile ID: {stamp.InterviewerProfileId}",
                $"Signed on: {signedAt} IST  |  IP address: {stamp.IpAddress}",
                $"Reference: {stamp.Reference}  |  Document: {document.FileName} (SHA-256 {fingerprint}...)",
                "The signer read this agreement in the Risebird portal, entered the details above and accepted it with the signature shown."
            };

            var audit = document.Layout.Audit ?? new AgreementLayoutAudit();
            var auditPage = audit.Page <= 0 ? reader.NumberOfPages : audit.Page;
            var pageHeight = document.PageSizes[auditPage - 1].Height;
            var canvas = stamper.GetOverContent(auditPage);
            var top = pageHeight - audit.Y;
            var width = document.PageSizes[auditPage - 1].Width - audit.X * 2;

            canvas.SaveState();
            canvas.SetColorStroke(new BaseColor(150, 150, 150));
            canvas.SetLineWidth(0.6f);
            canvas.Rectangle(audit.X - 6, top - 14 - lines.Length * 11 - 4, width + 12, 14 + lines.Length * 11 + 10);
            canvas.Stroke();
            canvas.RestoreState();

            canvas.BeginText();
            canvas.SetColorFill(BaseColor.BLACK);
            canvas.SetFontAndSize(bold, 8.5f);
            canvas.SetTextMatrix(audit.X, top - 10);
            canvas.ShowText("Electronically signed via Risebird Portal");
            canvas.SetFontAndSize(font, 7.5f);
            for (var i = 0; i < lines.Length; i++)
            {
                canvas.SetTextMatrix(audit.X, top - 23 - i * 11);
                canvas.ShowText(lines[i]);
            }
            canvas.EndText();

            var footer = $"Electronically signed by {stamp.FullName} on {signedAt} IST  |  Ref: {stamp.Reference}";
            for (var page = 1; page <= reader.NumberOfPages; page++)
            {
                var size = document.PageSizes[page - 1];
                var pageCanvas = stamper.GetOverContent(page);
                pageCanvas.BeginText();
                pageCanvas.SetColorFill(new BaseColor(110, 110, 110));
                pageCanvas.SetFontAndSize(font, 6.5f);
                pageCanvas.ShowTextAligned(Element.ALIGN_CENTER, footer, size.Width / 2, 10, 0);
                pageCanvas.EndText();
            }
        }

        // Blob source: the PDF must be in the container. The layout may be uploaded next to it; if it is
        // not there, the layout in the local AgreementDocuments folder is used.
        private async Task<byte[]> ReadFileAsync(string fileName, bool allowLocalFallback)
        {
            if (string.Equals(_options.DocumentSource, "Blob", StringComparison.OrdinalIgnoreCase))
            {
                var blob = _azureStorageHelper.GetBlobDataAsBlob(_options.DocumentBlobContainer, fileName);
                if (blob != null && await blob.ExistsAsync())
                {
                    using var ms = new MemoryStream();
                    await blob.DownloadToStreamAsync(ms);
                    return ms.ToArray();
                }
                if (!allowLocalFallback)
                    throw new FileNotFoundException($"Agreement file '{fileName}' not found in blob container '{_options.DocumentBlobContainer}'.");
            }

            var path = Path.Combine(_environment.ContentRootPath, _options.LocalDocumentFolder, fileName);
            if (!File.Exists(path))
                throw new FileNotFoundException($"Agreement file not found: {path}");
            return await File.ReadAllBytesAsync(path);
        }

        private static AgreementDocument Prepare(byte[] pdf, AgreementDocumentLayout layout)
        {
            var document = new AgreementDocument { Layout = layout };
            var reader = new PdfReader(pdf);
            try
            {
                for (var page = 1; page <= reader.NumberOfPages; page++)
                {
                    var size = reader.GetPageSizeWithRotation(page);
                    document.PageSizes.Add((size.Width, size.Height));
                }

                using var output = new MemoryStream();
                var stamper = new PdfStamper(reader, output);
                var font = BaseFont.CreateFont(BaseFont.TIMES_ROMAN, BaseFont.CP1252, BaseFont.NOT_EMBEDDED);

                foreach (var rect in layout.Clear)
                {
                    var pageHeight = document.PageSizes[rect.Page - 1].Height;
                    var canvas = stamper.GetOverContent(rect.Page);
                    canvas.SaveState();
                    canvas.SetColorFill(BaseColor.WHITE);
                    canvas.Rectangle(rect.X, pageHeight - rect.Y - rect.Height, rect.Width, rect.Height);
                    canvas.Fill();
                    canvas.RestoreState();
                }

                foreach (var text in layout.Text)
                {
                    var pageHeight = document.PageSizes[text.Page - 1].Height;
                    var canvas = stamper.GetOverContent(text.Page);
                    canvas.SaveState();
                    canvas.BeginText();
                    canvas.SetColorFill(BaseColor.BLACK);
                    canvas.SetFontAndSize(font, text.FontSize);
                    canvas.SetTextMatrix(text.X, pageHeight - text.Baseline);
                    canvas.ShowText(text.Value);
                    canvas.EndText();
                    canvas.RestoreState();
                }

                stamper.Close();
                document.Pdf = output.ToArray();
            }
            finally
            {
                reader.Close();
            }
            return document;
        }
    }
}
