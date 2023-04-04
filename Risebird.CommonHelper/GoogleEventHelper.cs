using Google.Apis.Auth.OAuth2;
using Google.Apis.Calendar.v3.Data;
using Google.Apis.Calendar.v3;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Risebird.CommonHelper.Interface;
using Google.Apis.Drive.v3;
using System.Data;
using Risebird.Entities.Data;

namespace Risebird.CommonHelper
{
    public class GoogleEventHelper : IGoogleEventHelper
    {
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
                string credPath = "token.json";
                credential = GoogleWebAuthorizationBroker.AuthorizeAsync(
                    GoogleClientSecrets.Load(stream).Secrets,
                    scopes,
                    "user",
                    CancellationToken.None,
                    new FileDataStore(credPath, true)).Result;
            }

            CalendarService service = new CalendarService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = credential,
                ApplicationName = "Risebird_test",
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
                string credPath = "token.json";
                credential = GoogleWebAuthorizationBroker.AuthorizeAsync(
                    GoogleClientSecrets.Load(stream).Secrets,
                    scopes,
                    "user",
                    CancellationToken.None,
                    new FileDataStore(credPath, true)).Result;
            }

            CalendarService service = new CalendarService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = credential,
                ApplicationName = "Risebird_test",
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
                string credPath = "tokendrive.json";
                credential = GoogleWebAuthorizationBroker.AuthorizeAsync(
                    GoogleClientSecrets.Load(stream).Secrets,
                                scopes,
                                "user",
                                CancellationToken.None,
                                new FileDataStore(credPath, true)).Result;
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

            using (var stream = new FileStream(Path.Combine(Directory.GetCurrentDirectory(), "Cre", "credrive.json"), FileMode.Open, FileAccess.Read))
            {
                string credPath = "tokendrive.json";
                credential = GoogleWebAuthorizationBroker.AuthorizeAsync(
                    GoogleClientSecrets.Load(stream).Secrets,
                                scopes,
                                "user",
                                CancellationToken.None,
                                new FileDataStore(credPath, true)).Result;
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
    }
}
