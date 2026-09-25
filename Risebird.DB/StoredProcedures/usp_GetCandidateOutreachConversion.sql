-- =============================================
-- SP Name:     dbo.usp_GetCandidateOutreachConversion
-- Description: Call Analytics "Conversion" tab (RB38). Candidate calls placed by HR / Senior HR,
--              tagged Reschedule vs NewSchedule, with how many were followed by a booking.
--
--   OutreachType  Reschedule  = the candidate already had a meeting (same IRF) before the call
--                 NewSchedule = no earlier meeting
--   Converted     NewSchedule: an InterviewMeetings row for the same IRF + candidate is created
--                              within @windowDays after the call
--                 Reschedule:  the same, OR an existing meeting's interviewDate is changed within
--                              the window (reschedules update the row instead of inserting one)
--
--   This is inference by time window, not a stored call -> booking link, so treat it as a
--   team / role conversion rate rather than a per-call fact.
--
--   Clocks differ per table, so everything is compared in UTC:
--     CallRecordingData.DateCreated     web server local time -> pass its UTC offset in @callTimeOffsetMinutes
--     InterviewMeetings.dtCreatedOn     GETUTCDATE()
--     InterviewMeetingsHistory.PerformedAt  DATEADD(MINUTE, 330, getdate()) i.e. IST
--
--   CallRecordingData.IRFId holds IRF.id (numeric) while InterviewMeetings.IRFId holds IRF.IRFId
--   (the code), so the call's IRF is resolved through IRF / InterviewMeetings first.
-- =============================================
IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_GetCandidateOutreachConversion'))
BEGIN
    DROP PROCEDURE dbo.usp_GetCandidateOutreachConversion
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[usp_GetCandidateOutreachConversion]
@from date,
@to date,
@callTimeOffsetMinutes int = 0,
@windowDays int = 3
AS
BEGIN
SET NOCOUNT ON;

;with hr_calls as (
    select crd.id,
           crd.[Caller],
           callerRole.[Name] as CallerProfileType,
           ltrim(rtrim(crd.CallRecipient)) as candidateEmail,
           coalesce(irf.IRFId, imCall.IRFId) as IRFCode,
           dateadd(minute, -@callTimeOffsetMinutes, crd.DateCreated) as CallUtc,
           case when TRY_CONVERT(float, crd.Duration) > 0 then 1
                when crd.Duration like '%:%' and TRY_CONVERT(time, crd.Duration) > '00:00:00' then 1
                else 0 end as Connected
    from CallRecordingData crd
    outer apply (select top 1 pr.[Name]
                 from UserList ul join [Profile] pr on pr.id = ul.idProfile
                 where ul.EmailId = crd.[Caller]) callerRole
    left join IRF irf on irf.id = TRY_CONVERT(int, crd.IRFId)
    left join InterviewMeetings imCall on imCall.id = crd.InterviewId
    where callerRole.[Name] in ('HR', 'SeniorHR')
      and coalesce(nullif(crd.RecipientKind, ''), crd.RecipientProfileType) = 'Candidate'
      and crd.CallRecipient is not null
      and crd.DateCreated >= @from and crd.DateCreated < dateadd(day, 1, @to)
),
tagged as (
    select c.*,
           case when exists (select 1 from InterviewMeetings m
                             where ltrim(rtrim(m.candidateEmail)) = c.candidateEmail
                               and (c.IRFCode is null or m.IRFId = c.IRFCode)
                               and m.dtCreatedOn < c.CallUtc)
                then 'Reschedule' else 'NewSchedule' end as OutreachType
    from hr_calls c
),
scored as (
    select t.*,
           case when exists (select 1 from InterviewMeetings m
                             where ltrim(rtrim(m.candidateEmail)) = t.candidateEmail
                               and (t.IRFCode is null or m.IRFId = t.IRFCode)
                               and m.dtCreatedOn >= t.CallUtc
                               and m.dtCreatedOn < dateadd(day, @windowDays, t.CallUtc))
                  or (t.OutreachType = 'Reschedule'
                      and exists (select 1 from InterviewMeetingsHistory h
                                  where h.ActionType = 'Updated'
                                    and ltrim(rtrim(h.candidateEmail)) = t.candidateEmail
                                    and (t.IRFCode is null or h.IRFId = t.IRFCode)
                                    and dateadd(minute, -330, h.PerformedAt) >= t.CallUtc
                                    and dateadd(minute, -330, h.PerformedAt) < dateadd(day, @windowDays, t.CallUtc)
                                    and h.interviewDate is not null
                                    and h.interviewDate <> isnull((select top 1 p.interviewDate
                                                                  from InterviewMeetingsHistory p
                                                                  where p.id = h.id and p.PerformedAt < h.PerformedAt
                                                                  order by p.PerformedAt desc), h.interviewDate)))
                then 1 else 0 end as Converted
    from tagged t
)
select s.[Caller],
       max(s.CallerProfileType) as CallerProfileType,
       s.OutreachType,
       count(*) as CallsPlaced,
       sum(s.Connected) as CallsConnected,
       sum(s.Converted) as Converted
from scored s
group by s.[Caller], s.OutreachType
order by s.[Caller], s.OutreachType

END
GO
