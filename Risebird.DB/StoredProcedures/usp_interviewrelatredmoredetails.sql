IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_interviewrelatredmoredetails'))BEGIN    DROP PROCEDURE dbo.usp_interviewrelatredmoredetailsEND
SET ANSI_NULLS ONGOSET QUOTED_IDENTIFIER ONGO
Create Procedure usp_interviewrelatredmoredetails
@Interviewid int
AS
BEGIN
Select ic.InterviewStatus,ic.MeetingStatus,pm.DurationSelectedMin as interviewDuration,(pm.DurationSelectedMin-ic.VideoDuration) as deficit,CASE  WHEN DATEDIFF(MINUTE, im.interviewDate,imj.Joinedon) >= 5 THEN 1  ELSE 0  END AS InterviewerLateJoin,cfd.Rating,imj.JoinedonFrom InterviewMeetings im left join InterviewComments ic on ic.Interviewid=im.idleft join IRF on IRF.IRFId=im.IRFId   left join PlanTypeChargesMaster pm on pm.CountryType=IRF.JobCountry and pm.CompanyType=IRF.CompanyType and pm.PlanTypeEnumValue=IRF.Heuristicsscoreleft join CandidateFeedbackDetails cfd on cfd.Interviewid=im.id outer apply(Select Min(Joinedon) Joinedon from InterviewMeetingsJoinTime where interviewid=im.id) as imjwhere im.id=@InterviewidENDGO