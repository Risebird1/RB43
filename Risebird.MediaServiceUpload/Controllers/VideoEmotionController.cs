using Microsoft.WindowsAzure.MediaServices.Client;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Drawing;
using Microsoft.Azure.CognitiveServices.Vision.Face;
using Microsoft.Azure.CognitiveServices.Vision.Face.Models;
using GleamTech.VideoUltimate;

namespace AzureMediaServiceMVC.Controllers
{
    public class VideoEmotionController : Controller
    {
        
        //// DEPRECATED method
        //private static readonly string mediaServiceAccountName = ConfigurationManager.AppSettings["MediaServiceAccountName"];
        //private static readonly string mediaServiceAccountKey = ConfigurationManager.AppSettings["MediaServiceAccountKey"];
        //private static readonly CloudMediaContext context = new CloudMediaContext(new MediaServicesCredentials(mediaServiceAccountName, mediaServiceAccountKey));

        //// Azure Active Directory (AAD)
        private static readonly string _AADTenantDomain = ConfigurationManager.AppSettings["AMSAADTenantDomain"];
        private static readonly string _AADTenantId = ConfigurationManager.AppSettings["AADTenantId"];
        private static readonly string _RESTAPIEndpoint = ConfigurationManager.AppSettings["AMSRESTAPIEndpoint"];
        private static readonly string _AMSClientId = ConfigurationManager.AppSettings["AMSClientId"];
        private static readonly string _AMSClientSecret = ConfigurationManager.AppSettings["AMSClientSecret"];
        private static readonly string _ARMEndpoint = ConfigurationManager.AppSettings["ARMEndpoint"];
        private static readonly string _AMSSubscriptionId = ConfigurationManager.AppSettings["AMSSubscriptionId"];
        private static readonly string _AMSResourceGroup = ConfigurationManager.AppSettings["AMSResourceGroup"];
        private static readonly string _AMSAccountName = ConfigurationManager.AppSettings["AMSAccountName"];


        private static readonly string _SentimentApiKey = ConfigurationManager.AppSettings["SentimentApiKey"];
        private static readonly string _SentimentEndpoint = ConfigurationManager.AppSettings["SentimentEndpoint"];
        //private static readonly AzureAdTokenCredentials azureAdTokenCredentials = new AzureAdTokenCredentials(mediaServiceADTenantDomain, new AzureAdClientSymmetricKey(mediaServiceADApplicationId, mediaServiceADApplicationSecret), AzureEnvironments.AzureCloudEnvironment);
        //private static readonly AzureAdTokenProvider azureAdTokenProvider = new AzureAdTokenProvider(azureAdTokenCredentials);
        //private static readonly CloudMediaContext context = new CloudMediaContext(new Uri(mediaServiceADRestApiEndpoint), azureAdTokenProvider);
        //private static CloudMediaContext _context = null;

        public bool ThumbnailCallback()
        {
            return false;
        }
        // GET: VideoEmotion
        public async Task<ActionResult> Index()
        {
            //GetEmotionAnalysis();
            //Bitmap bmp =  IMediaDet FrameGrabber.GetFrameFromVideo(_videopath, 0.2d);
            FaceClient faceClient = new FaceClient(new ApiKeyServiceClientCredentials(_SentimentApiKey))
            {
                Endpoint = _SentimentEndpoint
            };

            using (var videoFrameReader = new VideoFrameReader(@"E:\EmotionAnalysisFiles\WIN_20200819_14_24_35_Pro.mp4"))
            {
                foreach (var frame in videoFrameReader)
                {
                    using (frame)
                    {
                        //byte[] data = new byte[0];
                        using (MemoryStream ms = new MemoryStream())
                        {
                            Image.GetThumbnailImageAbort myCallback =
                                        new Image.GetThumbnailImageAbort(ThumbnailCallback);
                            var img = frame.GetThumbnailImage(1280, 720, myCallback, IntPtr.Zero);
                            img.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                            ms.Seek(0, 0);

                            var faces = (await faceClient.Face.DetectWithStreamAsync(ms,
                                        returnFaceId: false,
                                        returnFaceLandmarks: false,
                                        returnFaceAttributes: new FaceAttributeType[1] { FaceAttributeType.Emotion })).ToArray();
                            //var result = faceClient.Face.DetectWithStreamAsync(ms).Result;

                            //data = ms.ToArray();
                        }
                        //return data;

                        //Do something with current frame bitmap
                        //faceClient.Face.DetectWithStreamAsync((frame.GetThumbnailImage()));//)

                    }
                }
            }
            return View();
        }

        #region Sentiment Analysis

        #endregion


        //#region emotion analysis


        //public void GetEmotionAnalysis()
        //{
        //    AzureAdTokenCredentials tokenCredentials =
        //        new AzureAdTokenCredentials(_AADTenantDomain,
        //            new AzureAdClientSymmetricKey(_AMSClientId, _AMSClientSecret),
        //            AzureEnvironments.AzureCloudEnvironment);

        //    var tokenProvider = new AzureAdTokenProvider(tokenCredentials);

        //    _context = new CloudMediaContext(new Uri(_RESTAPIEndpoint), tokenProvider);

        //    // Run the FaceDetection job.
        //    var asset = RunFaceDetectionJob(@"E:\EmotionAnalysisFiles\WIN_20200819_14_24_35_Pro.mp4",
        //                                @"E:\EmotionAnalysisFiles\config.json");

        //    // Download the job output asset.
        //    DownloadAsset(asset, @"E:\EmotionAnalysisFiles\Output");
        //}

        //static IAsset RunFaceDetectionJob(string inputMediaFilePath, string configurationFile)
        //{
        //    // Create an asset and upload the input media file to storage.
        //    IAsset asset = CreateAssetAndUploadSingleFile(inputMediaFilePath,
        //        "My Face Detection Input Asset",
        //        AssetCreationOptions.None);

        //    // Declare a new job.
        //    IJob job = _context.Jobs.Create("My Face Detection Job");

        //    // Get a reference to Azure Media Face Detector.
        //    string MediaProcessorName = "Azure Media Face Detector";//"Azure Media Redactor";

        //    var processor = GetLatestMediaProcessorByName(MediaProcessorName);

        //    // Read configuration from the specified file.
        //    string configuration = System.IO.File.ReadAllText(configurationFile);

        //    // Create a task with the encoding details, using a string preset.
        //    ITask task = job.Tasks.AddNew("My Face Detection Task",
        //        processor,
        //        configuration,
        //        TaskOptions.None);

        //    // Specify the input asset.
        //    task.InputAssets.Add(asset);

        //    // Add an output asset to contain the results of the job.
        //    task.OutputAssets.AddNew("My Face Detection Output Asset", AssetCreationOptions.None);

        //    // Use the following event handler to check job progress.  
        //    job.StateChanged += new EventHandler<JobStateChangedEventArgs>(StateChanged);

        //    // Launch the job.
        //    job.Submit();

        //    // Check job execution and wait for job to finish.
        //    Task progressJobTask = job.GetExecutionProgressTask(CancellationToken.None);

        //    progressJobTask.Wait();

        //    // If job state is Error, the event handling
        //    // method for job progress should log errors.  Here we check
        //    // for error state and exit if needed.
        //    if (job.State == JobState.Error)
        //    {
        //        ErrorDetail error = job.Tasks.First().ErrorDetails.First();
        //        Console.WriteLine(string.Format("Error: {0}. {1}",
        //                                        error.Code,
        //                                        error.Message));
        //        return null;
        //    }

        //    return job.OutputMediaAssets[0];
        //}

        //static IAsset CreateAssetAndUploadSingleFile(string filePath, string assetName, AssetCreationOptions options)
        //{
        //    IAsset asset = _context.Assets.Create(assetName, options);

        //    var assetFile = asset.AssetFiles.Create(Path.GetFileName(filePath));
        //    assetFile.Upload(filePath);

        //    return asset;
        //}

        //static void DownloadAsset(IAsset asset, string outputDirectory)
        //{
        //    foreach (IAssetFile file in asset.AssetFiles)
        //    {
        //        file.Download(Path.Combine(outputDirectory, file.Name));
        //    }
        //}

        //static IMediaProcessor GetLatestMediaProcessorByName(string mediaProcessorName)
        //{
        //    var processor = _context.MediaProcessors
        //        .Where(p => p.Name == mediaProcessorName)
        //        .ToList()
        //        .OrderBy(p => new Version(p.Version))
        //        .LastOrDefault();

        //    //if (processor == null)
        //    //    throw new ArgumentException(string.Format("Unknown media processor",
        //    //                                               mediaProcessorName));

        //    return processor;
        //}

        //static private void StateChanged(object sender, JobStateChangedEventArgs e)
        //{
        //    Console.WriteLine("Job state changed event:");
        //    Console.WriteLine("  Previous state: " + e.PreviousState);
        //    Console.WriteLine("  Current state: " + e.CurrentState);

        //    switch (e.CurrentState)
        //    {
        //        case JobState.Finished:
        //            Console.WriteLine();
        //            Console.WriteLine("Job is finished.");
        //            Console.WriteLine();
        //            break;
        //        case JobState.Canceling:
        //        case JobState.Queued:
        //        case JobState.Scheduled:
        //        case JobState.Processing:
        //            Console.WriteLine("Please wait...\n");
        //            break;
        //        case JobState.Canceled:
        //        case JobState.Error:
        //            // Cast sender as a job.
        //            IJob job = (IJob)sender;
        //            // Display or log error details as needed.
        //            // LogJobStop(job.Id);
        //            break;
        //        default:
        //            break;
        //    }
        //}
        //#endregion
    }
}