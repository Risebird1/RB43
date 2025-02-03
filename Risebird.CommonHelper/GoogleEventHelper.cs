using Google.Apis.Auth.OAuth2;
using Google.Apis.Calendar.v3;
using Google.Apis.Calendar.v3.Data;
using Google.Apis.Drive.v3;
using Google.Apis.Services;
using Google.Cloud.Storage.V1;
using Microsoft.Extensions.Configuration;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Data;
using Risebird.Entities.Enum;
using System;
using System.Data;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Risebird.CommonHelper
{
    public class GoogleEventHelper : IGoogleEventHelper
    {

        private readonly IApplicationErrorLogger _applicationErrorLogger;
        private readonly IConfiguration _configuration;
        


        public GoogleEventHelper(IApplicationErrorLogger applicationErrorLogger, IConfiguration configuration)
        {
            _configuration = configuration;
            _applicationErrorLogger = applicationErrorLogger;
        }
        #region Meeting Event Action
        public async Task<Event> CreateCalendarEvent(CalendarEventDM model)
        {
            UserCredential credential;
            //       string[] scopes = new string[] {
            //CalendarService.Scope.Calendar,
            //CalendarService.Scope.CalendarReadonly};
            //credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(new ClientSecrets
            //{
            //    ClientId = clientId,
            //    ClientSecret = clientSecret
            //}, scopes, userName, CancellationToken.None, new FileDataStore("Daimto.GoogleCalendar.Auth.Store"));
            string[] scopes = { CalendarService.Scope.Calendar };
            using (var stream = new FileStream(Path.Combine(Directory.GetCurrentDirectory(), "Cre", "cre.json"), FileMode.Open, FileAccess.Read))
            {
                credential = GoogleWebAuthorizationBroker.AuthorizeAsync(
                    GoogleClientSecrets.FromStream(stream).Secrets,
                    scopes,
                    "user",
                    CancellationToken.None).Result;
            }

            CalendarService service = new CalendarService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = credential,
                ApplicationName = "Risebird_calendar",
            });
            //Event eventcalendar = new Event()
            //{
            //    Summary = "Test meeting Create",
            //    Description = "Meeting for testing Purpose",
            //    Start = new EventDateTime
            //    {
            //        DateTime = DateTime.Now.AddHours(20),
            //        TimeZone = "Asia/Kolkata"
            //    },
            //    End = new EventDateTime
            //    {
            //        DateTime = DateTime.Now.AddHours(21),
            //        TimeZone = "Asia/Kolkata"
            //    },
            //    Attendees = new List<EventAttendee>() { new EventAttendee { Email = "Prashantindoliya@hotmail.com" } },
            //    //Reminders = new Event.RemindersData
            //    //{
            //    //    UseDefault = true,
            //    //    Overrides = new List<EventReminder>()
            //    //    {
            //    //    new EventReminder{Method="email",Minutes=24*60},
            //    //    new EventReminder{Method="popup",Minutes=10}
            //    //    }
            //    //},
            //    ConferenceData = new ConferenceData()
            //    {
            //        CreateRequest = new CreateConferenceRequest
            //        {
            //            RequestId = Guid.NewGuid().ToString(),
            //            ConferenceSolutionKey = new ConferenceSolutionKey
            //            {
            //                Type = "hangoutsMeet"
            //            }
            //        }
            //    }
            //};

            Event eventcalendar = new Event()
            {
                Summary = model.Summary,
                Description = model.Description,
                Start = new EventDateTime
                {
                    DateTime = model.StartDate,
                    TimeZone = model.TimeZone
                },
                End = new EventDateTime
                {
                    DateTime = model.EndDate,
                    TimeZone = model.TimeZone
                },
                Attendees = model.Attendees,
                ConferenceData = new ConferenceData()
                {
                    CreateRequest = new CreateConferenceRequest
                    {
                        RequestId = Guid.NewGuid().ToString(),
                        ConferenceSolutionKey = new ConferenceSolutionKey
                        {
                            Type = "hangoutsMeet"
                        }
                    },
                    Signature = "Test Request"
                }
            };
            var eventrequest = service.Events.Insert(eventcalendar, "primary");
            eventrequest.ConferenceDataVersion = 1;
            eventrequest.SendNotifications = false;
            eventrequest.SendUpdates = EventsResource.InsertRequest.SendUpdatesEnum.None;
            var requestCreate = await eventrequest.ExecuteAsync();
            return requestCreate;
        }
        public async Task DeleteCalendarEvent(string eventid)
        {
            UserCredential credential;
            string[] scopes = { CalendarService.Scope.Calendar };
            using (var stream = new FileStream(Path.Combine(Directory.GetCurrentDirectory(), "Cre", "cre.json"), FileMode.Open, FileAccess.Read))
            {
                credential = GoogleWebAuthorizationBroker.AuthorizeAsync(
                    GoogleClientSecrets.FromStream(stream).Secrets,
                    scopes,
                    "user",
                    CancellationToken.None).Result;
            }

            CalendarService service = new CalendarService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = credential,
                ApplicationName = "Risebird_calendar",
            });
            var eventrequest = service.Events.Delete("primary", eventid);
            var requestDelete = await eventrequest.ExecuteAsync();
            var requestreponse = "";
        }
        #endregion

        #region Meeting Videos Actions
        public async Task<DataTable> CalendarEventVideo(string filename)
        {
            UserCredential credential;
            string[] scopes = new string[] { DriveService.Scope.Drive,
                               DriveService.Scope.DriveFile};

            using (var stream = new FileStream(Path.Combine(Directory.GetCurrentDirectory(), "Cre", "credrive.json"), FileMode.Open, FileAccess.Read))
            {
                credential = GoogleWebAuthorizationBroker.AuthorizeAsync(
                    GoogleClientSecrets.FromStream(stream).Secrets,
                scopes,
                "user",
                CancellationToken.None).Result;
            }

            DriveService service = new DriveService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = credential,
                ApplicationName = "Risebird_Drive",
            });

            service.HttpClient.Timeout = TimeSpan.FromMinutes(100);

            var filesrequest = service.Files.List();
            if (!string.IsNullOrEmpty(filename))
            {
                filesrequest.Q = "mimeType='video/mp4' and trashed = false and name contains '" + filename + "'";
            }
            else
            {
                filesrequest.Q = "mimeType='video/mp4' and trashed = false";
            }
            filesrequest.Fields = "nextPageToken, files(*)";
            filesrequest.PageToken = null;
            var filesList = await filesrequest.ExecuteAsync();
            DataTable dt = new DataTable();
            dt.Columns.Add("File Name");
            dt.Columns.Add("File Type");
            dt.Columns.Add("File ID");
            dt.Columns.Add("Video Link");
            dt.Columns.Add("Download Link");
            dt.Columns.Add("Video Duration");
            dt.Columns.Add("Thumbnail Link");
            if (filesList.Files.Count > 0)
            {
                foreach (Google.Apis.Drive.v3.Data.File file in filesList.Files)
                {
                    DataRow dr = dt.NewRow();
                    dr["File Name"] = file.Name;
                    dr["File Type"] = file.MimeType;
                    dr["File ID"] = file.Id;
                    dr["Video Link"] = file.WebViewLink;
                    dr["Download Link"] = file.WebContentLink;
                    dr["Video Duration"] = (file.VideoMediaMetadata.DurationMillis / 60000);
                    dr["Thumbnail Link"] = file.ThumbnailLink;
                    dt.Rows.Add(dr);
                }
            }
            return dt;
        }
        public async Task DeleteCalendarEventVideo(string fileid)
        {
            UserCredential credential;
            string[] scopes = new string[] { DriveService.Scope.Drive,
                               DriveService.Scope.DriveFile};

            //using (var stream = new FileStream(Path.Combine(Directory.GetCurrentDirectory(), "Cre", "credrive.json"), FileMode.Open, FileAccess.Read))
            //{
            //    string credPath = "tokendrive.json";
            //    credential = GoogleWebAuthorizationBroker.AuthorizeAsync(
            //        GoogleClientSecrets.Load(stream).Secrets,
            //                    scopes,
            //                    "user",
            //                    CancellationToken.None,
            //                    new FileDataStore(credPath, true)).Result;
            //}
            using (var stream = new FileStream(Path.Combine(Directory.GetCurrentDirectory(), "Cre", "credrive.json"), FileMode.Open, FileAccess.Read))
            {
                credential = GoogleWebAuthorizationBroker.AuthorizeAsync(
                    GoogleClientSecrets.FromStream(stream).Secrets,
                                scopes,
                                "user",
                                CancellationToken.None).Result;
            }

            DriveService service = new DriveService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = credential,
                ApplicationName = "Risebird_Drive",
            });

            var filesrequest = service.Files.Delete(fileid);
            var Deletefiles = await filesrequest.ExecuteAsync();
            var requestreponse = "";
        }
        #endregion

        public async Task<string> UploadToGoogleCloudStorage(string fileName, Stream fileStream, int meetingId)
        {
            try
            {
                string bucketName = "external-interview-recordings-risebird";
                
                // Path to your service account JSON file
                //string serviceAccountPath = "path/to/your/service-account.json";

                // Authenticate using the service account
                //GoogleCredential credential = GoogleCredential.FromFile(serviceAccountPath);

                var credential = GoogleCredential.FromJson(@"
                {
                  ""type"": ""service_account"",
                  ""project_id"": ""turing-gpt"",
                  ""private_key_id"": ""5e03e282a1293e79b031969a4954179727fc4cbd"",
                  ""private_key"": ""-----BEGIN PRIVATE KEY-----\nMIIEugIBADANBgkqhkiG9w0BAQEFAASCBKQwggSgAgEAAoIBAQC2AwPmw1SFdP+p\nIGif7UrRlyn27t75ZN7RdQyzPfLJrvMP3UFIjpLZOYpu0JW0Dco6iIJFgI+LT0jz\nk5vVK4gctt0EZ+N+RXq8COZp680QG+3xPKX6hcJw8OD/FBRvbv7JKgYd+2EEYsjB\nh07GEpEa1Mw6BO0JLs9axAwWWmtNSq90nk+wJX8eadbz8J/NWgLEgojzDbKJmze7\nOgo+cTTzU4dHnHTHA+YqXJwawTfkcUmC1Ri6l0QfwwlLkGYxdUD78ESwaKwJA3ky\nAqvJj86C5N/hXdcjJKTHBlTY/CoVpsB170pJ1jRK7dIBXm1MUf+hUHzehHDKYaYC\nSor+kweVAgMBAAECgf9cZIDHQeZ1FY2l92wYnjeBOXfW+hWLRtnU51kE+Tya897Q\nIpjckWrVU/KEBPyLmqUyBSXSzNF+zcB3avzIOSxAc26BVCLeApfE9C1X+OEHJ9EC\n4UBsQOfUtkFPzQYxEPZFDxw8leTOSPVu/us63bgH8vFvpt+4sTsjRc8+fWq9xHTl\niBI1t8kexFpthxIal6C0cF/Qa9VWOKmOQpaWLH0bDZ6xY5SzMEjJdy1BtZGMpUI7\nQ8VNkUAVtW9YZrUJNFx20TZetAOlziVV/iLKI7slAcQ0hLDXem4ybIJ7imzYH9Ff\nWQBk0rkUwt6tMZLC43Fbo+psQR+bB439B1T1P+ECgYEA42rPvTx0UI6TMjN1aLcQ\niqPGY5yn8+MWzi0DobymfbG1iu2obJwexWUgtVPzYj7sqBnPgG1YXILZu0uqS8/8\nLKNZzgb53++UtDO8d7ljKtjr1NSy3G29DovPHfAvpcaFhj8RME6GovN2isTtWUXr\nxGMVhfvbkT5pha8y3iG/i0kCgYEAzONGhXQDjbhOcN4r52uQooGZqBNCUseeBf8H\nIJPNebsn3DThw1NPoVWgiMQe2mPmeWi9u/SuqjKdVC2f1PJMNSy1rpPV+pfdflwX\nBubTV07W4pN4h0Xo+UZiYX6agvznc311tS3X08Ib6bEzVHcvI8ZeWnpi2UGPZzUl\nBYlpbe0CgYAkMxxeQwFA0XEczJQj1wALEYbC1GznpX47026AkEbwOxlqw+nLYA6F\n3VMXigVz3hBNsc4V41HtZ+bAqYqyBqldcaMZ6IJd4wUIqjOV2yyt5VmkxzaxX8Z4\nSkjtuw3alX8rn15dEiLcTWTRMgeV7ktZN2x1p2GL/SNO5FR0h2q2AQKBgGMZrxuP\noMRkmDRJkezOf1fK3dqI7jSB7vIxh23/b4xotP/y6BwE6Og9NXfRfKbfWTPyMIt/\n7xfNgYGcy8lBJRE3Pe1h/kbGO9EJk+23qhN5w8A12j971hL0M1O629ttnjmjPlLT\nCKiZI80l85DwUPG52htjRPqWllkyQGTsMP2FAoGAWgG6yPMBY7RikMEwhWdDlxox\nyTr6IymLdqb7af8P8ea/9ah2AI7qRn0feHiXvfG1MyPjZSPRRXOaZMIMk6c2ZaY7\nSpMIw/1t5cs8vA96UjGN+e6ru04reXP1o880yaGEiwjLHgp/yEQI07o75E1NPg+L\noyi7UsVMq0rVQ7l3rbY=\n-----END PRIVATE KEY-----\n"",
                  ""client_email"": ""risebird@turing-gpt.iam.gserviceaccount.com"",
                  ""client_id"": ""104972780963002769299"",
                  ""auth_uri"": ""https://accounts.google.com/o/oauth2/auth"",
                  ""token_uri"": ""https://oauth2.googleapis.com/token"",
                  ""auth_provider_x509_cert_url"": ""https://www.googleapis.com/oauth2/v1/certs"",
                  ""client_x509_cert_url"": ""https://www.googleapis.com/robot/v1/metadata/x509/risebird%40turing-gpt.iam.gserviceaccount.com"",
                  ""universe_domain"": ""googleapis.com""
                }");

                StorageClient storageClient = StorageClient.Create(credential);

                //string bucketName = "your-bucket-name";
                string folderName = meetingId.ToString(); // Folder name
                //string filePath = "path/to/local/video.mp4";
                string objectName = $"{folderName}/{fileName}"; // Store file in the "videos" folder

                // 1. Create a "folder" by uploading an empty object
                if(await CreateFolderInBucket(storageClient, bucketName, folderName))
                {
                    var response = await storageClient.UploadObjectAsync(bucketName, objectName, null, fileStream);
                }
                

                // Upload the file to Google Cloud Storage
                //Console.WriteLine($"Uploading {objectName} to bucket {bucketName}...");
                //await storageClient.UploadObjectAsync(bucketName, objectName, null, fileStream);

                // Construct the uploaded file URL (replace with your storage URL if different)
                string uploadedUrl = $"https://storage.googleapis.com/{bucketName}/{objectName}";
                //Console.WriteLine($"Uploaded to: {uploadedUrl}");

                return uploadedUrl;
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }
        private async Task<bool> CreateFolderInBucket(StorageClient storageClient, string bucketName, string folderName)
        {
            try
            {
                string folderObjectName = $"{folderName}/"; // Folder simulation
                using (var emptyStream = new MemoryStream())
                {
                    _ = await storageClient.UploadObjectAsync(bucketName, folderObjectName, null, emptyStream);

                }
                return true;
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return false;
            }
        }
    }
}
