IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_UpdateStatusInterviewerVideoKYC'))
BEGIN
    DROP PROCEDURE dbo.usp_UpdateStatusInterviewerVideoKYC
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
CREATE Procedure usp_UpdateStatusInterviewerVideoKYC
@id int,
@type varchar(50)
AS
Begin
IF(@type='Approve')
Begin
Update InterviewerProfile set IsVideoApproved=1 where Id=@id
End
ELSE
Begin
Update InterviewerProfile set VideoLink=NULL where Id=@id
END
End




GO