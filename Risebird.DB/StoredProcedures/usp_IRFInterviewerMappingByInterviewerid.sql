IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_IRFInterviewerMappingByInterviewerid'))
BEGIN
    DROP PROCEDURE [dbo].[usp_IRFInterviewerMappingByInterviewerid]
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE usp_IRFInterviewerMappingByInterviewerid
@Emailid varchar(100)
AS
BEGIN
Select ti.IsAvailable,IRF.IRFId,IRF.JDLink,IRF.PrimarySkillInvite,IRF.id as IRFintid,ti.id 
From IRF join TransInterviewer ti on ti.IRFId=IRF.id join UserList ul on ul.id=ti.UserId
Where ul.EmailId=@Emailid
END
GO

