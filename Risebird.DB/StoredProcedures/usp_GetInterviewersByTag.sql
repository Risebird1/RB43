/****** Object:  StoredProcedure [dbo].[usp_GetCandidateAudios]    Script Date: 02-02-2021 5.20.49 PM ******/
IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetInterviewersByTag'))
BEGIN
    DROP PROCEDURE dbo.usp_GetInterviewersByTag
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO


Create PROCEDURE [dbo].[usp_GetInterviewersByTag]

@Tag varchar(200)

AS
Begin

select UserId, ul.EmailId from InterviewerProfile ipr join UserList ul on ul.id =ipr.UserId where IsApproved=1 and Tag like ('%' +@Tag +'%')


End


GO


--jwere
--rumatest3
