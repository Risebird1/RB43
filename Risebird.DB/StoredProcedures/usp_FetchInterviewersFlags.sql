IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_FetchInterviewersFlags'))
BEGIN
    DROP PROCEDURE dbo.usp_FetchInterviewersFlags
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
Create Procedure usp_FetchInterviewersFlags
@Emailid varchar(150)
As
BEGIN
Select ul.Emailid,ip.isApproved,ul.Isactive,ul.IsInterviewerAccess,ip.IsInterest as IsInterested,ul.LastLogin,ip.IsLeadInterviewer,ip.CreatedBy,ip.id,ip.IsDeleted,
-- Call opt-out: flag panel "Calls" column (Opted out when Isavailable = 5 or CallConsent = 0) and its marker (AICall | HumanCall).
CAST(CASE WHEN ip.Isavailable = 5 OR ip.CallConsent = 0 THEN 1 ELSE 0 END AS bit) as IsCallOptedOut,ip.CallOptOutSource,ip.CallConsent,ul.id as UserId from 
InterviewerProfile ip join Userlist ul on ul.id=ip.Userid 
where ul.Emailid=@Emailid
END
GO