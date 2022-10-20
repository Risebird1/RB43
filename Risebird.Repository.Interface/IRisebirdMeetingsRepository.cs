using System;

namespace Risebird.Repository.Interface
{
    public interface IRisebirdMeetingsRepository
    {
        bool AddOrUpdateMeeting(DateTime interviewDate, string interviewDuration, string candidateName, string candidateEmail, string resumeLink,
                    string zoomLink, string IRFID, string zoommeetingId, string interviewOnDevice, string textreminder, string staffingAgenecy, string ClientHrContact, int TimeZoneoffset, string TimeZonesValue, string zoomAccountEmail = null);
    }
}
