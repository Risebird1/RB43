/****** Object:  StoredProcedure [dbo].[usp_GetInterviewerProfiletByUserId]    Script Date: 4/17/2020 10:08:42 PM ******/
IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_GetInterviewerProfiletByUserId'))
BEGIN
    DROP PROCEDURE dbo.usp_GetInterviewerProfiletByUserId
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[usp_GetInterviewerProfiletByUserId]

@userId int

AS
Begin


SELECT id, UserId, FirstName, LastName, Nickname, EmailId, AltemailId, [Location],
MobileNumber, AltMobileNumber, CurrentRole , Experience, PreferredDomain, ResumeLink, 
CodedResumeLink, IsLeadInterviewer, IsActive, IsApproved
 
from 
InterviewerProfile
  
WHERE userId = @userid ;


End
GO


