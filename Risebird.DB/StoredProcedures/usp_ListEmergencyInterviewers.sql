/****** Object:  StoredProcedure [dbo].[usp_ListEmergencyInterviewers]    Script Date: 18-11-2019 6.41.19 PM ******/
IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_ListEmergencyInterviewers'))
BEGIN
    DROP PROCEDURE dbo.usp_ListEmergencyInterviewers
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO




CREATE PROCEDURE [dbo].[usp_ListEmergencyInterviewers]

@IRFId int

AS
Begin

select ul.EmailId from TransEmergencyInterviewer tei
join 
UserList ul on tei.UserId = ul.id where IRFId=@IRFId and  tei.IsActive=1

End

GO


