/****** Object:  StoredProcedure [dbo].[usp_GetAllUserVMs]    Script Date: 06-10-2020 7.49.45 PM ******/

IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetIRFMembersEmails'))
BEGIN
    DROP PROCEDURE dbo.usp_GetIRFMembersEmails
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[usp_GetIRFMembersEmails]
@IRFId int 

AS
Begin

declare @Members table (EmailId varchar(100));
		
	Begin

	insert into @Members
	select ul.EmailId from TransEmergencyInterviewer tei join UserList ul on ul.id = tei.UserId where IRFId = @IRFId and tei.IsActive=1

	insert into @Members
	select ul.EmailId from TransInterviewer ti join UserList ul on ul.id = ti.UserId where IRFId = @IRFId and ti.IsActive=1

	insert into @Members
	select ul.EmailId from TransQualityController tqc join UserList ul on ul.id = tqc.UserId where IRFId = @IRFId and tqc.IsActive=1

	select distinct(EmailId) from @Members

	End
		


End

GO


