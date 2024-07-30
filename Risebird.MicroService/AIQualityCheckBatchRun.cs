using System;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Enum;
using Risebird.Repository.Interface;
using System.Linq;
using System.Net.Http;


namespace Risebird.MicroService
{
    public class AIQualityCheckBatchRun
    {
        private readonly IConfiguration _configuration;
        private readonly IApplicationErrorLogger _applicationErrorLogger;
        //private readonly IAssistantServiceHelper _assistantServiceHelper;
        private readonly IDashboarRepository _dashboardRepository;
        //private readonly ICompositeViewEngine _viewEngine;
        //private readonly ITempDataProvider _tempDataProvider;
        //private readonly IHttpContextAccessor _contextAccessor;
        //private readonly IConverter _converter;
        //private readonly IAzureStorageHelper _azureStorageHelper;
        //private readonly IMeetingService _meetingService;

        public AIQualityCheckBatchRun(IConfiguration configuration,
            IApplicationErrorLogger applicationErrorLogger,
            IDashboarRepository dashboardRepository)
        {
            _configuration = configuration;
            _applicationErrorLogger = applicationErrorLogger;
            //_assistantServiceHelper = assistantServiceHelper;
            //_viewEngine = viewEngine;
            //_tempDataProvider = tempDataProvider;
            //_contextAccessor = contextAccessor;
            //_converter = converter;
            //_azureStorageHelper = azureStorageHelper;
            //_meetingService = meetingService;
            _dashboardRepository = dashboardRepository;
        }

        [FunctionName("AIQualityCheckBatchRun")]
        public async Task Run([TimerTrigger("0 */1 * * * *")] TimerInfo myTimer, ILogger log)//0 */5 * * * *
        {
            log.LogInformation($"AIQualityCheckBatchRun : C# Timer trigger function started executing  at: {DateTime.Now}");

            try
            {
                var qcPendingReports = await _dashboardRepository.GetQCPendingReportsForAIQC();
                if(qcPendingReports != null && qcPendingReports.Count()>0)
                {
                    foreach (var item in qcPendingReports)
                    {
                        try
                        {
                            using (HttpClient client = new HttpClient())
                            {
                                //var request = new HttpRequestMessage(HttpMethod.Get, $"https://portal.risebird.io/Interview/AIClientReport?interviewId={item.id}&irfId={item.irfIntId}&isOpenAIQC={true}&isBackgroudJobRequest={true}");
                                var request = new HttpRequestMessage(HttpMethod.Get, $"https://localhost:44329//Interview/AIClientReport?interviewId={item.id}&irfId={item.irfIntId}&isOpenAIQC={true}&isBackgroudJobRequest={true}");
//{
                                //    //Headers = { { "Authorization", $"Bearer {apiKey}" }, { "OpenAI-Beta", "assistants=v2" } }//{ "Content-Type", "application/json" },
                                //};


                                var response = await client.SendAsync(request);

                                response.EnsureSuccessStatusCode();
                                //var responseContent = await response.Content.ReadAsStringAsync();

                            }
                        }
                        catch (Exception ex)
                        {
                            log.LogInformation($"AIQualityCheckBatchRun : C# Timer trigger {ex.Message}");
                            await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                        }

                    }
                    
                        
                }
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }

            log.LogInformation($"AIQualityCheckBatchRun : C# Timer trigger function executed at: {DateTime.Now}");

        }

        //public async Task<bool> PerformQualityCheck(InterviewReport model)
        //{
        //    try
//    {
        //        // Render the partial view to a string
        //        var htmlContent = await RenderToStringAsync("AIClientReport", model);

        //        // Generate PDF from HTML content
        //        var pdfContent = GeneratePdf(htmlContent);
        //        string fileName = "AIClientReport_" + model.InterviewId.ToString() + "_" + DateTime.Now.ToString() + ".pdf";

//        private readonly string ContainerName;


        //        // Upload PDF to Azure Blob Storage
        //        string uploadedFileURL = string.Empty;
        //        using (MemoryStream stream = new MemoryStream(pdfContent))
//        {
        //            await _azureStorageHelper.InsertBlobCloudCompare(fileName, "aiqc", blobFile: stream, isOpenAiQc: true);
        //            var containerRef = _azureStorageHelper.GetCloudBlobContainer("aiqc");
        //            var blob = containerRef.GetBlockBlobReference(fileName);
        //            uploadedFileURL = blob.Uri.ToString();
        //        }

        //        var request = new ScreeningRequest
        //        {
        //            Type = ProfileNames.QualityController,
        //            ReportUrl = uploadedFileURL
        //        };
        //        var response = await _assistantServiceHelper.ScreenAsync(request);

        //        var responseInJson = JsonConvert.SerializeObject(response);
        //        //save the response in db
        //        bool saveOperationResult = true;
        //        if (!string.IsNullOrEmpty(responseInJson) && responseInJson != "null")
        //        {
        //            saveOperationResult = await _meetingService.SaveOpenAIReportQC(model.InterviewId, responseInJson);
//        }

        //        //if (saveOperationResult)
        //        //{
        //        //    ViewBag.SaveOperationResult = "Evaluation saved successfully.";
        //        //}
        //        //else
        //        //{
        //        //    ViewBag.SaveOperationResult = "Failed to save the evaluation.";
        //        //}

        //        return true;

        //    }
        //    catch (System.Exception ex)
        //    {

        //        await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
        //        return false;
        //    }
        //}
        //public byte[] GeneratePdf(string htmlContent)
        //{
        //    try
        //    {
        //        var globalSettings = new GlobalSettings
        //        {
        //            ColorMode = DinkToPdf.ColorMode.Color,
        //            Orientation = Orientation.Portrait,
        //            PaperSize = DinkToPdf.PaperKind.A4,
        //        };

        //        var objectSettings = new ObjectSettings
        //        {
        //            PagesCount = true,
        //            HtmlContent = htmlContent,
        //            WebSettings = { DefaultEncoding = "utf-8" },
        //            HeaderSettings = { FontName = "Arial", FontSize = 9, Right = "Page [page] of [toPage]", Line = true },
        //            FooterSettings = { FontName = "Arial", FontSize = 9, Line = true, Center = "Report Footer" }
        //        };

        //        var pdf = new HtmlToPdfDocument()
        //        {
        //            GlobalSettings = globalSettings,
        //            Objects = { objectSettings }
        //        };

        //        return _converter.Convert(pdf);
        //    }
        //    catch (Exception ex)
        //    {
        //        _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
        //        return null;
        //    }
        //}
        //public async Task<string> RenderToStringAsync(string viewName, object model)
        //{
        //    try
        //    {
        //        var httpContext = _contextAccessor.HttpContext;
        //        var actionContext = new ActionContext(httpContext, httpContext.GetRouteData(), new ActionDescriptor());

        //        using (var sw = new StringWriter())
        //        {
        //            var viewResult = _viewEngine.FindView(actionContext, viewName, false);

        //            if (viewResult.View == null)
        //            {
        //                throw new ArgumentNullException($"The view '{viewName}' was not found.");
//            }

        //            var viewDictionary = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary())
//            {
        //                Model = model
        //            };

        //            var tempData = new TempDataDictionary(actionContext.HttpContext, _tempDataProvider);

        //            var viewContext = new ViewContext(
        //                actionContext,
        //                viewResult.View,
        //                viewDictionary,
        //                tempData,
        //                sw,
        //                new HtmlHelperOptions()
        //            );

        //            await viewResult.View.RenderAsync(viewContext);
        //            return sw.ToString();
//        }
//    }
        //    catch (Exception ex)
        //    {
        //        await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
        //        return null;
        //    }
//}
    }
}
