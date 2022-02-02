//using Dapper;
//using Microsoft.Azure.WebJobs;
//using Microsoft.Azure.WebJobs.Host;
//using System;
//using System.Collections.Generic;
//using System.Configuration;
//using System.Data.SqlClient;
//using System.Linq;
//using System.Threading.Tasks;
//using Twilio;
//using Twilio.Rest.Api.V2010.Account;
//using Twilio.Types;

//namespace Risebird.Functions
//{
//    public static class VoiceCallCandidate
//    {
//        [FunctionName("VoiceCallCandidate")]
//        public static void Run([TimerTrigger("0 */30 * * * *")] TimerInfo myTimer, TraceWriter log)
//        {
//            log.Info($"Voice call Timer trigger function executed at: {DateTime.Now}");
//            try
//            {
//                var phoneNumbers = new List<string>();
//                var str = ConfigurationManager.ConnectionStrings["sqldb_connection"].ConnectionString;
//                var query = "select MobileNumber from InterviewMeetings where interviewDate<=@timeH and interviewDate>=@timeL and confirmationStatus=1";
//                var dynamicParameters = new DynamicParameters();
//                dynamicParameters.Add("@timeL", DateTime.Now);
//                dynamicParameters.Add("@timeH", DateTime.Now.AddMinutes(35));
//                using (SqlConnection con = new SqlConnection(str))
//                {
//                    phoneNumbers = con.Query<string>(query, dynamicParameters).ToList();
//                }

//                if (phoneNumbers != null && phoneNumbers.Count() > 0)
//                {
//                    var twilioAuthToken = "3f15940e73243adb8e75f9689adaa0e2";
//                    var twilioAccountSID = "ACcad9f6682d4a76df9af7cde78dd0867e";
//                    var twilioVoiceCallNumber = "+17274782239";

//                    Parallel.ForEach(phoneNumbers, phoneNumber =>
//                    {
//                        if (!string.IsNullOrEmpty(phoneNumber))
//                        {
//                            try
//                            {
//                                TwilioClient.Init(twilioAccountSID, twilioAuthToken);
//                                var to = new PhoneNumber(phoneNumber);
//                                var from = new PhoneNumber(twilioVoiceCallNumber);
//                                var call = CallResource.CreateAsync(
//                                    to: to,
//                                    from: from,
//                                    url: new Uri("https://beige-vulture-6005.twil.io/assets/phoneCalCandidateFile.xml"));
//                                log.Info($"Voice call Candidate Timer trigger function executed for: {phoneNumber}");
//                            }
//                            catch (Exception)
//                            {
//                                log.Error($"Failed to make call. Please check all filled and try again.");
//                            }
//                        }

//                    });
//                }

//            }
//            catch (Exception)
//            {
//                log.Error($"Failed to make call. Please check all filled and try again.");
//            }
//        }
//    }
//}
