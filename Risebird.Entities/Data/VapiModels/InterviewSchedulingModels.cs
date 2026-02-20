using System;
using Newtonsoft.Json;

namespace Risebird.Entities.Data.VapiModels
{
    /// <summary>
    /// Data model for interview slot submission from Vapi assistant
    /// </summary>
    public class InterviewSlotSubmission
    {
        [JsonProperty("callId")]
        public string CallId { get; set; }

        [JsonProperty("candidateName")]
        public string CandidateName { get; set; }

        [JsonProperty("candidatePhone")]
        public string CandidatePhone { get; set; }

        [JsonProperty("interviewDate")]
        public string InterviewDate { get; set; } // YYYY-MM-DD format

        [JsonProperty("interviewTime")]
        public string InterviewTime { get; set; } // HH:MM AM/PM IST format

        [JsonProperty("timezone")]
        public string Timezone { get; set; } // Should be IST

        [JsonProperty("candidateConfirmed")]
        public bool CandidateConfirmed { get; set; }

        [JsonProperty("rawTranscript")]
        public string RawTranscript { get; set; }

        /// <summary>
        /// Parses the interview date and time into a DateTime object
        /// </summary>
        public DateTime? GetInterviewDateTime()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(InterviewDate) || string.IsNullOrWhiteSpace(InterviewTime))
                    return null;

                var dateTimeString = $"{InterviewDate} {InterviewTime}";
                
                // Try parsing with various formats
                string[] formats = { 
                    "yyyy-MM-dd hh:mm tt",
                    "yyyy-MM-dd h:mm tt",
                    "yyyy-MM-dd HH:mm",
                    "yyyy-MM-dd H:mm"
                };

                foreach (var format in formats)
                {
                    if (DateTime.TryParseExact(dateTimeString, format, 
                        System.Globalization.CultureInfo.InvariantCulture, 
                        System.Globalization.DateTimeStyles.None, out DateTime result))
                    {
                        return result;
                    }
                }

                return null;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Validates the submitted data
        /// </summary>
        public (bool isValid, string errorMessage) Validate()
        {
            if (string.IsNullOrWhiteSpace(CandidateName))
                return (false, "Candidate name is missing");

            if (string.IsNullOrWhiteSpace(InterviewDate))
                return (false, "Interview date is missing");

            if (string.IsNullOrWhiteSpace(InterviewTime))
                return (false, "Interview time is missing");

            if (!CandidateConfirmed)
                return (false, "Candidate has not confirmed the slot");

            var dateTime = GetInterviewDateTime();
            if (!dateTime.HasValue)
                return (false, "Invalid date/time format");

            // Check if within next 2 days
            var maxDate = DateTime.Now.AddDays(2).Date.AddDays(1); // End of day 2 from now
            if (dateTime.Value > maxDate)
                return (false, "Interview date must be within next 2 days");

            // Check if in the past
            if (dateTime.Value < DateTime.Now)
                return (false, "Interview date cannot be in the past");

            return (true, null);
        }
    }

    /// <summary>
    /// Response to send back to Vapi after processing interview slot
    /// </summary>
    public class InterviewSlotResponse
    {
        [JsonProperty("success")]
        public bool Success { get; set; }

        [JsonProperty("message")]
        public string Message { get; set; }

        [JsonProperty("bookingId")]
        public int? BookingId { get; set; }

        [JsonProperty("interviewDateTime")]
        public string InterviewDateTime { get; set; }
    }
}
