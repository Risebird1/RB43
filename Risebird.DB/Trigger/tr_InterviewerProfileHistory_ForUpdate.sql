
IF EXISTS (SELECT 1 FROM sys.triggers 
           WHERE Name = 'tr_InterviewerProfileHistory_ForUpdate')
BEGIN
    Drop trigger tr_InterviewerProfileHistory_ForUpdate
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TRIGGER tr_InterviewerProfileHistory_ForUpdate
ON InterviewerProfile
FOR UPDATE
AS
BEGIN
 Declare @id int
 Select @id = Id from inserted

 insert into InterviewerProfileHistory
 (id, UserId, NickName,	AltEmailID,	DOB, Gender, AnnualSalary, Country,	City, AltMobileNumber, CurrentRole,
 Experience, PreferredDomain, GitHubLink, ResumeLink,	CodedResumeLink, StackOverflowLink,	LinkedinLink,	
 Blog,	IsLeadInterviewer,	IsApproved,	Tag, AssessmentGroupId, Isinstaavailable, Isavailable, IsNotificationAccepted,
 CreatedOn,	ModifiedOn,	CreatedBy,	ModifiedBy,
 PerformedAt, PerformedBy, ActionType)

 (select id, UserId, NickName,	AltEmailID,	DOB, Gender, AnnualSalary, Country,	City, AltMobileNumber, CurrentRole,
 Experience, PreferredDomain, GitHubLink, ResumeLink,	CodedResumeLink, StackOverflowLink,	LinkedinLink,	
 Blog,	IsLeadInterviewer,	IsApproved,	Tag, AssessmentGroupId, Isinstaavailable, Isavailable, IsNotificationAccepted,
 CreatedOn,	ModifiedOn,	CreatedBy,	ModifiedBy, dateadd(MINUTE, 330, getdate()), ModifiedBy, 'Updated'
 from InterviewerProfile where id = @id )

END

