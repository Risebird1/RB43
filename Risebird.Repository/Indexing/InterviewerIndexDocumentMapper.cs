using Azure.Search.Documents.Models;
using Risebird.Entities.Search;
using Risebird.Entities.ViewModel;
using Risebird.Repository.Search;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Risebird.Repository.Indexing
{
    /// <summary>
    /// Converts interviewer index data between SQL, domain documents, Azure payloads, and list VMs.
    /// </summary>
    internal static class InterviewerIndexDocumentMapper
    {
        public static InterviewerIndexDocument FromDatabaseRow(InterviewerIndexDocumentRow row)
        {
            if (row == null)
                throw new ArgumentNullException(nameof(row));

            return new InterviewerIndexDocument
            {
                InterviewerProfileId = row.InterviewerProfileId,
                UserId = row.UserId,
                FirstName = row.FirstName,
                LastName = row.LastName,
                NickName = row.NickName,
                EmailId = row.EmailId,
                MobileNumber = row.MobileNumber,
                PrimarySkill = row.PrimarySkill,
                Skills = SplitCsv(row.PrimarySkill),
                AssessmentGroupId = row.AssessmentGroupId,
                AssessmentGroupNames = SplitCsv(row.AssessmentGroupNames),
                Senioritylevel = row.Senioritylevel,
                PerHourRateIndia = row.PerHourRateIndia,
                PerHourRateOutside = row.PerHourRateOutside,
                CompentencyLevel = row.CompentencyLevel,
                ProfileType = row.ProfileType,
                CompanyType = row.CompanyType,
                Country = row.Country,
                IsLeadInterviewer = row.IsLeadInterviewer,
                IsActive = row.IsActive,
                IsInterviewerAccess = row.IsInterviewerAccess,
                IsApproved = row.IsApproved,
                IsInterested = row.IsInterested,
                IsDeleted = row.IsDeleted,
                Isavailable = row.Isavailable,
                AvailabilityStatusValue = row.AvailabilityStatusValue,
                VideoConfirmation = row.VideoConfirmation,
                ResumeLink = row.ResumeLink,
                CodedResumeLink = row.CodedResumeLink,
                City = row.City,
                CurrentRole = row.CurrentRole,
                Experience = row.Experience,
                PreferredDomain = row.PreferredDomain,
                uniqueid = row.uniqueid,
                AvailabilityResponseDate = row.AvailabilityResponseDate,
                IsHoldPayment = row.IsHoldPayment
            };
        }

        public static SearchDocument ToAzureDocument(InterviewerIndexDocument document)
        {
            if (document == null)
                throw new ArgumentNullException(nameof(document));

            var azureDoc = new SearchDocument
            {
                [InterviewerIndexFields.Key] = document.InterviewerProfileId.ToString(CultureInfo.InvariantCulture),
                [InterviewerIndexFields.UserId] = document.UserId,
                [InterviewerIndexFields.FirstName] = document.FirstName ?? string.Empty,
                [InterviewerIndexFields.LastName] = document.LastName ?? string.Empty,
                [InterviewerIndexFields.NickName] = document.NickName ?? string.Empty,
                [InterviewerIndexFields.EmailId] = document.EmailId ?? string.Empty,
                [InterviewerIndexFields.MobileNumber] = document.MobileNumber ?? string.Empty,
                [InterviewerIndexFields.PrimarySkill] = document.PrimarySkill ?? string.Empty,
                [InterviewerIndexFields.Skills] = document.Skills?.ToList() ?? new List<string>(),
                [InterviewerIndexFields.AssessmentGroupId] = document.AssessmentGroupId ?? string.Empty,
                [InterviewerIndexFields.AssessmentGroupNames] = document.AssessmentGroupNames?.ToList() ?? new List<string>(),
                [InterviewerIndexFields.Senioritylevel] = document.Senioritylevel ?? string.Empty,
                [InterviewerIndexFields.PerHourRateIndia] = document.PerHourRateIndia ?? string.Empty,
                [InterviewerIndexFields.PerHourRateOutside] = document.PerHourRateOutside ?? string.Empty,
                [InterviewerIndexFields.CompentencyLevel] = document.CompentencyLevel ?? string.Empty,
                [InterviewerIndexFields.ProfileType] = document.ProfileType ?? string.Empty,
                [InterviewerIndexFields.CompanyType] = document.CompanyType ?? string.Empty,
                [InterviewerIndexFields.Country] = document.Country ?? string.Empty,
                [InterviewerIndexFields.IsLeadInterviewer] = document.IsLeadInterviewer,
                [InterviewerIndexFields.IsActive] = document.IsActive,
                [InterviewerIndexFields.IsInterviewerAccess] = document.IsInterviewerAccess,
                [InterviewerIndexFields.IsApproved] = document.IsApproved,
                [InterviewerIndexFields.IsInterested] = document.IsInterested,
                [InterviewerIndexFields.IsDeleted] = document.IsDeleted,
                [InterviewerIndexFields.Isavailable] = document.Isavailable,
                [InterviewerIndexFields.AvailabilityStatusValue] = document.AvailabilityStatusValue ?? string.Empty,
                [InterviewerIndexFields.VideoConfirmation] = document.VideoConfirmation,
                [InterviewerIndexFields.ResumeLink] = document.ResumeLink ?? string.Empty,
                [InterviewerIndexFields.CodedResumeLink] = document.CodedResumeLink ?? string.Empty,
                [InterviewerIndexFields.City] = document.City ?? string.Empty,
                [InterviewerIndexFields.CurrentRole] = document.CurrentRole ?? string.Empty,
                [InterviewerIndexFields.Experience] = (double)document.Experience,
                [InterviewerIndexFields.PreferredDomain] = document.PreferredDomain ?? string.Empty,
                [InterviewerIndexFields.UniqueId] = document.uniqueid ?? string.Empty,
                [InterviewerIndexFields.IsHoldPayment] = document.IsHoldPayment
            };

            if (document.AvailabilityResponseDate.HasValue)
                azureDoc[InterviewerIndexFields.AvailabilityResponseDate] = ToIndexDate(document.AvailabilityResponseDate.Value);

            return azureDoc;
        }

        public static InterviewerProfileVM ToListViewModel(SearchDocument document)
        {
            if (document == null)
                throw new ArgumentNullException(nameof(document));

            return new InterviewerProfileVM
            {
                id = AzureSearchFieldReader.GetInt(document, InterviewerIndexFields.Key),
                UserId = AzureSearchFieldReader.GetInt(document, InterviewerIndexFields.UserId),
                FirstName = AzureSearchFieldReader.GetString(document, InterviewerIndexFields.FirstName),
                LastName = AzureSearchFieldReader.GetString(document, InterviewerIndexFields.LastName),
                NickName = AzureSearchFieldReader.GetString(document, InterviewerIndexFields.NickName),
                EmailId = AzureSearchFieldReader.GetString(document, InterviewerIndexFields.EmailId),
                MobileNumber = AzureSearchFieldReader.GetString(document, InterviewerIndexFields.MobileNumber),
                PrimarySkill = AzureSearchFieldReader.GetString(document, InterviewerIndexFields.PrimarySkill),
                AssessmentGroupId = AzureSearchFieldReader.GetString(document, InterviewerIndexFields.AssessmentGroupId),
                Senioritylevel = AzureSearchFieldReader.GetString(document, InterviewerIndexFields.Senioritylevel),
                PerHourRateIndia = AzureSearchFieldReader.GetString(document, InterviewerIndexFields.PerHourRateIndia),
                PerHourRateOutside = AzureSearchFieldReader.GetString(document, InterviewerIndexFields.PerHourRateOutside),
                CompentencyLevel = AzureSearchFieldReader.GetString(document, InterviewerIndexFields.CompentencyLevel),
                ProfileType = AzureSearchFieldReader.GetString(document, InterviewerIndexFields.ProfileType),
                CompanyType = AzureSearchFieldReader.GetString(document, InterviewerIndexFields.CompanyType),
                Country = AzureSearchFieldReader.GetString(document, InterviewerIndexFields.Country),
                IsLeadInterviewer = AzureSearchFieldReader.GetBool(document, InterviewerIndexFields.IsLeadInterviewer),
                IsActive = AzureSearchFieldReader.GetBool(document, InterviewerIndexFields.IsActive),
                IsInterviewerAccess = AzureSearchFieldReader.GetBool(document, InterviewerIndexFields.IsInterviewerAccess),
                IsApproved = AzureSearchFieldReader.GetBool(document, InterviewerIndexFields.IsApproved),
                IsInterested = AzureSearchFieldReader.GetBool(document, InterviewerIndexFields.IsInterested),
                IsDeleted = AzureSearchFieldReader.GetBool(document, InterviewerIndexFields.IsDeleted),
                Isavailable = AzureSearchFieldReader.GetInt(document, InterviewerIndexFields.Isavailable),
                AvailabilityStatusValue = AzureSearchFieldReader.GetString(document, InterviewerIndexFields.AvailabilityStatusValue),
                VideoConfirmation = AzureSearchFieldReader.GetInt(document, InterviewerIndexFields.VideoConfirmation),
                ResumeLink = AzureSearchFieldReader.GetString(document, InterviewerIndexFields.ResumeLink),
                CodedResumeLink = AzureSearchFieldReader.GetString(document, InterviewerIndexFields.CodedResumeLink),
                City = AzureSearchFieldReader.GetString(document, InterviewerIndexFields.City),
                CurrentRole = AzureSearchFieldReader.GetString(document, InterviewerIndexFields.CurrentRole),
                Experience = (decimal)AzureSearchFieldReader.GetDouble(document, InterviewerIndexFields.Experience),
                PreferredDomain = AzureSearchFieldReader.GetString(document, InterviewerIndexFields.PreferredDomain),
                uniqueid = AzureSearchFieldReader.GetString(document, InterviewerIndexFields.UniqueId),
                AvailabilityResponseDate = AzureSearchFieldReader.GetNullableDateTime(document, InterviewerIndexFields.AvailabilityResponseDate),
                IsHoldPayment = AzureSearchFieldReader.GetBool(document, InterviewerIndexFields.IsHoldPayment)
            };
        }

        private static IList<string> SplitCsv(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return new List<string>();

            return value
                .Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private static DateTimeOffset ToIndexDate(DateTime value)
        {
            if (value == default)
                return DateTimeOffset.MinValue;

            if (value.Kind == DateTimeKind.Unspecified)
                return new DateTimeOffset(value, TimeSpan.Zero);

            return new DateTimeOffset(value.ToUniversalTime());
        }
    }
}
